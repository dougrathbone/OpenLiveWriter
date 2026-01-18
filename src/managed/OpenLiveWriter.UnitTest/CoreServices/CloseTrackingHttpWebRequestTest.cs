// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.IO;
using System.Net;
using NUnit.Framework;
using OpenLiveWriter.CoreServices;

namespace OpenLiveWriter.UnitTest.CoreServices
{
    /// <summary>
    /// Tests for the close tracking functionality in HttpRequestHelper.
    /// These tests verify the public API for tracking HTTP response disposal.
    /// </summary>
    [TestFixture]
    public class CloseTrackingHttpWebRequestTest
    {
        /// <summary>
        /// Tests HttpRequestHelper.TrackResponse returns a valid tracker for null response.
        /// </summary>
        [Test]
        public void TestTrackResponseWithNullReturnsClosedTracker()
        {
            // Act
            var tracker = HttpRequestHelper.TrackResponse(null);

            // Assert
            Assert.IsNotNull(tracker, "TrackResponse should return a non-null tracker");
            Assert.IsTrue(tracker.IsClosed, "Tracker for null response should be marked as closed");
        }

        /// <summary>
        /// Tests that calling MarkClosed on a tracker is safe and idempotent.
        /// </summary>
        [Test]
        public void TestTrackerMarkClosedIdempotent()
        {
            // Arrange
            var tracker = HttpRequestHelper.TrackResponse(null);

            // Act - calling MarkClosed multiple times should be safe
            tracker.MarkClosed();
            tracker.MarkClosed();
            tracker.MarkClosed();

            // Assert
            Assert.IsTrue(tracker.IsClosed);
        }

        /// <summary>
        /// Tests that TrackResponseClosing method exists and doesn't throw.
        /// </summary>
        [Test]
        public void TestTrackResponseClosingApiCompatibility()
        {
            // Arrange
            HttpWebRequest request = null;

            // Act - should not throw
            HttpRequestHelper.TrackResponseClosing(ref request);

            // Assert - request should still be null (no proxying in new implementation)
            Assert.IsNull(request);
        }

        /// <summary>
        /// Tests that GetTrackedResponseStream works with a real HTTP response.
        /// </summary>
        [Test]
        public void TestGetTrackedResponseStreamWithRealResponse()
        {
            try
            {
                // Arrange - This requires network - skip if not available
                var request = HttpRequestHelper.CreateHttpWebRequest("https://httpbin.org/get", false);
                using var response = (HttpWebResponse)request.GetResponse();

                // Act
                using var trackedStream = HttpRequestHelper.GetTrackedResponseStream(response);

                // Assert
                Assert.IsNotNull(trackedStream, "GetTrackedResponseStream should return a non-null stream");
                Assert.IsTrue(trackedStream.CanRead, "Stream should be readable");

                // Read some data to verify the stream works
                using var reader = new StreamReader(trackedStream);
                string content = reader.ReadToEnd();
                Assert.IsTrue(content.Length > 0, "Should have read content from response");
                Assert.IsTrue(content.Contains("httpbin"), "Content should contain expected response data");
            }
            catch (WebException)
            {
                Assert.Ignore("Network not available for integration test");
            }
            catch (System.Net.Http.HttpRequestException)
            {
                Assert.Ignore("Network not available for integration test");
            }
            catch (TypeInitializationException)
            {
                Assert.Ignore("Application environment not initialized for integration test");
            }
        }

        /// <summary>
        /// Tests that ResponseCloseTracker.IsClosed property works correctly.
        /// </summary>
        [Test]
        public void TestResponseCloseTrackerIsClosedProperty()
        {
            // Arrange
            var tracker = HttpRequestHelper.TrackResponse(null);

            // Act & Assert - null response tracker is already closed
            Assert.IsTrue(tracker.IsClosed, "Tracker for null should report IsClosed as true");
        }

        /// <summary>
        /// Tests that GetTrackedResponseStream properly wraps the response stream.
        /// </summary>
        [Test]
        public void TestGetTrackedResponseStreamReturnsWorkingStream()
        {
            try
            {
                // Arrange
                var request = HttpRequestHelper.CreateHttpWebRequest("https://httpbin.org/bytes/100", false);
                using var response = (HttpWebResponse)request.GetResponse();

                // Act
                using var trackedStream = HttpRequestHelper.GetTrackedResponseStream(response);

                // Assert - verify stream operations work
                Assert.IsNotNull(trackedStream);
                Assert.IsTrue(trackedStream.CanRead);

                // Read the bytes
                byte[] buffer = new byte[100];
                int totalRead = 0;
                int bytesRead;
                while ((bytesRead = trackedStream.Read(buffer, totalRead, buffer.Length - totalRead)) > 0)
                {
                    totalRead += bytesRead;
                    if (totalRead >= buffer.Length) break;
                }

                Assert.IsTrue(totalRead > 0, "Should have read some bytes from the stream");
            }
            catch (WebException)
            {
                Assert.Ignore("Network not available for integration test");
            }
            catch (System.Net.Http.HttpRequestException)
            {
                Assert.Ignore("Network not available for integration test");
            }
            catch (TypeInitializationException)
            {
                Assert.Ignore("Application environment not initialized for integration test");
            }
        }

        /// <summary>
        /// Tests that the tracked stream can be disposed multiple times safely.
        /// </summary>
        [Test]
        public void TestTrackedStreamDisposeIdempotent()
        {
            try
            {
                // Arrange
                var request = HttpRequestHelper.CreateHttpWebRequest("https://httpbin.org/get", false);
                using var response = (HttpWebResponse)request.GetResponse();
                var trackedStream = HttpRequestHelper.GetTrackedResponseStream(response);

                // Act - dispose multiple times should be safe
                trackedStream.Dispose();
                trackedStream.Dispose();
                trackedStream.Dispose();

                // Assert - no exception thrown
                Assert.Pass("Disposing stream multiple times did not throw");
            }
            catch (WebException)
            {
                Assert.Ignore("Network not available for integration test");
            }
            catch (System.Net.Http.HttpRequestException)
            {
                Assert.Ignore("Network not available for integration test");
            }
            catch (TypeInitializationException)
            {
                Assert.Ignore("Application environment not initialized for integration test");
            }
        }
    }
}
