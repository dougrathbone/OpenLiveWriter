// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace OpenLiveWriter.WebView2Shim
{
    /// <summary>
    /// WebView2 selection wrapper - standalone class for now.
    /// TODO: Implement IHTMLSelectionObject interface once all members are added.
    /// </summary>
    public class WebView2Selection
    {
        private readonly WebView2Bridge _bridge;
        private readonly WebView2Document _document;

        public WebView2Selection(WebView2Bridge bridge, WebView2Document document)
        {
            _bridge = bridge;
            _document = document;
        }

        public WebView2TextRange createRange()
        {
            return new WebView2TextRange(_bridge, _document);
        }

        public void empty()
        {
            _bridge.SelectionClear();
        }

        public void clear()
        {
            _bridge.SelectionClear();
        }

        public string type => _bridge.SelectionGetType();
    }
}
