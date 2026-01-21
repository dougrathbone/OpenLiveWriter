// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Windows.Forms;
using OpenLiveWriter.Api;

// Map feature has been deprecated - Bing Maps API no longer available

namespace OpenLiveWriter.InternalWriterPlugin
{
    /// <summary>
    /// Stub sidebar control for deprecated map feature.
    /// This class is retained for backward compatibility with existing map content
    /// but no longer provides any editing functionality.
    /// </summary>
    internal class MapSidebarControl : SmartContentEditor
    {
        public MapSidebarControl(MapOptions mapOptions, ISmartContentEditorSite contentEditorSite)
        {
            // No initialization needed - feature deprecated
            var label = new Label
            {
                Text = "The Map feature is no longer available.\nBing Maps API has been deprecated.",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.TopCenter,
                Padding = new Padding(10)
            };
            Controls.Add(label);
        }

        protected override void OnSelectedContentChanged()
        {
            base.OnSelectedContentChanged();
            // No-op - feature deprecated
        }
    }
}
