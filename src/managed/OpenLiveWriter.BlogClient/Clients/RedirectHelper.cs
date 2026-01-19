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
    /// Now uses HttpClient internally for modern .NET compatibility.
    /// </summary>
    public class RedirectHelper
    {
        private const int MaxRedirects = 50;

        public delegate HttpWebRequest RequestFactory(string uri);

        /// <summary>
        /// Sends a request following redirects. Returns an HttpResponseMessageWrapper
        /// that provides HttpWebResponse-like properties for backward compatibility.
        /// </summary>
        public static HttpResponseMessageWrapper GetResponse(string initialUri, RequestFactory requestFactory)
        {
            // Extract method and filter from the factory by creating a test request
            var testRequest = requestFactory(initialUri);
            string method = testRequest.Method;

            // Build an Action that configures HttpRequestMessage the same way
            Action<HttpRequestMessage> configureRequest = request =>
            {
                // Copy headers from the factory's request configuration
                var factoryRequest = requestFactory(request.RequestUri.AbsoluteUri);

                foreach (string headerName in factoryRequest.Headers.AllKeys)
                {
                    string headerValue = factoryRequest.Headers[headerName];
                    if (!request.Headers.TryAddWithoutValidation(headerName, headerValue))
                    {
                        // Try adding to content headers if it's a content header
                        if (request.Content != null)
                            request.Content.Headers.TryAddWithoutValidation(headerName, headerValue);
                    }
                }

                // Copy credentials if present
                if (factoryRequest.Credentials != null)
                {
                    // Credentials are handled by HttpClientService based on settings
                }

                // Copy content type
                if (request.Content != null && !string.IsNullOrEmpty(factoryRequest.ContentType))
                {
                    request.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(factoryRequest.ContentType);
                }
            };

            string uri = initialUri;
            for (int i = 0; i < MaxRedirects; i++)
            {
                using var request = new HttpRequestMessage(new HttpMethod(method), uri);
                configureRequest(request);

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

                return new HttpResponseMessageWrapper(response, new Uri(uri));
            }

            throw new BlogClientInvalidServerResponseException(initialUri,
                $"Allowed number of redirects ({MaxRedirects}) was exceeded", string.Empty);
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
    /// Wrapper around HttpResponseMessage that provides HttpWebResponse-compatible properties.
    /// This allows gradual migration from HttpWebResponse to HttpResponseMessage.
    /// </summary>
    public class HttpResponseMessageWrapper : IDisposable
    {
        private readonly HttpResponseMessage _response;
        private readonly Uri _responseUri;
        private WebHeaderCollection _headers;
        private Stream _responseStream;

        public HttpResponseMessageWrapper(HttpResponseMessage response, Uri responseUri)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
            _responseUri = responseUri ?? response.RequestMessage?.RequestUri;
        }

        /// <summary>
        /// Gets the final URI after following redirects.
        /// </summary>
        public Uri ResponseUri => _responseUri;

        /// <summary>
        /// Gets the HTTP status code.
        /// </summary>
        public HttpStatusCode StatusCode => _response.StatusCode;

        /// <summary>
        /// Gets the status description (reason phrase).
        /// </summary>
        public string StatusDescription => _response.ReasonPhrase ?? _response.StatusCode.ToString();

        /// <summary>
        /// Gets the response headers as a WebHeaderCollection for backward compatibility.
        /// </summary>
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

        /// <summary>
        /// Gets the response stream.
        /// </summary>
        public Stream GetResponseStream()
        {
            if (_responseStream == null)
            {
                _responseStream = _response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
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
            _response?.Dispose();
        }

        /// <summary>
        /// Implicit conversion to allow backward compatibility with code expecting HttpWebResponse patterns.
        /// </summary>
        public static implicit operator HttpResponseMessage(HttpResponseMessageWrapper wrapper)
        {
            return wrapper._response;
        }
    }
}
