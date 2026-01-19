// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#define APIHACK
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using OpenLiveWriter.CoreServices;
using OpenLiveWriter.CoreServices.Threading;
using OpenLiveWriter.Extensibility.BlogClient;
using OpenLiveWriter.HtmlParser.Parser;

namespace OpenLiveWriter.BlogClient.Clients
{
    public class AtomMediaUploader
    {
        protected const string EDIT_MEDIA_LINK = "EditMediaLink";
        protected const string EDIT_MEDIA_ENTRY_LINK = "EditMediaLinkEntryLink";
        protected const string MEDIA_ETAG = "MediaEtag";

        protected XmlNamespaceManager _nsMgr
        {
            get;
            private set;
        }
        protected HttpRequestFilter _requestFilter
        {
            get;
            private set;
        }
        protected readonly string _collectionUri;
        protected IBlogClientOptions _options
        {
            get;
            private set;
        }
        protected XmlRestRequestHelper xmlRestRequestHelper
        {
            get;
            private set;
        }

        public AtomMediaUploader(XmlNamespaceManager nsMgr, HttpRequestFilter requestFilter, string collectionUri, IBlogClientOptions options)
            : this(nsMgr, requestFilter, collectionUri, options, new XmlRestRequestHelper())
        {
        }

        public AtomMediaUploader(XmlNamespaceManager nsMgr, HttpRequestFilter requestFilter, string collectionUri, IBlogClientOptions options, XmlRestRequestHelper xmlRestRequestHelper)
        {
            this._nsMgr = nsMgr;
            this._requestFilter = requestFilter;
            this._collectionUri = collectionUri;
            this._options = options;
            this.xmlRestRequestHelper = xmlRestRequestHelper;
        }

        public string DoBeforePublishUploadWork(IFileUploadContext uploadContext)
        {
            string path = uploadContext.GetContentsLocalFilePath();
            string srcUrl;
            string editUri = uploadContext.Settings.GetString(EDIT_MEDIA_LINK, null);
            string editEntryUri = uploadContext.Settings.GetString(EDIT_MEDIA_ENTRY_LINK, null);
            string etag = uploadContext.Settings.GetString(MEDIA_ETAG, null);
            if (string.IsNullOrEmpty(editUri))
            {
                PostNewImage(path, false, out srcUrl, out editUri, out editEntryUri);
            }
            else
            {
                try
                {
                    UpdateImage(ref editUri, path, editEntryUri, etag, true, out srcUrl);
                }
                catch (Exception e)
                {
                    Trace.Fail(e.ToString());

                    bool success = false;
                    srcUrl = null; // compiler complains without this line
                    try
                    {
                        // couldn't update existing image? try posting a new one
                        PostNewImage(path, false, out srcUrl, out editUri, out editEntryUri);
                        success = true;

                        if (e is WebException)
                        {
                            Trace.WriteLine("Image PUT failed, but POST succeeded. PUT exception follows.");
                            HttpRequestHelper.LogException((WebException)e);
                        }
                    }
                    catch
                    {
                    }
                    if (!success)
                        throw;  // rethrow the exception from the update, not the post
                }
            }
            uploadContext.Settings.SetString(EDIT_MEDIA_LINK, editUri);
            uploadContext.Settings.SetString(EDIT_MEDIA_ENTRY_LINK, editEntryUri);
            uploadContext.Settings.SetString(MEDIA_ETAG, null);

            UpdateETag(uploadContext, editUri);
            return srcUrl;
        }

        protected virtual void UpdateETag(IFileUploadContext uploadContext, string editUri)
        {
            try
            {
                string newEtag = AtomClient.GetEtag(editUri, _requestFilter);
                uploadContext.Settings.SetString(MEDIA_ETAG, newEtag);
            }
            catch (Exception)
            {

            }
        }

        public virtual void PostNewImage(string path, bool allowWriteStreamBuffering, out string srcUrl, out string editMediaUri, out string editEntryUri)
        {
            string mediaCollectionUri = _collectionUri;
            if (mediaCollectionUri == null || mediaCollectionUri == "")
                throw new BlogClientFileUploadNotSupportedException();

            try
            {
                using (var response = UploadImage(mediaCollectionUri, path, "POST", null))
                {
                    string entryUri;
                    string etag;
                    string selfPage;
                    XmlDocument xmlDoc = GetCreatedEntity(response, out entryUri, out etag);
                    ParseResponse(xmlDoc, out srcUrl, out editMediaUri, out editEntryUri, out selfPage);
                }
            }
            catch (HttpRequestException ex) when (ex.InnerException is WebException we)
            {
                // The error may have been due to the server requiring stream buffering (WinLive 114314, 252175)
                // Try again with stream buffering (ignored for HttpClient, but kept for API compatibility).
                if (we.Status == WebExceptionStatus.ProtocolError && !allowWriteStreamBuffering)
                {
                    PostNewImage(path, true, out srcUrl, out editMediaUri, out editEntryUri);
                }
                else
                {
                    throw;
                }
            }
            catch (WebException we)
            {
                // The error may have been due to the server requiring stream buffering (WinLive 114314, 252175)
                // Try again with stream buffering.
                if (we.Status == WebExceptionStatus.ProtocolError && !allowWriteStreamBuffering)
                {
                    PostNewImage(path, true, out srcUrl, out editMediaUri, out editEntryUri);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Uploads an image using HttpClient.
        /// </summary>
        private HttpResponseMessageWrapper UploadImage(string uri, string filename, string method, string etag)
        {
            byte[] fileBytes;
            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fileBytes = new byte[fs.Length];
                fs.Read(fileBytes, 0, fileBytes.Length);
            }

            return RedirectHelper.GetResponse(
                uri,
                new HttpMethod(method),
                request =>
                {
                    // Add slug if supported
                    if (_options != null && _options.SupportsSlug)
                        request.Headers.TryAddWithoutValidation("Slug", Path.GetFileNameWithoutExtension(filename));

                    // Add etag for updates
                    if (!string.IsNullOrEmpty(etag))
                        request.Headers.TryAddWithoutValidation("If-match", etag);

                    // Apply legacy filter for authorization
                    if (_requestFilter != null)
                        HttpRequestHelper.ApplyLegacyFilter(request, _requestFilter, uri);
                },
                () =>
                {
                    var content = new ByteArrayContent(fileBytes);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                        MimeHelper.GetContentType(Path.GetExtension(filename)));
                    return content;
                });
        }

        private XmlDocument GetCreatedEntity(HttpResponseMessageWrapper postResponse, out string editUri, out string etag)
        {
            editUri = postResponse.Headers["Location"];
            string contentLocation = postResponse.Headers["Content-Location"];
            if (string.IsNullOrEmpty(editUri) || editUri != contentLocation)
            {
                Uri uri = postResponse.ResponseUri;
                if (!string.IsNullOrEmpty(editUri))
                    uri = new Uri(editUri);
                WebHeaderCollection responseHeaders;
                XmlDocument doc = xmlRestRequestHelper.Get(ref uri, _requestFilter, out responseHeaders);
                etag = responseHeaders["ETag"];
                return doc;
            }
            else
            {
                etag = postResponse.Headers["ETag"];
                XmlDocument xmlDoc = new XmlDocument();
                using (Stream s = postResponse.GetResponseStream())
                    xmlDoc.Load(s);
                XmlHelper.ApplyBaseUri(xmlDoc, postResponse.ResponseUri);
                return xmlDoc;
            }
        }

        protected virtual void UpdateImage(ref string editMediaUri, string path, string editEntryUri, string etag, bool getEditInfo, out string srcUrl)
        {
            string thumbnailSmall;
            string thumbnailLarge;

            UpdateImage(false, ref editMediaUri, path, editEntryUri, etag, getEditInfo, out srcUrl, out thumbnailSmall, out thumbnailLarge);
        }

        protected virtual void UpdateImage(bool allowWriteStreamBuffering, ref string editMediaUri, string path, string editEntryUri, string etag, bool getEditInfo, out string srcUrl)
        {
            string thumbnailSmall;
            string thumbnailLarge;

            UpdateImage(allowWriteStreamBuffering, ref editMediaUri, path, editEntryUri, etag, getEditInfo, out srcUrl, out thumbnailSmall, out thumbnailLarge);
        }

        protected virtual void UpdateImage(bool allowWriteStreamBuffering, ref string editMediaUri, string path, string editEntryUri, string etag, bool getEditInfo, out string srcUrl, out string thumbnailSmall, out string thumbnailLarge)
        {
            try
            {
                using (var response = UploadImage(editMediaUri, path, "PUT", etag))
                {
                    // Response obtained successfully
                }
            }
            catch (HttpRequestException ex)
            {
                bool recovered = false;

                if (ex.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    string newEtag = AtomClient.GetEtag(editMediaUri, _requestFilter);
                    if (!string.IsNullOrEmpty(newEtag) && newEtag != etag)
                    {
                        if (!AtomClient.ConfirmOverwrite())
                            throw new BlogClientOperationCancelledException();

                        using (var response = UploadImage(editMediaUri, path, "PUT", newEtag))
                        {
                            // Response obtained successfully
                        }

                        recovered = true;
                    }
                }

                if (!recovered)
                    throw;
            }
            catch (WebException we)
            {
                bool recovered = false;

                if (we.Status == WebExceptionStatus.ProtocolError && we.Response != null)
                {
                    HttpWebResponse errResponse = we.Response as HttpWebResponse;
                    if (errResponse != null && errResponse.StatusCode == HttpStatusCode.PreconditionFailed)
                    {
                        string newEtag = AtomClient.GetEtag(editMediaUri, _requestFilter);
                        if (newEtag != null && newEtag.Length > 0 && newEtag != etag)
                        {
                            if (!AtomClient.ConfirmOverwrite())
                                throw new BlogClientOperationCancelledException();

                            using (var response = UploadImage(editMediaUri, path, "PUT", newEtag))
                            {
                                // Response obtained successfully
                            }

                            recovered = true;
                        }
                    }
                    else if (!allowWriteStreamBuffering)
                    {
                        // The error may have been due to the server requiring stream buffering (WinLive 114314, 252175)
                        // Try again with stream buffering.
                        UpdateImage(true, ref editMediaUri, path, editEntryUri, etag, getEditInfo, out srcUrl, out thumbnailSmall, out thumbnailLarge);
                        recovered = true;
                    }
                }
                if (!recovered)
                    throw;
            }

            // Check to see if we are going to get the src url and the etag, in most cases we will want to get this
            // information, but in the case of a photo album, since we never edit the image or link directly to them
            // we don't need the information and it can saves an http request.
            if (getEditInfo)
            {
                string selfPage;
                Uri uri = new Uri(editEntryUri);
                XmlDocument mediaLinkEntry = xmlRestRequestHelper.Get(ref uri, _requestFilter);
                ParseResponse(mediaLinkEntry, out srcUrl, out editMediaUri, out editEntryUri, out selfPage, out thumbnailSmall, out thumbnailLarge);
            }
            else
            {
                thumbnailSmall = null;
                thumbnailLarge = null;
                srcUrl = null;
            }
        }

        protected virtual void ParseResponse(XmlDocument xmlDoc, out string srcUrl, out string editUri, out string editEntryUri, out string selfPage, out string thumbnailSmall, out string thumbnailLarge)
        {
            thumbnailSmall = null;
            thumbnailLarge = null;
            ParseResponse(xmlDoc, out srcUrl, out editUri, out editEntryUri, out selfPage);
        }

        protected virtual void ParseResponse(XmlDocument xmlDoc, out string srcUrl, out string editUri, out string editEntryUri, out string selfPage)
        {
            XmlElement contentEl = xmlDoc.SelectSingleNode("/atom:entry/atom:content", _nsMgr) as XmlElement;
            srcUrl = XmlHelper.GetUrl(contentEl, "@src", null);
            editUri = AtomEntry.GetLink(xmlDoc.SelectSingleNode("/atom:entry", _nsMgr) as XmlElement, _nsMgr, "edit-media",
                              null, null, null);
            editEntryUri = AtomEntry.GetLink(xmlDoc.SelectSingleNode("/atom:entry", _nsMgr) as XmlElement, _nsMgr, "edit",
                                   null, null, null);
            selfPage = AtomEntry.GetLink(xmlDoc.SelectSingleNode("/atom:entry", _nsMgr) as XmlElement, _nsMgr, "alternate",
                       null, null, null);
        }
    }
}
