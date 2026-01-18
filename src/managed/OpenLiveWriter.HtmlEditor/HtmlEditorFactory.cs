// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;

namespace OpenLiveWriter.HtmlEditor
{
    /// <summary>
    /// Factory for creating HTML editors. Allows switching between
    /// MSHTML (IE) and WebView2 based editors.
    /// </summary>
    public static class HtmlEditorFactory
    {
        private static bool? _useWebView2;

        /// <summary>
        /// Gets or sets whether to use WebView2 instead of MSHTML.
        /// Default is false (use MSHTML) for backward compatibility.
        /// Set via environment variable OLW_USE_WEBVIEW2_EDITOR=1 to enable.
        /// </summary>
        public static bool UseWebView2
        {
            get
            {
                if (!_useWebView2.HasValue)
                {
                    string envVar = Environment.GetEnvironmentVariable("OLW_USE_WEBVIEW2_EDITOR");
                    _useWebView2 = !string.IsNullOrEmpty(envVar) && 
                                   (envVar == "1" || envVar.Equals("true", StringComparison.OrdinalIgnoreCase));
                    
                    if (_useWebView2.Value)
                    {
                        System.Diagnostics.Debug.WriteLine("[OLW-DEBUG] WebView2 editor mode ENABLED");
                    }
                }
                return _useWebView2.Value;
            }
            set
            {
                _useWebView2 = value;
            }
        }
    }
}
