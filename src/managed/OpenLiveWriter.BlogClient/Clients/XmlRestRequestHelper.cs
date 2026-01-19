// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Xml;
using OpenLiveWriter.CoreServices;
using OpenLiveWriter.CoreServices.Diagnostics;
using OpenLiveWriter.HtmlParser.Parser.FormAgent;

namespace OpenLiveWriter.BlogClient.Clients
{
    /// <summary>
    /// Helper class for making REST-ful XML HTTP requests using HttpClient.
    /// </summary>
    public class XmlRestRequestHelper
    {
        public XmlRestRequestHelper()
        {
        }

        public XmlDocument Get(ref Uri uri, HttpRequestFilter filter, params string[] parameters)
        {
            WebHeaderCollection responseHeaders;
            return Get(ref uri, filter, out responseHeaders, parameters);
        }

        /// <summary>
        /// Retrieve the specified URI, using the given filter, with the supplied parameters (if any).
        /// The parameters parameter should be an even number of strings, where each odd element is
        /// a param name and each following even element is the corresponding param value.  For example,
        /// to retrieve http://www.vox.com/atom?svc=post&id=100, you would say:
        ///
        /// Get("http://www.vox.com/atom", "svc", "post", "id", "100");
        ///
        /// If a param value is null or empty string, that param will not be included in the final URL
        /// (i.e. the corresponding param name will also be dropped).
        /// </summary>
        public virtual XmlDocument Get(ref Uri uri, HttpRequestFilter filter, out WebHeaderCollection responseHeaders, params string[] parameters)
        {
            return SimpleRequest("GET", ref uri, filter, out responseHeaders, parameters);
        }

        /// <summary>
        /// Performs an HTTP DELETE on the URL and contains no body, returns the body as an XmlDocument if there is one
        /// </summary>
        public virtual XmlDocument Delete(Uri uri, HttpRequestFilter filter, out WebHeaderCollection responseHeaders)
        {
            return SimpleRequest("DELETE", ref uri, filter, out responseHeaders, new string[] { });
        }

        private static XmlDocument SimpleRequest(string method, ref Uri uri, HttpRequestFilter filter, out WebHeaderCollection responseHeaders, params string[] parameters)
        {
            string absUri = UrlHelper.SafeToAbsoluteUri(uri);

            if (parameters.Length > 0)
            {
                FormData formData = new FormData(true, parameters);

                if (absUri.IndexOf('?') == -1)
                    absUri += "?" + formData.ToString();
                else
                    absUri += "&" + formData.ToString();
            }

            using (HttpResponseMessageWrapper response = RedirectHelper.GetResponse(absUri, method,
                filter != null ? request => HttpRequestHelper.ApplyLegacyFilter(request, filter, absUri) : null))
            {
                uri = response.ResponseUri;
                responseHeaders = response.Headers;
                return ParseXmlResponse(response);
            }
        }

        /// <summary>
        /// Performs an HTTP PUT with the specified XML document as the request body.
        /// </summary>
        public XmlDocument Put(ref Uri uri, string etag, HttpRequestFilter filter, string contentType, XmlDocument doc, string encoding, bool ignoreResponse)
        {
            WebHeaderCollection responseHeaders;
            return Put(ref uri, etag, filter, contentType, doc, encoding, ignoreResponse, out responseHeaders);
        }

        /// <summary>
        /// Performs an HTTP PUT with the specified XML document as the request body.
        /// </summary>
        public XmlDocument Put(ref Uri uri, string etag, HttpRequestFilter filter, string contentType, XmlDocument doc, string encoding, bool ignoreResponse, out WebHeaderCollection responseHeaders)
        {
            return Send("PUT", ref uri, etag, filter, contentType, doc, encoding, null, ignoreResponse, out responseHeaders);
        }

        /// <summary>
        /// Performs an HTTP POST with the specified XML document as the request body.
        /// </summary>
        public XmlDocument Post(ref Uri uri, HttpRequestFilter filter, string contentType, XmlDocument doc, string encoding)
        {
            WebHeaderCollection responseHeaders;
            return Post(ref uri, filter, contentType, doc, encoding, out responseHeaders);
        }

        /// <summary>
        /// Performs an HTTP POST with the specified XML document as the request body.
        /// </summary>
        public XmlDocument Post(ref Uri uri, HttpRequestFilter filter, string contentType, XmlDocument doc, string encoding, out WebHeaderCollection responseHeaders)
        {
            return Send("POST", ref uri, null, filter, contentType, doc, encoding, null, false, out responseHeaders);
        }

        /// <summary>
        /// Performs a multipart MIME HTTP POST with the specified XML document as the request body and filename as the payload.
        /// </summary>
        public XmlDocument Post(ref Uri uri, HttpRequestFilter filter, string contentType, XmlDocument doc, string encoding, string filename, out WebHeaderCollection responseHeaders)
        {
            return Send("POST", ref uri, null, filter, contentType, doc, encoding, filename, false, out responseHeaders);
        }

        protected virtual XmlDocument MultipartSend(string method, ref Uri uri, string etag, HttpRequestFilter filter, string contentType, XmlDocument doc, string encoding, string filename, bool ignoreResponse, out WebHeaderCollection responseHeaders)
        {
            throw new NotImplementedException();
        }

        protected virtual XmlDocument Send(string method, ref Uri uri, string etag, HttpRequestFilter filter, string contentType, XmlDocument doc, string encoding, string filename, bool ignoreResponse, out WebHeaderCollection responseHeaders)
        {
            if (!String.IsNullOrEmpty(filename))
            {
                return MultipartSend(method, ref uri, etag, filter, contentType, doc, encoding, filename, ignoreResponse,
                                     out responseHeaders);
            }

            string absUri = UrlHelper.SafeToAbsoluteUri(uri);
            Debug.WriteLine("XML Request to " + absUri + ":\r\n" + doc.InnerXml);

            // Select encoding
            Encoding encodingToUse = new UTF8Encoding(false, false);
            try
            {
                encodingToUse = StringHelper.GetEncoding(encoding, encodingToUse);
            }
            catch (Exception ex)
            {
                Trace.Fail("Error while getting transport encoding: " + ex.ToString());
            }

            // Create XML content (captured for content factory)
            byte[] xmlBytes;
            using (var ms = new MemoryStream())
            {
                using (var writer = new XmlTextWriter(ms, encodingToUse))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.Indentation = 1;
                    writer.IndentChar = ' ';
                    writer.WriteStartDocument();
                    doc.DocumentElement.WriteTo(writer);
                    writer.Flush();
                }
                xmlBytes = ms.ToArray();
            }

            if (ApplicationDiagnostics.VerboseLogging)
                Trace.WriteLine(
                    string.Format(CultureInfo.InvariantCulture, "XML REST request:\r\n{0} {1}\r\n{2}\r\n{3}",
                        method, absUri, !string.IsNullOrEmpty(etag) ? "If-match: " + etag : "(no etag)", doc.OuterXml));

            using (HttpResponseMessageWrapper response = RedirectHelper.GetResponse(
                absUri,
                new HttpMethod(method),
                request =>
                {
                    if (!string.IsNullOrEmpty(etag))
                        request.Headers.TryAddWithoutValidation("If-match", etag);

                    // Apply legacy filter
                    if (filter != null)
                        HttpRequestHelper.ApplyLegacyFilter(request, filter, absUri);
                },
                () =>
                {
                    var content = new ByteArrayContent(xmlBytes);
                    if (!string.IsNullOrEmpty(contentType))
                        content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
                    return content;
                }))
            {
                responseHeaders = response.Headers;
                uri = response.ResponseUri;
                if (ignoreResponse || response.StatusCode == HttpStatusCode.NoContent)
                {
                    return null;
                }
                else
                {
                    XmlDocument xmlDocResponse = ParseXmlResponse(response);
                    return xmlDocResponse;
                }
            }
        }

        protected static XmlDocument ParseXmlResponse(HttpResponseMessageWrapper response)
        {
            MemoryStream ms = new MemoryStream();
            using (Stream s = response.GetResponseStream())
            {
                StreamHelper.Transfer(s, ms);
            }
            ms.Seek(0, SeekOrigin.Begin);

            try
            {
                if (ApplicationDiagnostics.VerboseLogging)
                    Trace.WriteLine("XML REST response:\r\n" + UrlHelper.SafeToAbsoluteUri(response.ResponseUri) + "\r\n" + new StreamReader(ms, Encoding.UTF8).ReadToEnd());
            }
            catch (Exception e)
            {
                Trace.Fail("Failed to log REST response: " + e.ToString());
            }

            ms.Seek(0, SeekOrigin.Begin);
            if (ms.Length == 0)
                return null;

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(ms);
                XmlHelper.ApplyBaseUri(xmlDoc, response.ResponseUri);

                return xmlDoc;
            }
            catch (Exception e)
            {
                Trace.Fail("Malformed XML document: " + e.ToString());
                return null;
            }
        }
    }
}
