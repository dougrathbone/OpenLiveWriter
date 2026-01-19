// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Windows.Forms;

namespace OpenLiveWriter.BrowserControl
{
    /// <summary>
    /// Factory for creating browser controls. Allows switching between
    /// WebView2 (default) and IE WebBrowser (ExplorerBrowserControl).
    /// </summary>
    public static class BrowserControlFactory
    {
        private static bool? _useMshtml;

        /// <summary>
        /// Gets or sets whether to use IE WebBrowser instead of WebView2.
        /// Default is false (use WebView2).
        /// Set via environment variable OLW_USE_MSHTML=1 to use legacy IE engine.
        /// </summary>
        public static bool UseMshtml
        {
            get
            {
                if (!_useMshtml.HasValue)
                {
                    string envVar = Environment.GetEnvironmentVariable("OLW_USE_MSHTML");
                    _useMshtml = !string.IsNullOrEmpty(envVar) && 
                                 (envVar == "1" || envVar.Equals("true", StringComparison.OrdinalIgnoreCase));
                }
                return _useMshtml.Value;
            }
            set
            {
                _useMshtml = value;
            }
        }

        /// <summary>
        /// Gets whether to use WebView2 (inverse of UseMshtml for compatibility).
        /// WebView2 is now the default.
        /// </summary>
        public static bool UseWebView2 => !UseMshtml;

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
