// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace OpenLiveWriter.ApplicationFramework
{
    /// <summary>
    /// Interface for controls that provide a command bar definition.
    /// </summary>
    public interface ICommandBarProvider
    {
        /// <summary>
        /// Gets the command bar definition for this provider.
        /// </summary>
        CommandBarDefinition CommandBarDefinition { get; }
    }
}
