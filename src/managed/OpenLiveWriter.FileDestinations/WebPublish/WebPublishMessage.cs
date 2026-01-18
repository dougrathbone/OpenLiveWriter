// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Globalization;
using OpenLiveWriter.Controls;
using OpenLiveWriter.Localization;

namespace OpenLiveWriter.FileDestinations
{
    public class WebPublishMessage
    {
        private DisplayMessage _displayMessage;
        private object[] _textFormatArgs;
        private string _title;
        private string _text;

        /// <summary>
        /// Constructor for MessageId-based messages (modern pattern)
        /// </summary>
        public WebPublishMessage(MessageId messageId, params object[] textFormatArgs)
        {
            _displayMessage = new DisplayMessage(messageId);
            _textFormatArgs = textFormatArgs;
        }

        /// <summary>
        /// Protected constructor for designer-based messages (legacy pattern)
        /// </summary>
        protected WebPublishMessage()
        {
            _textFormatArgs = Array.Empty<object>();
        }

        public string Title
        {
            get { return _displayMessage?.Title ?? _title ?? string.Empty; }
            protected set { _title = value; }
        }

        public string Text
        {
            get
            {
                if (_displayMessage != null)
                    return string.Format(CultureInfo.CurrentCulture, _displayMessage.Text, _textFormatArgs);
                return string.Format(CultureInfo.CurrentCulture, _text ?? string.Empty, _textFormatArgs);
            }
            protected set { _text = value; }
        }

        protected object[] TextFormatArgs
        {
            get { return _textFormatArgs; }
            set { _textFormatArgs = value ?? Array.Empty<object>(); }
        }
    }
}
