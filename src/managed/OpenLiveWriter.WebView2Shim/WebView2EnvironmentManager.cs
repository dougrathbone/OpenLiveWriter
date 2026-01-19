// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace OpenLiveWriter.WebView2Shim
{
    /// <summary>
    /// Manages a shared WebView2 environment to avoid initialization conflicts
    /// when multiple WebView2 controls are created.
    /// </summary>
    public static class WebView2EnvironmentManager
    {
        private static CoreWebView2Environment _sharedEnvironment;
        private static Task<CoreWebView2Environment> _initTask;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the shared WebView2 environment, creating it if necessary.
        /// Thread-safe and can be called from multiple controls simultaneously.
        /// Uses --allow-file-access-from-files for local image support.
        /// </summary>
        public static Task<CoreWebView2Environment> GetEnvironmentAsync()
        {
            lock (_lock)
            {
                if (_sharedEnvironment != null)
                {
                    return Task.FromResult(_sharedEnvironment);
                }

                if (_initTask == null)
                {
                    Debug.WriteLine("[OLW-DEBUG] WebView2EnvironmentManager: Creating shared environment");
                    _initTask = CreateEnvironmentAsync();
                }
            }

            return _initTask;
        }

        private static async Task<CoreWebView2Environment> CreateEnvironmentAsync()
        {
            try
            {
                // Use --allow-file-access-from-files for local image support in WYSIWYG editor
                var options = new CoreWebView2EnvironmentOptions("--allow-file-access-from-files");
                var env = await CoreWebView2Environment.CreateAsync(null, null, options);
                lock (_lock)
                {
                    _sharedEnvironment = env;
                }
                Debug.WriteLine("[OLW-DEBUG] WebView2EnvironmentManager: Shared environment created successfully");
                return env;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OLW-DEBUG] WebView2EnvironmentManager: Failed to create environment: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Returns true if the shared environment has been created.
        /// </summary>
        public static bool IsEnvironmentReady => _sharedEnvironment != null;
    }
}
