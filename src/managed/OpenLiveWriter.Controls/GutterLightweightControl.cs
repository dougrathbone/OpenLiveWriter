// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.ComponentModel;

namespace OpenLiveWriter.Controls
{
    /// <summary>
    /// A lightweight control for displaying a gutter area.
    /// </summary>
    public class GutterLightweightControl : LightweightControl
    {
        public GutterLightweightControl()
        {
        }

        public GutterLightweightControl(IContainer container)
        {
            container.Add(this);
        }
    }
}
