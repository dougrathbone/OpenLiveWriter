// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenLiveWriter.BlogClient.Clients;
using OpenLiveWriter.Extensibility.BlogClient;

namespace OpenLiveWriter.UnitTest.BlogClient
{
    [TestFixture]
    public class HttpClientRedirectHelperTest
    {
        /// <summary>
        /// Tests that a simple GET request without redirects works correctly.
        /// </summary>
        [Test]
        public void TestSimpleGetNoRedirect()
        {
            // Arrange
            var handler = new MockHttpMessageHandler(request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.RequestMessage = request;
                response.Content = new StringContent("<html>Test</html>");
                return response;
            });

            using var client = new HttpClient(handler);

            // Act
            using var response = HttpClientRedirectHelper.Get(client, "https://example.com/page");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("https://example.com/page", handler.LastRequestUri);
        }

        /// <summary>
        /// Tests that a 301 redirect is followed correctly.
        /// </summary>
        [Test]
        public void TestSingleRedirect()
        {
            // Arrange
            int requestCount = 0;
            var handler = new MockHttpMessageHandler(request =>
            {
                requestCount++;
                if (requestCount == 1)
                {
                    // First request - return redirect
                    var redirectResponse = new HttpResponseMessage(HttpStatusCode.MovedPermanently);
                    redirectResponse.RequestMessage = request;
                    redirectResponse.Headers.Location = new Uri("https://example.com/new-page");
                    return redirectResponse;
                }
                else
                {
                    // Second request - return success
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.RequestMessage = request;
                    response.Content = new StringContent("<html>Redirected</html>");
                    return response;
                }
            });

            using var client = new HttpClient(handler);

            // Act
            using var response = HttpClientRedirectHelper.Get(client, "https://example.com/old-page");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(2, requestCount);
            Assert.AreEqual("https://example.com/new-page", handler.LastRequestUri);
        }

        /// <summary>
        /// Tests that query strings are preserved through redirects when the redirect location doesn't have one.
        /// </summary>
        [Test]
        public void TestQueryStringPreservation()
        {
            // Arrange
            int requestCount = 0;
            string lastUri = null;
            var handler = new MockHttpMessageHandler(request =>
            {
                requestCount++;
                lastUri = request.RequestUri.ToString();

                if (requestCount == 1)
                {
                    // Redirect without query string - original query should be preserved
                    var redirectResponse = new HttpResponseMessage(HttpStatusCode.Found);
                    redirectResponse.RequestMessage = request;
                    redirectResponse.Headers.Location = new Uri("https://example.com/new-page");
                    return redirectResponse;
                }
                else
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.RequestMessage = request;
                    return response;
                }
            });

            using var client = new HttpClient(handler);

            // Act
            using var response = HttpClientRedirectHelper.Get(client, "https://example.com/old-page?key=value");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(lastUri.Contains("?key=value"), "Query string should be preserved");
        }

        /// <summary>
        /// Tests that an exception is thrown when redirect Location header is missing.
        /// </summary>
        [Test]
        public void TestMissingLocationHeader()
        {
            // Arrange
            var handler = new MockHttpMessageHandler(request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.MovedPermanently);
                response.RequestMessage = request;
                // No Location header set
                return response;
            });

            using var client = new HttpClient(handler);

            // Act & Assert
            Assert.Throws<BlogClientInvalidServerResponseException>(() =>
            {
                using var response = HttpClientRedirectHelper.Get(client, "https://example.com/page");
            });
        }

        /// <summary>
        /// Tests that an exception is thrown when too many redirects occur.
        /// </summary>
        [Test]
        public void TestMaxRedirectsExceeded()
        {
            // Arrange
            int requestCount = 0;
            var handler = new MockHttpMessageHandler(request =>
            {
                requestCount++;
                // Always redirect
                var response = new HttpResponseMessage(HttpStatusCode.Found);
                response.RequestMessage = request;
                response.Headers.Location = new Uri($"https://example.com/redirect-{requestCount}");
                return response;
            });

            using var client = new HttpClient(handler);

            // Act & Assert
            var ex = Assert.Throws<BlogClientInvalidServerResponseException>(() =>
            {
                using var response = HttpClientRedirectHelper.Get(client, "https://example.com/start");
            });

            Assert.IsTrue(ex.Message.Contains("50") || ex.ToString().Contains("50"), "Should mention redirect limit");
        }

        /// <summary>
        /// Tests that the request configurator is called for each request.
        /// </summary>
        [Test]
        public void TestRequestConfigurator()
        {
            // Arrange
            int configuratorCallCount = 0;
            var handler = new MockHttpMessageHandler(request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.RequestMessage = request;
                // Verify custom header was added
                Assert.IsTrue(request.Headers.Contains("X-Custom-Header"));
                return response;
            });

            using var client = new HttpClient(handler);

            // Act
            using var response = HttpClientRedirectHelper.Get(client, "https://example.com/page", request =>
            {
                configuratorCallCount++;
                request.Headers.Add("X-Custom-Header", "TestValue");
            });

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(1, configuratorCallCount);
        }

        /// <summary>
        /// Tests POST method with content.
        /// </summary>
        [Test]
        public void TestPostWithContent()
        {
            // Arrange
            string receivedContent = null;
            var handler = new MockHttpMessageHandler(async request =>
            {
                Assert.AreEqual(HttpMethod.Post, request.Method);
                receivedContent = await request.Content.ReadAsStringAsync();
                var response = new HttpResponseMessage(HttpStatusCode.Created);
                response.RequestMessage = request;
                return response;
            });

            using var client = new HttpClient(handler);
            var content = new StringContent("<entry>Test</entry>", System.Text.Encoding.UTF8, "application/xml");

            // Act
            using var response = HttpClientRedirectHelper.Post(client, "https://example.com/entries", content);

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            Assert.AreEqual("<entry>Test</entry>", receivedContent);
        }

        /// <summary>
        /// Tests DELETE method.
        /// </summary>
        [Test]
        public void TestDelete()
        {
            // Arrange
            var handler = new MockHttpMessageHandler(request =>
            {
                Assert.AreEqual(HttpMethod.Delete, request.Method);
                var response = new HttpResponseMessage(HttpStatusCode.NoContent);
                response.RequestMessage = request;
                return response;
            });

            using var client = new HttpClient(handler);

            // Act
            using var response = HttpClientRedirectHelper.Delete(client, "https://example.com/entry/123");

            // Assert
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        }

        /// <summary>
        /// Tests relative redirect URI resolution.
        /// </summary>
        [Test]
        public void TestRelativeRedirect()
        {
            // Arrange
            int requestCount = 0;
            var handler = new MockHttpMessageHandler(request =>
            {
                requestCount++;
                if (requestCount == 1)
                {
                    // Redirect with relative URI
                    var redirectResponse = new HttpResponseMessage(HttpStatusCode.Found);
                    redirectResponse.RequestMessage = request;
                    redirectResponse.Headers.Location = new Uri("/other-page", UriKind.Relative);
                    return redirectResponse;
                }
                else
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.RequestMessage = request;
                    return response;
                }
            });

            using var client = new HttpClient(handler);

            // Act
            using var response = HttpClientRedirectHelper.Get(client, "https://example.com/start");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("https://example.com/other-page", handler.LastRequestUri);
        }

        /// <summary>
        /// Mock HttpMessageHandler for testing.
        /// </summary>
        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

            public string LastRequestUri { get; private set; }

            public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            {
                _handler = request => Task.FromResult(handler(request));
            }

            public MockHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequestUri = request.RequestUri.AbsoluteUri;
                return _handler(request);
            }
        }
    }
}
