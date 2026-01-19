// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OpenLiveWriter.CoreServices;
using OpenLiveWriter.Extensibility.BlogClient;

namespace OpenLiveWriter.BlogClient.Clients
{
    /// <summary>
    /// Helper class for following HTTP redirects.
    /// Uses HttpWebRequest internally to support legacy request factories.
    /// </summary>
    public class RedirectHelper
    {
        private const int MaxRedirects = 50;

        public delegate HttpWebRequest RequestFactory(string uri);

        /// <summary>
        /// Sends a request following redirects. Returns an HttpResponseMessageWrapper
        /// that provides HttpWebResponse-like properties for backward compatibility.
        /// </summary>
        /// <remarks>
        /// This method uses HttpWebRequest internally to support legacy request factories
        /// that write content to the request stream. The SYSLIB0014 suppression is localized here.
        /// </remarks>
        #pragma warning disable SYSLIB0014 // WebRequest is obsolete
        public static HttpResponseMessageWrapper GetResponse(string initialUri, RequestFactory requestFactory)
        {
            string uri = initialUri;
            for (int i = 0; i < MaxRedirects; i++)
            {
                HttpWebRequest request = requestFactory(uri);
                request.AllowAutoRedirect = false;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                int statusCode = (int)response.StatusCode;
                if (statusCode >= 300 && statusCode < 400)
                {
                    string redirectedLocation = response.Headers["Location"];
                    if (string.IsNullOrEmpty(redirectedLocation))
                    {
                        response.Close();
                        throw new BlogClientInvalidServerResponseException(initialUri,
                            "An invalid redirect was returned (Location header was expected but not found)", string.Empty);
                    }
                    uri = MergeUris(uri, redirectedLocation);
                    response.Close();
                    continue;
                }

                // Wrap the HttpWebResponse in our wrapper for unified interface
                return new HttpResponseMessageWrapper(response);
            }

            throw new BlogClientInvalidServerResponseException(initialUri,
                $"Allowed number of redirects ({MaxRedirects}) was exceeded", string.Empty);
        }
        #pragma warning restore SYSLIB0014

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

        /// <summary>
        /// Simple request factory for basic GET/DELETE/etc requests.
        /// </summary>
        public class SimpleRequest
        {
            private readonly string _method;
            private readonly HttpRequestFilter _filter;

            public SimpleRequest(string method, HttpRequestFilter filter)
            {
                _method = method;
                _filter = filter;
            }

            public HttpWebRequest Create(string uri)
            {
                HttpWebRequest request = HttpRequestHelper.CreateHttpWebRequest(uri, false);
                request.Method = _method;
                if (_filter != null)
                    _filter(request);
                return request;
            }
        }
    }

    /// <summary>
    /// Wrapper that provides a unified interface for HTTP responses.
    /// Supports both HttpWebResponse (legacy) and HttpResponseMessage (modern).
    /// </summary>
    public class HttpResponseMessageWrapper : IDisposable
    {
        private readonly HttpWebResponse _webResponse;
        private readonly HttpResponseMessage _httpResponse;
        private WebHeaderCollection _headers;
        private Stream _responseStream;

        /// <summary>
        /// Creates a wrapper from an HttpWebResponse (legacy path).
        /// </summary>
        public HttpResponseMessageWrapper(HttpWebResponse response)
        {
            _webResponse = response ?? throw new ArgumentNullException(nameof(response));
        }

        /// <summary>
        /// Creates a wrapper from an HttpResponseMessage (modern path).
        /// </summary>
        public HttpResponseMessageWrapper(HttpResponseMessage response, Uri responseUri = null)
        {
            _httpResponse = response ?? throw new ArgumentNullException(nameof(response));
        }

        /// <summary>
        /// Gets the final URI after following redirects.
        /// </summary>
        public Uri ResponseUri => _webResponse?.ResponseUri ?? _httpResponse?.RequestMessage?.RequestUri;

        /// <summary>
        /// Gets the HTTP status code.
        /// </summary>
        public HttpStatusCode StatusCode => _webResponse?.StatusCode ?? _httpResponse.StatusCode;

        /// <summary>
        /// Gets the status description (reason phrase).
        /// </summary>
        public string StatusDescription => _webResponse?.StatusDescription ?? _httpResponse?.ReasonPhrase ?? StatusCode.ToString();

        /// <summary>
        /// Gets the response headers as a WebHeaderCollection for backward compatibility.
        /// </summary>
        public WebHeaderCollection Headers
        {
            get
            {
                if (_headers == null)
                {
                    if (_webResponse != null)
                    {
                        _headers = _webResponse.Headers;
                    }
                    else
                    {
                        _headers = new WebHeaderCollection();
                        foreach (var header in _httpResponse.Headers)
                        {
                            foreach (var value in header.Value)
                            {
                                _headers.Add(header.Key, value);
                            }
                        }
                        if (_httpResponse.Content != null)
                        {
                            foreach (var header in _httpResponse.Content.Headers)
                            {
                                foreach (var value in header.Value)
                                {
                                    _headers.Add(header.Key, value);
                                }
                            }
                        }
                    }
                }
                return _headers;
            }
        }

        /// <summary>
        /// Gets the response stream.
        /// </summary>
        public Stream GetResponseStream()
        {
            if (_responseStream == null)
            {
                if (_webResponse != null)
                {
                    _responseStream = _webResponse.GetResponseStream();
                }
                else
                {
                    _responseStream = _httpResponse.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                }
            }
            return _responseStream;
        }

        /// <summary>
        /// Closes the response and releases resources.
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            _responseStream?.Dispose();
            _webResponse?.Close();
            _httpResponse?.Dispose();
        }
    }
}
