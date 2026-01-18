// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Windows.Forms;

namespace OpenLiveWriter.BrowserControl
{
    /// <summary>
    /// Factory for creating browser controls. Allows switching between
    /// IE WebBrowser (ExplorerBrowserControl) and WebView2 (WebView2BrowserControl).
    /// </summary>
    public static class BrowserControlFactory
    {
        private static bool? _useWebView2;

        /// <summary>
        /// Gets or sets whether to use WebView2 instead of IE WebBrowser.
        /// Default is false (use IE) for backward compatibility.
        /// Set via environment variable OLW_USE_WEBVIEW2=1 to enable.
        /// </summary>
        public static bool UseWebView2
        {
            get
            {
                if (!_useWebView2.HasValue)
                {
                    string envVar = Environment.GetEnvironmentVariable("OLW_USE_WEBVIEW2");
                    _useWebView2 = !string.IsNullOrEmpty(envVar) && 
                                   (envVar == "1" || envVar.Equals("true", StringComparison.OrdinalIgnoreCase));
                }
                return _useWebView2.Value;
            }
            set
            {
                _useWebView2 = value;
            }
        }

        /// <summary>
        /// Creates a new browser control based on the current configuration.
        /// </summary>
        /// <returns>An IBrowserControl implementation</returns>
        public static IBrowserControl CreateBrowserControl()
        {
            if (UseWebView2)
            {
                return new WebView2BrowserControl();
            }
            else
            {
                return new ExplorerBrowserControl();
            }
        }

        /// <summary>
        /// Creates a new browser control as a UserControl for adding to a form.
        /// </summary>
        /// <returns>A UserControl that implements IBrowserControl</returns>
        public static UserControl CreateBrowserUserControl()
        {
            if (UseWebView2)
            {
                return new WebView2BrowserControl();
            }
            else
            {
                return new ExplorerBrowserControl();
            }
        }
    }
}
