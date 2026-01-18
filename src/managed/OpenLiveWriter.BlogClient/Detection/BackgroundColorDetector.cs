// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Diagnostics;
using System.Drawing;

namespace OpenLiveWriter.BlogClient.Detection
{
    class BackgroundColorDetector
    {
        /// <summary>
        /// Detect the background color of a post body from a URI where
        /// the post body element contains BlogEditingTemplate.POST_BODY_MARKER.
        /// 
        /// STUBBED OUT: This previously used IE via BrowserOperationInvoker which was slow
        /// and depends on MSHTML. Returns default color for now.
        /// 
        /// TODO: Re-implement with WebView2 when tackling BrowserOperationInvoker migration.
        /// The WebView2 implementation would use JavaScript:
        ///   var walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
        ///   while(walker.nextNode()) {
        ///       if (walker.currentNode.textContent.includes('MARKER')) {
        ///           return window.getComputedStyle(walker.currentNode.parentElement).backgroundColor;
        ///       }
        ///   }
        /// </summary>
        public static Color? DetectColor(string uri, Color? defaultColor)
        {
            Debug.WriteLine("[OLW-DEBUG] BackgroundColorDetector.DetectColor() - STUBBED, returning default color");
            
            // Return white as the default - most blogs have light backgrounds
            // This is purely cosmetic - affects editor background color matching
            return defaultColor ?? Color.White;
        }
    }
}
