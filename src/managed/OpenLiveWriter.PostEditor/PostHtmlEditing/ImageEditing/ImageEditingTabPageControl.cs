// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Drawing;
using System.Windows.Forms;
using OpenLiveWriter.ApplicationFramework;

namespace OpenLiveWriter.PostEditor.PostHtmlEditing
{
    /// <summary>
    /// Base class for image editing tab page controls.
    /// </summary>
    public class ImageEditingTabPageControl : TabPageControl
    {
        public ImageEditingTabPageControl()
        {
        }

        /// <summary>
        /// Gets or sets the decorators manager.
        /// </summary>
        public virtual ImageDecoratorsManager DecoratorsManager { get; set; }

        /// <summary>
        /// Gets or sets the image properties info.
        /// </summary>
        public virtual ImagePropertiesInfo ImageInfo { get; set; }

        /// <summary>
        /// Event raised when an image property changes.
        /// </summary>
        public event ImagePropertyEventHandler ImagePropertyChanged;

        /// <summary>
        /// Raises the ImagePropertyChanged event.
        /// </summary>
        protected virtual void OnImagePropertyChanged(ImagePropertyEvent evt)
        {
            ImagePropertyChanged?.Invoke(this, evt);
        }

        /// <summary>
        /// Handles an image property changed event from another source.
        /// </summary>
        public virtual void HandleImagePropertyChangedEvent(ImagePropertyEvent evt)
        {
            // Subclasses can override to respond to property changes
        }
    }

    /// <summary>
    /// Tab page control for image-related settings.
    /// </summary>
    public class ImageTabPageImageControl : ImageEditingTabPageControl
    {
        public ImageTabPageImageControl()
        {
            TabText = "Image";
            TabBitmap = null;
        }
    }

    /// <summary>
    /// Tab page control for layout-related settings.
    /// </summary>
    public class ImageTabPageLayoutControl : ImageEditingTabPageControl
    {
        public ImageTabPageLayoutControl()
        {
            TabText = "Layout";
            TabBitmap = null;
        }
    }

    /// <summary>
    /// Tab page control for effects-related settings.
    /// </summary>
    public class ImageTabPageEffectsControl : ImageEditingTabPageControl
    {
        public ImageTabPageEffectsControl()
        {
            TabText = "Effects";
            TabBitmap = null;
        }
    }

    /// <summary>
    /// Tab page control for upload-related settings.
    /// </summary>
    public class ImageTabPageUploadControl : ImageEditingTabPageControl
    {
        public ImageTabPageUploadControl()
        {
            TabText = "Upload";
            TabBitmap = null;
        }
    }
}
