// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using OpenLiveWriter.CoreServices;
using OpenLiveWriter.CoreServices.Diagnostics;
using OpenLiveWriter.HtmlParser.Parser.FormAgent;

namespace OpenLiveWriter.BlogClient.Clients
{
    /// <summary>
    /// Modern HttpClient-based helper class for making REST-ful XML HTTP requests.
    /// Replaces XmlRestRequestHelper for new code.
    /// </summary>
    public class HttpClientXmlRestRequestHelper
    {
        private readonly HttpClient _client;

        public HttpClientXmlRestRequestHelper() : this(HttpRequestHelper.HttpClient)
        {
        }

        public HttpClientXmlRestRequestHelper(HttpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Retrieve the specified URI with optional query parameters.
        /// </summary>
        public XmlDocument Get(ref Uri uri, HttpClientRedirectHelper.RequestConfigurator configureRequest, params string[] parameters)
        {
            return Get(ref uri, configureRequest, out _, parameters);
        }

        /// <summary>
        /// Retrieve the specified URI with optional query parameters.
        /// Parameters should be an even number of strings (name/value pairs).
        /// </summary>
        public virtual XmlDocument Get(ref Uri uri, HttpClientRedirectHelper.RequestConfigurator configureRequest, out HttpResponseHeaders responseHeaders, params string[] parameters)
        {
            return SimpleRequest(HttpMethod.Get, ref uri, configureRequest, out responseHeaders, parameters);
        }

        /// <summary>
        /// Performs an HTTP DELETE on the URL, returns the body as an XmlDocument if there is one.
        /// </summary>
        public virtual XmlDocument Delete(Uri uri, HttpClientRedirectHelper.RequestConfigurator configureRequest, out HttpResponseHeaders responseHeaders)
        {
            return SimpleRequest(HttpMethod.Delete, ref uri, configureRequest, out responseHeaders);
        }

        private XmlDocument SimpleRequest(HttpMethod method, ref Uri uri, HttpClientRedirectHelper.RequestConfigurator configureRequest, out HttpResponseHeaders responseHeaders, params string[] parameters)
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

            using var response = HttpClientRedirectHelper.Send(_client, absUri, method, configureRequest);

            uri = response.RequestMessage?.RequestUri ?? uri;
            responseHeaders = response.Headers;
            return ParseXmlResponse(response);
        }

        /// <summary>
        /// Performs an HTTP PUT with the specified XML document as the request body.
        /// </summary>
        public XmlDocument Put(ref Uri uri, string etag, HttpClientRedirectHelper.RequestConfigurator configureRequest, string contentType, XmlDocument doc, string encoding, bool ignoreResponse)
        {
            return Put(ref uri, etag, configureRequest, contentType, doc, encoding, ignoreResponse, out _);
        }

        /// <summary>
        /// Performs an HTTP PUT with the specified XML document as the request body.
        /// </summary>
        public XmlDocument Put(ref Uri uri, string etag, HttpClientRedirectHelper.RequestConfigurator configureRequest, string contentType, XmlDocument doc, string encoding, bool ignoreResponse, out HttpResponseHeaders responseHeaders)
        {
            return Send(HttpMethod.Put, ref uri, etag, configureRequest, contentType, doc, encoding, ignoreResponse, out responseHeaders);
        }

        /// <summary>
        /// Performs an HTTP POST with the specified XML document as the request body.
        /// </summary>
        public XmlDocument Post(ref Uri uri, HttpClientRedirectHelper.RequestConfigurator configureRequest, string contentType, XmlDocument doc, string encoding)
        {
            return Post(ref uri, configureRequest, contentType, doc, encoding, out _);
        }

        /// <summary>
        /// Performs an HTTP POST with the specified XML document as the request body.
        /// </summary>
        public XmlDocument Post(ref Uri uri, HttpClientRedirectHelper.RequestConfigurator configureRequest, string contentType, XmlDocument doc, string encoding, out HttpResponseHeaders responseHeaders)
        {
            return Send(HttpMethod.Post, ref uri, null, configureRequest, contentType, doc, encoding, false, out responseHeaders);
        }

        protected virtual XmlDocument Send(HttpMethod method, ref Uri uri, string etag, HttpClientRedirectHelper.RequestConfigurator configureRequest, string contentType, XmlDocument doc, string encoding, bool ignoreResponse, out HttpResponseHeaders responseHeaders)
        {
            string absUri = UrlHelper.SafeToAbsoluteUri(uri);

            if (ApplicationDiagnostics.VerboseLogging)
                Debug.WriteLine("XML Request to " + absUri + ":\r\n" + doc.InnerXml);

            // Select the encoding
            Encoding encodingToUse = new UTF8Encoding(false, false);
            try
            {
                encodingToUse = StringHelper.GetEncoding(encoding, encodingToUse);
            }
            catch (Exception ex)
            {
                Trace.Fail("Error while getting transport encoding: " + ex.ToString());
            }

            // Serialize the XML document
            using var ms = new MemoryStream();
            using (var writer = new XmlTextWriter(ms, encodingToUse) { Formatting = Formatting.Indented, Indentation = 1, IndentChar = ' ' })
            {
                writer.WriteStartDocument();
                doc.DocumentElement.WriteTo(writer);
            }

            var content = new ByteArrayContent(ms.ToArray());
            if (!string.IsNullOrEmpty(contentType))
            {
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            }

            using var response = HttpClientRedirectHelper.Send(_client, absUri, method, request =>
            {
                request.Content = content;
                if (!string.IsNullOrEmpty(etag))
                    request.Headers.TryAddWithoutValidation("If-match", etag);
                configureRequest?.Invoke(request);
            });

            responseHeaders = response.Headers;
            uri = response.RequestMessage?.RequestUri ?? uri;

            if (ignoreResponse || response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            return ParseXmlResponse(response);
        }

        /// <summary>
        /// Parses an XML response from an HttpResponseMessage.
        /// </summary>
        public static XmlDocument ParseXmlResponse(HttpResponseMessage response)
        {
            using var stream = response.Content.ReadAsStream();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);

            if (ApplicationDiagnostics.VerboseLogging)
            {
                try
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Trace.WriteLine("XML REST response:\r\n" + 
                        (response.RequestMessage?.RequestUri?.AbsoluteUri ?? "unknown") + "\r\n" + 
                        new StreamReader(ms, Encoding.UTF8).ReadToEnd());
                }
                catch (Exception e)
                {
                    Trace.TraceWarning("Failed to log REST response: " + e.ToString());
                }
            }

            ms.Seek(0, SeekOrigin.Begin);
            if (ms.Length == 0)
                return null;

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(ms);

                if (response.RequestMessage?.RequestUri != null)
                {
                    XmlHelper.ApplyBaseUri(xmlDoc, response.RequestMessage.RequestUri);
                }

                return xmlDoc;
            }
            catch (Exception e)
            {
                Trace.TraceWarning("Malformed XML document: " + e.ToString());
                return null;
            }
        }
    }
}
