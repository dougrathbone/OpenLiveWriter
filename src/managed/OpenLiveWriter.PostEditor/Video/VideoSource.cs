// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Drawing;
using OpenLiveWriter.PostEditor.ContentSources.Common;

namespace OpenLiveWriter.PostEditor.Video
{
    /// <summary>
    /// Abstract base class for video sources.
    /// Extends MediaTab with video-specific functionality.
    /// </summary>
    public abstract class VideoSource : MediaTab
    {
        /// <summary>
        /// Gets the video for insertion into the post.
        /// </summary>
        /// <returns>The video to insert, or null if no video selected.</returns>
        public abstract Video GetVideoForInsert();

        /// <summary>
        /// Event raised when a video is selected.
        /// </summary>
        public event EventHandler VideoSelected;

        /// <summary>
        /// Raises the VideoSelected event.
        /// </summary>
        protected void OnVideoSelected()
        {
            VideoSelected?.Invoke(this, EventArgs.Empty);
        }
    }
}
