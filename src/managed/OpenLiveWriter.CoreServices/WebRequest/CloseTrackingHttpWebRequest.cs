// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace OpenLiveWriter.CoreServices
{
    /// <summary>
    /// Provides debug-time tracking of HTTP response lifecycle to detect resource leaks.
    /// This is a .NET 10 compatible implementation that replaces the original RealProxy-based version.
    /// 
    /// In DEBUG builds, this tracks HttpWebResponse objects and warns if they are garbage collected
    /// without being properly closed/disposed.
    /// </summary>
    internal static class CloseTrackingHttpWebRequest
    {
        // Track responses that haven't been closed yet
        private static readonly ConcurrentDictionary<int, ResponseTracker> _trackedResponses = new();
        private static int _nextTrackerId;

        /// <summary>
        /// Wraps an HttpWebRequest to track response closing in DEBUG builds.
        /// This method exists for API compatibility - the actual tracking happens when
        /// you call <see cref="TrackResponse"/> on the response.
        /// </summary>
        [Conditional("DEBUG")]
        public static void Wrap(ref HttpWebRequest request)
        {
            // In the new implementation, tracking is done at the response level.
            // This method is kept for API compatibility.
        }

        /// <summary>
        /// Wraps an HttpWebResponse for close tracking. Call this after GetResponse().
        /// Returns a wrapper that tracks disposal and warns if the response is not closed.
        /// </summary>
        /// <param name="response">The response to track.</param>
        /// <returns>A tracking wrapper around the response.</returns>
        [Conditional("DEBUG")]
        public static void TrackResponse(ref HttpWebResponse response)
        {
            if (response == null) return;
            response = new CloseTrackingResponse(response);
        }

        /// <summary>
        /// Gets the number of responses currently being tracked (for testing).
        /// </summary>
        internal static int TrackedResponseCount => _trackedResponses.Count;

        /// <summary>
        /// Clears all tracked responses (for testing).
        /// </summary>
        internal static void ClearTrackedResponses()
        {
            _trackedResponses.Clear();
        }

        internal static int RegisterResponse(string stackTrace)
        {
            int id = System.Threading.Interlocked.Increment(ref _nextTrackerId);
            _trackedResponses[id] = new ResponseTracker(stackTrace);
            return id;
        }

        internal static void UnregisterResponse(int id)
        {
            _trackedResponses.TryRemove(id, out _);
        }

        internal static string GetStackTrace(int id)
        {
            return _trackedResponses.TryGetValue(id, out var tracker) ? tracker.StackTrace : null;
        }

        private class ResponseTracker
        {
            public string StackTrace { get; }

            public ResponseTracker(string stackTrace)
            {
                StackTrace = stackTrace;
            }
        }
    }

    /// <summary>
    /// A wrapper around HttpWebResponse that tracks whether it was properly closed.
    /// If the wrapper is finalized without being closed, it logs a warning with the
    /// original stack trace.
    /// </summary>
    internal class CloseTrackingResponse : HttpWebResponse
    {
        private readonly HttpWebResponse _wrapped;
        private readonly int _trackerId;
        private bool _closed;

        // Store stack trace in unmanaged memory to survive until finalizer runs
        private IntPtr _pStackTrace;

        internal CloseTrackingResponse(HttpWebResponse wrapped) : base()
        {
            _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
            
            string stackTrace = Environment.StackTrace;
            _pStackTrace = Marshal.StringToCoTaskMemUni(stackTrace);
            _trackerId = CloseTrackingHttpWebRequest.RegisterResponse(stackTrace);
        }

        ~CloseTrackingResponse()
        {
            if (!_closed)
            {
                string stackTrace = _pStackTrace != IntPtr.Zero 
                    ? Marshal.PtrToStringUni(_pStackTrace) 
                    : "Stack trace unavailable";
                
                // Use Trace instead of Debug.Fail as Debug.Fail calls Environment.FailFast in .NET 10
                Trace.TraceWarning($"[CloseTrackingHttpWebRequest] Unclosed HttpWebResponse detected. " +
                    $"The response was obtained here:\r\n{stackTrace}");
            }

            FreeStackTrace();
            CloseTrackingHttpWebRequest.UnregisterResponse(_trackerId);
        }

        private void FreeStackTrace()
        {
            if (_pStackTrace != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(_pStackTrace);
                _pStackTrace = IntPtr.Zero;
            }
        }

        private void MarkClosed()
        {
            if (!_closed)
            {
                _closed = true;
                FreeStackTrace();
                CloseTrackingHttpWebRequest.UnregisterResponse(_trackerId);
                GC.SuppressFinalize(this);
            }
        }

        // Override all HttpWebResponse members to delegate to the wrapped response

        public override long ContentLength => _wrapped.ContentLength;
        public override string ContentType => _wrapped.ContentType;
        public override string ContentEncoding => _wrapped.ContentEncoding;
        public override string CharacterSet => _wrapped.CharacterSet;
        public override string Server => _wrapped.Server;
        public override DateTime LastModified => _wrapped.LastModified;
        public override HttpStatusCode StatusCode => _wrapped.StatusCode;
        public override string StatusDescription => _wrapped.StatusDescription;
        public override Version ProtocolVersion => _wrapped.ProtocolVersion;
        public override Uri ResponseUri => _wrapped.ResponseUri;
        public override string Method => _wrapped.Method;
        public override WebHeaderCollection Headers => _wrapped.Headers;
        public override bool SupportsHeaders => _wrapped.SupportsHeaders;
        public override CookieCollection Cookies
        {
            get => _wrapped.Cookies;
            set => _wrapped.Cookies = value;
        }
        public override bool IsMutuallyAuthenticated => _wrapped.IsMutuallyAuthenticated;

        public override Stream GetResponseStream()
        {
            var stream = _wrapped.GetResponseStream();
            if (stream != null)
            {
                return new CloseTrackingStream(stream, this);
            }
            return stream;
        }

        public override string GetResponseHeader(string headerName)
        {
            return _wrapped.GetResponseHeader(headerName);
        }

        public override void Close()
        {
            MarkClosed();
            _wrapped.Close();
        }

        protected override void Dispose(bool disposing)
        {
            MarkClosed();
            if (disposing)
            {
                _wrapped.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// A wrapper around Stream that notifies when the stream is closed/disposed.
    /// Closing/disposing the stream also marks the parent response as properly closed.
    /// </summary>
    internal class CloseTrackingStream : Stream
    {
        private readonly Stream _wrapped;
        private readonly CloseTrackingResponse _parentResponse;

        internal CloseTrackingStream(Stream wrapped, CloseTrackingResponse parentResponse)
        {
            _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
            _parentResponse = parentResponse;
        }

        public override bool CanRead => _wrapped.CanRead;
        public override bool CanSeek => _wrapped.CanSeek;
        public override bool CanWrite => _wrapped.CanWrite;
        public override long Length => _wrapped.Length;
        public override long Position
        {
            get => _wrapped.Position;
            set => _wrapped.Position = value;
        }

        public override void Flush() => _wrapped.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _wrapped.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _wrapped.Seek(offset, origin);
        public override void SetLength(long value) => _wrapped.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _wrapped.Write(buffer, offset, count);

        public override void Close()
        {
            // Closing the stream also marks the response as closed
            _parentResponse?.Close();
            _wrapped.Close();
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Disposing the stream also marks the response as closed
                _parentResponse?.Close();
                _wrapped.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
