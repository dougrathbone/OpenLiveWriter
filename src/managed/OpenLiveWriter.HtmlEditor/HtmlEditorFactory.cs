// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;

namespace OpenLiveWriter.HtmlEditor
{
    /// <summary>
    /// Factory for creating HTML editors. Allows switching between
    /// WebView2 (default) and MSHTML (IE) based editors.
    /// </summary>
    public static class HtmlEditorFactory
    {
        private static bool? _useMshtml;

        /// <summary>
        /// Gets or sets whether to use MSHTML (IE) instead of WebView2.
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
                    
                    System.Diagnostics.Debug.WriteLine(_useMshtml.Value 
                        ? "[OLW-DEBUG] MSHTML (IE) editor mode ENABLED via OLW_USE_MSHTML" 
                        : "[OLW-DEBUG] WebView2 editor mode (default)");
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
    }
}
