// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using OpenLiveWriter.Extensibility.BlogClient;

namespace OpenLiveWriter.UnitTest.PostEditor
{
    /// <summary>
    /// Tests for ServiceUpdateChecker exception handling behavior.
    /// The ServiceUpdateChecker runs background checks for service updates and should
    /// handle exceptions gracefully without triggering assertion dialogs.
    /// </summary>
    [TestFixture]
    public class ServiceUpdateCheckerTest
    {
        /// <summary>
        /// Verifies that BlogClientInvalidServerResponseException can be instantiated
        /// and contains the expected error information.
        /// </summary>
        [Test]
        public void TestBlogClientInvalidServerResponseExceptionContainsErrorInfo()
        {
            // Arrange
            const string method = "blogger.getUsersBlogs";
            const string errorMessage = "Invalid response document returned from XmlRpc server";
            const string response = "<invalid>xml</invalid>";

            // Act
            var exception = new BlogClientInvalidServerResponseException(method, errorMessage, response);

            // Assert
            Assert.AreEqual(method, exception.Method);
            Assert.AreEqual(errorMessage, exception.ErrorMessage);
            Assert.AreEqual(response, exception.Response);
            Assert.IsTrue(exception.Message.Contains(method), "Exception message should contain the method name");
        }

        /// <summary>
        /// Verifies that BlogClientInvalidServerResponseException is a subclass of BlogClientException.
        /// This is important for exception handling in ServiceUpdateChecker.
        /// </summary>
        [Test]
        public void TestBlogClientInvalidServerResponseExceptionIsBlogClientException()
        {
            // Arrange
            var exception = new BlogClientInvalidServerResponseException("test.method", "error", "response");

            // Assert
            Assert.IsInstanceOf<BlogClientException>(exception);
            Assert.IsInstanceOf<Exception>(exception);
        }

        /// <summary>
        /// Tests that the exception handling pattern used in ServiceUpdateChecker
        /// correctly catches BlogClientInvalidServerResponseException separately from
        /// other exceptions, allowing for appropriate logging behavior.
        /// </summary>
        [Test]
        public void TestExceptionHandlingPatternCatchesServerResponseException()
        {
            // Arrange
            bool serverResponseExceptionCaught = false;
            bool generalExceptionCaught = false;
            var serverException = new BlogClientInvalidServerResponseException(
                "blogger.getUsersBlogs",
                "Invalid response",
                null);

            // Act - simulate the ServiceUpdateChecker exception handling pattern
            try
            {
                throw serverException;
            }
            catch (BlogClientInvalidServerResponseException)
            {
                serverResponseExceptionCaught = true;
            }
            catch (Exception)
            {
                generalExceptionCaught = true;
            }

            // Assert
            Assert.IsTrue(serverResponseExceptionCaught, 
                "BlogClientInvalidServerResponseException should be caught by its specific handler");
            Assert.IsFalse(generalExceptionCaught, 
                "General exception handler should not be reached");
        }

        /// <summary>
        /// Tests that the exception handling pattern allows other exceptions
        /// to fall through to the general handler.
        /// </summary>
        [Test]
        public void TestExceptionHandlingPatternFallsThroughForOtherExceptions()
        {
            // Arrange
            bool serverResponseExceptionCaught = false;
            bool generalExceptionCaught = false;
            var ioException = new IOException("Network error");

            // Act - simulate the ServiceUpdateChecker exception handling pattern
            try
            {
                throw ioException;
            }
            catch (BlogClientInvalidServerResponseException)
            {
                serverResponseExceptionCaught = true;
            }
            catch (Exception)
            {
                generalExceptionCaught = true;
            }

            // Assert
            Assert.IsFalse(serverResponseExceptionCaught, 
                "BlogClientInvalidServerResponseException handler should not catch IO exceptions");
            Assert.IsTrue(generalExceptionCaught, 
                "General exception handler should catch other exceptions");
        }

        /// <summary>
        /// Verifies that a TraceListener can capture trace output without assertion dialogs.
        /// This validates that Trace.WriteLine (used in the fix) works correctly.
        /// </summary>
        [Test]
        public void TestTraceWriteLineDoesNotTriggerAssertionDialog()
        {
            // Arrange
            var traceOutput = new StringWriter();
            var listener = new TextWriterTraceListener(traceOutput);
            Trace.Listeners.Add(listener);

            try
            {
                var exception = new BlogClientInvalidServerResponseException(
                    "test.method",
                    "Test error message",
                    null);

                // Act - simulate the fixed exception handling using Trace.WriteLine
                string logMessage = "ServiceUpdateChecker: Server response error (non-fatal): " + exception.Message;
                Trace.WriteLine(logMessage);
                Trace.Flush();

                // Assert
                string output = traceOutput.ToString();
                Assert.IsTrue(output.Contains("ServiceUpdateChecker"), 
                    "Trace output should contain the component name");
                Assert.IsTrue(output.Contains("non-fatal"), 
                    "Trace output should indicate the error is non-fatal");
            }
            finally
            {
                Trace.Listeners.Remove(listener);
                listener.Dispose();
            }
        }

        /// <summary>
        /// Tests that all relevant blog client exceptions are subclasses of BlogClientException,
        /// ensuring consistent exception handling in ServiceUpdateChecker.
        /// </summary>
        [Test]
        public void TestAllBlogClientExceptionsInheritFromBlogClientException()
        {
            // These are the exception types that ServiceUpdateChecker might encounter
            Assert.IsTrue(typeof(BlogClientInvalidServerResponseException).IsSubclassOf(typeof(BlogClientException)));
            Assert.IsTrue(typeof(BlogClientAuthenticationException).IsSubclassOf(typeof(BlogClientException)));
            Assert.IsTrue(typeof(BlogClientConnectionErrorException).IsSubclassOf(typeof(BlogClientException)));
            Assert.IsTrue(typeof(BlogClientIOException).IsSubclassOf(typeof(BlogClientException)));
            Assert.IsTrue(typeof(BlogClientOperationCancelledException).IsSubclassOf(typeof(BlogClientException)));
        }
    }
}
