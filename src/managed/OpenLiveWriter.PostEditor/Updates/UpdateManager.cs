// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using OpenLiveWriter.CoreServices;
using OpenLiveWriter.CoreServices.ResourceDownloading;
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Xml;

namespace OpenLiveWriter.PostEditor.Updates
{
    /// <summary>
    /// Application update manager.
    /// TODO: Migrate from Squirrel.Windows to Velopack for .NET 10 compatibility.
    /// See: https://docs.velopack.io/migrating/squirrel
    /// </summary>
    public class UpdateManager
    {
        public static DateTime Expires = DateTime.MaxValue;
        
        public static void CheckforUpdates(bool forceCheck = false)
        {
            // TODO: Implement Velopack-based updates
            // Squirrel.Windows is not compatible with .NET 10
            // For now, auto-update is disabled
            Trace.WriteLine("Auto-update is currently disabled. Migrate to Velopack for .NET 10 support.");
        }

        private const int UPDATELAUNCHDELAY = 10000;
    }
}
