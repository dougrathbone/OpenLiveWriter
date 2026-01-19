// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Xml;
using OpenLiveWriter.CoreServices;
using OpenLiveWriter.Extensibility.BlogClient;

namespace OpenLiveWriter.BlogClient.Clients
{
    /// <summary>
    /// Helper class for following HTTP redirects using HttpClient.
    /// </summary>
    public class RedirectHelper
    {
        private const int MaxRedirects = 50;

        /// <summary>
        /// Delegate for configuring an HTTP request.
        /// </summary>
        public delegate void RequestConfigurator(HttpRequestMessage request);

        /// <summary>
        /// Sends a request following redirects using HttpClient.
        /// </summary>
        /// <param name="initialUri">The initial URI to request</param>
        /// <param name="method">The HTTP method</param>
        /// <param name="configureRequest">Optional callback to configure headers</param>
        /// <param name="createContent">Optional callback to create content (called for each redirect)</param>
        public static HttpResponseMessageWrapper GetResponse(
            string initialUri,
            HttpMethod method,
            RequestConfigurator configureRequest = null,
            Func<HttpContent> createContent = null)
        {
            string uri = initialUri;
            for (int i = 0; i < MaxRedirects; i++)
            {
                using var request = new HttpRequestMessage(method, uri);

                // Apply header configuration
                configureRequest?.Invoke(request);

                // Set content if provided
                if (createContent != null)
                {
                    request.Content = createContent();
                }

                var response = HttpClientService.DefaultClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();

                int statusCode = (int)response.StatusCode;
                if (statusCode >= 300 && statusCode < 400)
                {
                    string redirectedLocation = response.Headers.Location?.ToString();
                    if (string.IsNullOrEmpty(redirectedLocation))
                    {
                        response.Dispose();
                        throw new BlogClientInvalidServerResponseException(initialUri,
                            "An invalid redirect was returned (Location header was expected but not found)", string.Empty);
                    }
                    uri = MergeUris(uri, redirectedLocation);
                    response.Dispose();
                    continue;
                }

                return new HttpResponseMessageWrapper(response);
            }

            throw new BlogClientInvalidServerResponseException(initialUri,
                $"Allowed number of redirects ({MaxRedirects}) was exceeded", string.Empty);
        }

        /// <summary>
        /// Convenience overload for simple requests (GET, DELETE, etc.).
        /// </summary>
        public static HttpResponseMessageWrapper GetResponse(
            string uri,
            string method,
            RequestConfigurator configureRequest = null)
        {
            return GetResponse(uri, new HttpMethod(method), configureRequest, null);
        }

        private static string MergeUris(string uri, string newUri)
        {
            // If the new URI is absolute, use it directly
            if (Uri.TryCreate(newUri, UriKind.Absolute, out _))
            {
                int i1 = uri.IndexOf('?');
                int i2 = newUri.IndexOf('?');
                if (i1 >= 0 && i2 < 0)
                    return newUri + uri.Substring(i1);
                return newUri;
            }

            // Relative URI - resolve against original
            if (Uri.TryCreate(new Uri(uri), newUri, out Uri resolved))
            {
                return resolved.AbsoluteUri;
            }

            return newUri;
        }

        #region Content Helpers

        /// <summary>
        /// Creates XML content from an XmlDocument.
        /// </summary>
        public static HttpContent CreateXmlContent(XmlDocument doc, string contentType, Encoding encoding)
        {
            using var ms = new MemoryStream();
            using (var writer = new XmlTextWriter(ms, encoding))
            {
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 1;
                writer.IndentChar = ' ';
                writer.WriteStartDocument();
                doc.DocumentElement.WriteTo(writer);
                writer.Flush();
            }

            var content = new ByteArrayContent(ms.ToArray());
            if (!string.IsNullOrEmpty(contentType))
            {
                content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
            }
            return content;
        }

        /// <summary>
        /// Creates form URL-encoded content.
        /// </summary>
        public static HttpContent CreateFormContent(params (string name, string value)[] fields)
        {
            var sb = new StringBuilder();
            foreach (var (name, value) in fields)
            {
                if (sb.Length > 0) sb.Append('&');
                sb.Append(Uri.EscapeDataString(name));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(value ?? ""));
            }
            return new StringContent(sb.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
        }

        #endregion
    }

    /// <summary>
    /// Wrapper that provides a unified interface for HTTP responses.
    /// </summary>
    public class HttpResponseMessageWrapper : IDisposable
    {
        private readonly HttpResponseMessage _response;
        private WebHeaderCollection _headers;
        private Stream _responseStream;

        public HttpResponseMessageWrapper(HttpResponseMessage response)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
        }

        public Uri ResponseUri => _response.RequestMessage?.RequestUri;

        public HttpStatusCode StatusCode => _response.StatusCode;

        public string StatusDescription => _response.ReasonPhrase ?? StatusCode.ToString();

        public WebHeaderCollection Headers
        {
            get
            {
                if (_headers == null)
                {
                    _headers = new WebHeaderCollection();
                    foreach (var header in _response.Headers)
                    {
                        foreach (var value in header.Value)
                        {
                            _headers.Add(header.Key, value);
                        }
                    }
                    if (_response.Content != null)
                    {
                        foreach (var header in _response.Content.Headers)
                        {
                            foreach (var value in header.Value)
                            {
                                _headers.Add(header.Key, value);
                            }
                        }
                    }
                }
                return _headers;
            }
        }

        public Stream GetResponseStream()
        {
            if (_responseStream == null)
            {
                _responseStream = _response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            }
            return _responseStream;
        }

        public void Close() => Dispose();

        public void Dispose()
        {
            _responseStream?.Dispose();
            _response?.Dispose();
        }
    }
}
