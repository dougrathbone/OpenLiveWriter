// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace OpenLiveWriter.BrowserControl
{
    /// <summary>
    /// WebView2-based browser control implementing IBrowserControl interface
    /// </summary>
    public class WebView2BrowserControl : UserControl, IBrowserControl
    {
        private WebView2 _webView;
        private bool _isInitialized;
        private string _pendingNavigateUrl;
        private string _title = string.Empty;
        private string _statusText = string.Empty;

        public WebView2BrowserControl()
        {
            InitializeWebView2();
        }

        private async void InitializeWebView2()
        {
            _webView = new WebView2();
            _webView.Dock = DockStyle.Fill;
            Controls.Add(_webView);

            try
            {
                await _webView.EnsureCoreWebView2Async(null);
                _isInitialized = true;

                // Wire up events
                _webView.NavigationCompleted += WebView_NavigationCompleted;
                _webView.NavigationStarting += WebView_NavigationStarting;
                _webView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
                _webView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;

                // Navigate to pending URL if any
                if (!string.IsNullOrEmpty(_pendingNavigateUrl))
                {
                    _webView.CoreWebView2.Navigate(_pendingNavigateUrl);
                    _pendingNavigateUrl = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebView2 initialization failed: {ex.Message}");
            }
        }

        #region Event Handlers

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            DownloadComplete?.Invoke(this, EventArgs.Empty);
            DocumentComplete?.Invoke(this, new BrowserDocumentEventArgs(_webView.Source?.ToString() ?? string.Empty, null));
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            DownloadBegin?.Invoke(this, EventArgs.Empty);
            NavigateComplete2?.Invoke(this, new BrowserNavigateComplete2EventArgs(null, e.Uri));
        }

        private void CoreWebView2_DocumentTitleChanged(object sender, object e)
        {
            _title = _webView.CoreWebView2?.DocumentTitle ?? string.Empty;
            TitleChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CoreWebView2_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            CommandStateChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region IBrowserControl Implementation

        public string LocationName => _webView?.CoreWebView2?.DocumentTitle ?? string.Empty;

        public string LocationURL => _webView?.Source?.ToString() ?? string.Empty;

        public string Title => _title;

        public string StatusText => _statusText;

        public EncryptionLevel EncryptionLevel
        {
            get
            {
                // WebView2 uses HTTPS by default, check URL scheme
                var url = LocationURL;
                if (!string.IsNullOrEmpty(url) && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return EncryptionLevel.OneHundredTwentyEightBit;
                return EncryptionLevel.Unsecure;
            }
        }

        public TextSize TextSize
        {
            get => TextSize.Medium;
            set
            {
                // WebView2 doesn't have direct text size control like IE
                // Would need to use JavaScript to adjust zoom
                if (_isInitialized && _webView?.CoreWebView2 != null)
                {
                    double zoomFactor;
                    switch (value)
                    {
                        case TextSize.Smallest: zoomFactor = 0.5; break;
                        case TextSize.Smaller: zoomFactor = 0.75; break;
                        case TextSize.Medium: zoomFactor = 1.0; break;
                        case TextSize.Larger: zoomFactor = 1.25; break;
                        case TextSize.Largest: zoomFactor = 1.5; break;
                        default: zoomFactor = 1.0; break;
                    }
                    _webView.ZoomFactor = zoomFactor;
                }
            }
        }

        public bool TextSizeSupported => true;

        public bool Busy => !_isInitialized;

        public bool Silent { get; set; } = true;

        public bool WorkOffline { get; set; }

        /// <summary>
        /// Returns the WebView2 control itself. Note: This is NOT an IHTMLDocument2!
        /// Code that casts this to IHTMLDocument2 will need to be updated.
        /// </summary>
        public object Document => _webView;

        public void Navigate(string url)
        {
            Navigate(url, false);
        }

        public void Navigate(string url, bool newWindow)
        {
            if (newWindow)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                return;
            }

            if (_isInitialized && _webView?.CoreWebView2 != null)
            {
                _webView.CoreWebView2.Navigate(url);
            }
            else
            {
                _pendingNavigateUrl = url;
            }
        }

        /// <summary>
        /// Navigate to HTML content directly
        /// </summary>
        public void NavigateToString(string htmlContent)
        {
            if (_isInitialized && _webView?.CoreWebView2 != null)
            {
                _webView.NavigateToString(htmlContent);
            }
        }

        public bool IsEnabled(BrowserCommand command)
        {
            if (!_isInitialized || _webView?.CoreWebView2 == null)
                return false;

            switch (command)
            {
                case BrowserCommand.GoBack: return _webView.CanGoBack;
                case BrowserCommand.GoForward: return _webView.CanGoForward;
                case BrowserCommand.Stop: return true;
                case BrowserCommand.Refresh: return true;
                case BrowserCommand.SelectAll: return true;
                case BrowserCommand.Copy: return true;
                default: return false;
            }
        }

        public void Execute(BrowserCommand command)
        {
            if (!_isInitialized || _webView?.CoreWebView2 == null)
                return;

            switch (command)
            {
                case BrowserCommand.GoBack:
                    _webView.GoBack();
                    break;
                case BrowserCommand.GoForward:
                    _webView.GoForward();
                    break;
                case BrowserCommand.Stop:
                    _webView.Stop();
                    break;
                case BrowserCommand.Refresh:
                    _webView.Reload();
                    break;
                case BrowserCommand.SelectAll:
                    _webView.CoreWebView2.ExecuteScriptAsync("document.execCommand('selectAll')");
                    break;
                case BrowserCommand.Copy:
                    _webView.CoreWebView2.ExecuteScriptAsync("document.execCommand('copy')");
                    break;
            }
        }

        public void UpdateCommandState()
        {
            CommandStateChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region WebView2-specific methods

        /// <summary>
        /// Execute JavaScript and return result
        /// </summary>
        public async Task<string> ExecuteScriptAsync(string script)
        {
            if (!_isInitialized || _webView?.CoreWebView2 == null)
                return null;

            return await _webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        /// <summary>
        /// Get the HTML content of the page
        /// </summary>
        public async Task<string> GetHtmlAsync()
        {
            if (!_isInitialized || _webView?.CoreWebView2 == null)
                return string.Empty;

            var result = await _webView.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML");
            // Result is JSON-encoded, need to unescape
            if (result != null && result.StartsWith("\"") && result.EndsWith("\""))
            {
                result = result.Substring(1, result.Length - 2)
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");
            }
            return result ?? string.Empty;
        }

        /// <summary>
        /// Set the HTML content of the page
        /// </summary>
        public void SetHtml(string html)
        {
            if (_isInitialized && _webView?.CoreWebView2 != null)
            {
                _webView.NavigateToString(html);
            }
        }

        /// <summary>
        /// Access to underlying WebView2 control for advanced scenarios
        /// </summary>
        public WebView2 WebView => _webView;

        /// <summary>
        /// Check if WebView2 is initialized and ready
        /// </summary>
        public bool IsReady => _isInitialized && _webView?.CoreWebView2 != null;

        /// <summary>
        /// Wait for WebView2 to be ready
        /// </summary>
        public async Task WaitForReadyAsync()
        {
            while (!IsReady)
            {
                await Task.Delay(50);
            }
        }

        #endregion

        #region Events

#pragma warning disable 0067 // Events not raised yet - will be connected in future implementation
        public event BrowserNavigateComplete2EventHandler NavigateComplete2;
        public event BrowserDocumentEventHandler DocumentComplete;
        public event BrowserDocumentEventHandler FrameComplete;
        public event EventHandler DownloadBegin;
        public event EventHandler DownloadComplete;
        public event BrowserProgressChangeEventHandler ProgressChange;
        public event EventHandler TitleChanged;
        public event EventHandler StatusTextChanged;
        public event EventHandler EncryptionLevelChanged;
        public event EventHandler CommandStateChanged;
#pragma warning restore 0067

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _webView?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
