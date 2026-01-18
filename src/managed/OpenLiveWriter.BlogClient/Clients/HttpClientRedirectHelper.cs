// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OpenLiveWriter.CoreServices;
using OpenLiveWriter.Extensibility.BlogClient;

namespace OpenLiveWriter.BlogClient.Clients
{
    /// <summary>
    /// Modern HttpClient-based redirect helper.
    /// Replaces RedirectHelper for new code.
    /// </summary>
    public class HttpClientRedirectHelper
    {
        private const int MaxRedirects = 50;

        /// <summary>
        /// Delegate for configuring an HttpRequestMessage before sending.
        /// </summary>
        public delegate void RequestConfigurator(HttpRequestMessage request);

        /// <summary>
        /// Sends an HTTP request, manually following redirects up to MaxRedirects times.
        /// </summary>
        /// <param name="client">The HttpClient to use (should have AllowAutoRedirect=false)</param>
        /// <param name="initialUri">The initial URI to request</param>
        /// <param name="method">The HTTP method to use</param>
        /// <param name="configureRequest">Optional callback to configure the request before sending</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The final HttpResponseMessage after following redirects</returns>
        public static async Task<HttpResponseMessage> SendAsync(
            HttpClient client,
            string initialUri,
            HttpMethod method,
            RequestConfigurator configureRequest = null,
            CancellationToken cancellationToken = default)
        {
            string uri = initialUri;

            for (int i = 0; i < MaxRedirects; i++)
            {
                using var request = new HttpRequestMessage(method, uri);
                configureRequest?.Invoke(request);

                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

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

                return response;
            }

            throw new BlogClientInvalidServerResponseException(initialUri,
                $"Allowed number of redirects ({MaxRedirects}) was exceeded", string.Empty);
        }

        /// <summary>
        /// Synchronous wrapper for SendAsync.
        /// </summary>
        public static HttpResponseMessage Send(
            HttpClient client,
            string initialUri,
            HttpMethod method,
            RequestConfigurator configureRequest = null)
        {
            return SendAsync(client, initialUri, method, configureRequest).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sends a GET request, following redirects.
        /// </summary>
        public static HttpResponseMessage Get(HttpClient client, string uri, RequestConfigurator configureRequest = null)
        {
            return Send(client, uri, HttpMethod.Get, configureRequest);
        }

        /// <summary>
        /// Sends a POST request, following redirects.
        /// </summary>
        public static HttpResponseMessage Post(HttpClient client, string uri, HttpContent content, RequestConfigurator configureRequest = null)
        {
            return Send(client, uri, HttpMethod.Post, request =>
            {
                request.Content = content;
                configureRequest?.Invoke(request);
            });
        }

        /// <summary>
        /// Sends a PUT request, following redirects.
        /// </summary>
        public static HttpResponseMessage Put(HttpClient client, string uri, HttpContent content, RequestConfigurator configureRequest = null)
        {
            return Send(client, uri, HttpMethod.Put, request =>
            {
                request.Content = content;
                configureRequest?.Invoke(request);
            });
        }

        /// <summary>
        /// Sends a DELETE request, following redirects.
        /// </summary>
        public static HttpResponseMessage Delete(HttpClient client, string uri, RequestConfigurator configureRequest = null)
        {
            return Send(client, uri, HttpMethod.Delete, configureRequest);
        }

        private static string MergeUris(string uri, string newUri)
        {
            // If the new URI is absolute, use it directly
            if (Uri.TryCreate(newUri, UriKind.Absolute, out _))
            {
                // Preserve query string from original URI if new URI doesn't have one
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
    }
}
