// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using OpenLiveWriter.BlogClient.Clients;

namespace OpenLiveWriter.UnitTest.BlogClient
{
    [TestFixture]
    public class HttpClientXmlRestRequestHelperTest
    {
        private const string TestXmlResponse = @"<?xml version=""1.0"" encoding=""utf-8""?>
<entry xmlns=""http://www.w3.org/2005/Atom"">
  <id>test-id-123</id>
  <title>Test Entry</title>
</entry>";

        /// <summary>
        /// Tests GET request returns parsed XML document.
        /// </summary>
        [Test]
        public void TestGetReturnsXmlDocument()
        {
            // Arrange
            var handler = new MockHttpMessageHandler(request =>
            {
                Assert.AreEqual(HttpMethod.Get, request.Method);
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.RequestMessage = request;
                response.Content = new StringContent(TestXmlResponse, Encoding.UTF8, "application/xml");
                return response;
            });

            using var client = CreateNoRedirectClient(handler);
            var helper = new HttpClientXmlRestRequestHelper(client);
            var uri = new Uri("https://example.com/feed");

            // Act
            var doc = helper.Get(ref uri, null);

            // Assert
            Assert.IsNotNull(doc);
            Assert.IsNotNull(doc.DocumentElement);
            Assert.AreEqual("entry", doc.DocumentElement.LocalName);
        }

        /// <summary>
        /// Tests GET with query parameters.
        /// </summary>
        [Test]
        public void TestGetWithQueryParameters()
        {
            // Arrange
            string requestedUri = null;
            var handler = new MockHttpMessageHandler(request =>
            {
                requestedUri = request.RequestUri.ToString();
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.RequestMessage = request;
                response.Content = new StringContent(TestXmlResponse, Encoding.UTF8, "application/xml");
                return response;
            });

            using var client = CreateNoRedirectClient(handler);
            var helper = new HttpClientXmlRestRequestHelper(client);
            var uri = new Uri("https://example.com/feed");

            // Act
            var doc = helper.Get(ref uri, null, "page", "2", "size", "10");

            // Assert
            Assert.IsNotNull(doc);
            Assert.IsTrue(requestedUri.Contains("page=2"));
            Assert.IsTrue(requestedUri.Contains("size=10"));
        }

        /// <summary>
        /// Tests POST sends XML document.
        /// </summary>
        [Test]
        public void TestPostSendsXmlDocument()
        {
            // Arrange
            string receivedContent = null;
            string receivedContentType = null;
            var handler = new MockHttpMessageHandler(async request =>
            {
                Assert.AreEqual(HttpMethod.Post, request.Method);
                receivedContent = await request.Content.ReadAsStringAsync();
                receivedContentType = request.Content.Headers.ContentType?.ToString();
                
                var response = new HttpResponseMessage(HttpStatusCode.Created);
                response.RequestMessage = request;
                response.Content = new StringContent(TestXmlResponse, Encoding.UTF8, "application/xml");
                return response;
            });

            using var client = CreateNoRedirectClient(handler);
            var helper = new HttpClientXmlRestRequestHelper(client);
            var uri = new Uri("https://example.com/entries");

            var postDoc = new XmlDocument();
            postDoc.LoadXml(@"<entry><title>New Post</title></entry>");

            // Act
            var responseDoc = helper.Post(ref uri, null, "application/atom+xml;type=entry", postDoc, "utf-8");

            // Assert
            Assert.IsNotNull(responseDoc);
            Assert.IsNotNull(receivedContent);
            Assert.IsTrue(receivedContent.Contains("<title>New Post</title>"));
            Assert.IsTrue(receivedContentType.Contains("application/atom+xml"));
        }

        /// <summary>
        /// Tests PUT with etag header.
        /// </summary>
        [Test]
        public void TestPutWithEtag()
        {
            // Arrange
            string receivedEtag = null;
            var handler = new MockHttpMessageHandler(request =>
            {
                Assert.AreEqual(HttpMethod.Put, request.Method);
                if (request.Headers.TryGetValues("If-match", out var etags))
                {
                    receivedEtag = string.Join(",", etags);
                }
                
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.RequestMessage = request;
                response.Content = new StringContent(TestXmlResponse, Encoding.UTF8, "application/xml");
                return response;
            });

            using var client = CreateNoRedirectClient(handler);
            var helper = new HttpClientXmlRestRequestHelper(client);
            var uri = new Uri("https://example.com/entry/123");

            var putDoc = new XmlDocument();
            putDoc.LoadXml(@"<entry><title>Updated</title></entry>");

            // Act
            var responseDoc = helper.Put(ref uri, "\"abc123\"", null, "application/atom+xml", putDoc, "utf-8", false);

            // Assert
            Assert.AreEqual("\"abc123\"", receivedEtag);
        }

        /// <summary>
        /// Tests PUT with ignoreResponse=true returns null.
        /// </summary>
        [Test]
        public void TestPutIgnoreResponse()
        {
            // Arrange
            var handler = new MockHttpMessageHandler(request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.RequestMessage = request;
                response.Content = new StringContent(TestXmlResponse, Encoding.UTF8, "application/xml");
                return response;
            });

            using var client = CreateNoRedirectClient(handler);
            var helper = new HttpClientXmlRestRequestHelper(client);
            var uri = new Uri("https://example.com/entry/123");

            var putDoc = new XmlDocument();
            putDoc.LoadXml(@"<entry><title>Updated</title></entry>");

            // Act
            var responseDoc = helper.Put(ref uri, null, null, "application/atom+xml", putDoc, "utf-8", ignoreResponse: true);

            // Assert
            Assert.IsNull(responseDoc);
        }

        /// <summary>
        /// Tests DELETE returns null for NoContent response.
        /// </summary>
        [Test]
        public void TestDeleteNoContent()
        {
            // Arrange
            var handler = new MockHttpMessageHandler(request =>
            {
                Assert.AreEqual(HttpMethod.Delete, request.Method);
                var response = new HttpResponseMessage(HttpStatusCode.NoContent);
                response.RequestMessage = request;
                return response;
            });

            using var client = CreateNoRedirectClient(handler);
            var helper = new HttpClientXmlRestRequestHelper(client);
            var uri = new Uri("https://example.com/entry/123");

            // Act
            var responseDoc = helper.Delete(uri, null, out _);

            // Assert
            Assert.IsNull(responseDoc);
        }

        /// <summary>
        /// Tests response headers are returned.
        /// </summary>
        [Test]
        public void TestResponseHeadersReturned()
        {
            // Arrange
            var handler = new MockHttpMessageHandler(request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.RequestMessage = request;
                response.Headers.ETag = new EntityTagHeaderValue("\"test-etag\"");
                response.Headers.Add("X-Custom-Header", "CustomValue");
                response.Content = new StringContent(TestXmlResponse, Encoding.UTF8, "application/xml");
                return response;
            });

            using var client = CreateNoRedirectClient(handler);
            var helper = new HttpClientXmlRestRequestHelper(client);
            var uri = new Uri("https://example.com/entry");

            // Act
            helper.Get(ref uri, null, out var responseHeaders);

            // Assert
            Assert.IsNotNull(responseHeaders);
            Assert.IsNotNull(responseHeaders.ETag);
            Assert.AreEqual("\"test-etag\"", responseHeaders.ETag.Tag);
        }

        /// <summary>
        /// Tests request configurator is called.
        /// </summary>
        [Test]
        public void TestRequestConfigurator()
        {
            // Arrange
            bool hasAuthHeader = false;
            var handler = new MockHttpMessageHandler(request =>
            {
                hasAuthHeader = request.Headers.Contains("Authorization");
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.RequestMessage = request;
                response.Content = new StringContent(TestXmlResponse, Encoding.UTF8, "application/xml");
                return response;
            });

            using var client = CreateNoRedirectClient(handler);
            var helper = new HttpClientXmlRestRequestHelper(client);
            var uri = new Uri("https://example.com/feed");

            // Act
            var doc = helper.Get(ref uri, request =>
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", "dXNlcjpwYXNz");
            });

            // Assert
            Assert.IsTrue(hasAuthHeader);
        }

        /// <summary>
        /// Tests empty response returns null document.
        /// </summary>
        [Test]
        public void TestEmptyResponseReturnsNull()
        {
            // Arrange
            var handler = new MockHttpMessageHandler(request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.RequestMessage = request;
                response.Content = new StringContent("", Encoding.UTF8, "application/xml");
                return response;
            });

            using var client = CreateNoRedirectClient(handler);
            var helper = new HttpClientXmlRestRequestHelper(client);
            var uri = new Uri("https://example.com/feed");

            // Act
            var doc = helper.Get(ref uri, null);

            // Assert
            Assert.IsNull(doc);
        }

        /// <summary>
        /// Tests malformed XML returns null document gracefully.
        /// </summary>
        [Test]
        public void TestMalformedXmlReturnsNull()
        {
            // Arrange
            var handler = new MockHttpMessageHandler(request =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.RequestMessage = request;
                response.Content = new StringContent("<invalid>xml<", Encoding.UTF8, "application/xml");
                return response;
            });

            using var client = CreateNoRedirectClient(handler);
            var helper = new HttpClientXmlRestRequestHelper(client);
            var uri = new Uri("https://example.com/feed");

            // Act
            var doc = helper.Get(ref uri, null);

            // Assert
            Assert.IsNull(doc);
        }

        /// <summary>
        /// Creates an HttpClient that doesn't auto-redirect.
        /// </summary>
        private static HttpClient CreateNoRedirectClient(HttpMessageHandler handler)
        {
            return new HttpClient(handler);
        }

        /// <summary>
        /// Mock HttpMessageHandler for testing.
        /// </summary>
        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

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
                return _handler(request);
            }
        }
    }
}
