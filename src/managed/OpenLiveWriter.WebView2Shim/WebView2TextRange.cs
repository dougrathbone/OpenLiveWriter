// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;

namespace OpenLiveWriter.WebView2Shim
{
    /// <summary>
    /// WebView2 text range wrapper - standalone class for now.
    /// Provides text range operations via JavaScript Range API.
    /// </summary>
    public class WebView2TextRange : IDisposable
    {
        private readonly WebView2Bridge _bridge;
        private readonly WebView2Document _document;
        private string _rangeId;
        private bool _disposed;

        public WebView2TextRange(WebView2Bridge bridge, string rangeId, WebView2Document document)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _document = document;
            _rangeId = rangeId ?? _bridge.TextRangeCreate();
        }

        public WebView2TextRange(WebView2Bridge bridge, WebView2Document document)
            : this(bridge, null, document)
        {
            // Create from current selection
            var selectionRangeId = _bridge.TextRangeCreateFromSelection();
            if (!string.IsNullOrEmpty(selectionRangeId))
            {
                _rangeId = selectionRangeId;
            }
        }

        public string RangeId => _rangeId;

        // ===== Basic Properties =====

        public string htmlText => _bridge.TextRangeGetHtml(_rangeId);

        public string text
        {
            get => _bridge.TextRangeGetText(_rangeId);
            set => _bridge.TextRangeSetText(_rangeId, value);
        }

        // ===== Navigation =====

        public WebView2Element parentElement()
        {
            var elementId = _bridge.TextRangeGetParentElement(_rangeId);
            if (string.IsNullOrEmpty(elementId)) return null;
            var el = new WebView2Element(_bridge, elementId);
            el.ParentDocument = _document;
            return el;
        }

        public void collapse(bool start = true) => _bridge.TextRangeCollapse(_rangeId, start);
        public void select() => _bridge.TextRangeSelect(_rangeId);

        public void moveToElementText(WebView2Element element)
        {
            if (element != null)
            {
                _bridge.TextRangeMoveToElement(_rangeId, element.ElementId);
            }
        }

        // ===== Duplicate =====

        public WebView2TextRange duplicate()
        {
            // Create a new range and copy the current one
            var newId = _bridge.ExecuteScript<string>("OLW.textRange.duplicate(" + WebView2Bridge.JsonEncode(_rangeId) + ")");
            if (string.IsNullOrEmpty(newId))
            {
                newId = _bridge.TextRangeCreate();
            }
            return new WebView2TextRange(_bridge, newId, _document);
        }

        // ===== HTML Operations =====

        public void pasteHTML(string html)
        {
            _bridge.TextRangeSetHtml(_rangeId, html);
        }

        // ===== Command Execution =====

        public bool execCommand(string cmdID, bool showUI = false, object value = null)
        {
            select(); // Ensure this range is selected first
            return _bridge.DocumentExecCommand(cmdID, showUI, value?.ToString());
        }

        public bool queryCommandState(string cmdID)
        {
            select();
            return _bridge.DocumentQueryCommandState(cmdID);
        }

        public bool queryCommandEnabled(string cmdID)
        {
            select();
            return _bridge.DocumentQueryCommandEnabled(cmdID);
        }

        public string queryCommandValue(string cmdID)
        {
            select();
            return _bridge.DocumentQueryCommandValue(cmdID);
        }

        // ===== Comparison =====

        public bool inRange(WebView2TextRange other)
        {
            if (other == null) return false;
            return _bridge.ExecuteScript<bool>("OLW.textRange.inRange(" + WebView2Bridge.JsonEncode(_rangeId) + ", " + WebView2Bridge.JsonEncode(other.RangeId) + ")");
        }

        public bool isEqual(WebView2TextRange other)
        {
            if (other == null) return false;
            return _bridge.ExecuteScript<bool>("OLW.textRange.isEqual(" + WebView2Bridge.JsonEncode(_rangeId) + ", " + WebView2Bridge.JsonEncode(other.RangeId) + ")");
        }

        // ===== Movement (simplified - not all MSHTML options supported) =====

        public bool expand(string unit)
        {
            return _bridge.ExecuteScript<bool>("OLW.textRange.expand(" + WebView2Bridge.JsonEncode(_rangeId) + ", " + WebView2Bridge.JsonEncode(unit) + ")");
        }

        public int move(string unit, int count = 1)
        {
            return _bridge.ExecuteScript<int>("OLW.textRange.move(" + WebView2Bridge.JsonEncode(_rangeId) + ", " + WebView2Bridge.JsonEncode(unit) + ", " + count + ")");
        }

        public int moveStart(string unit, int count = 1)
        {
            return _bridge.ExecuteScript<int>("OLW.textRange.moveStart(" + WebView2Bridge.JsonEncode(_rangeId) + ", " + WebView2Bridge.JsonEncode(unit) + ", " + count + ")");
        }

        public int moveEnd(string unit, int count = 1)
        {
            return _bridge.ExecuteScript<int>("OLW.textRange.moveEnd(" + WebView2Bridge.JsonEncode(_rangeId) + ", " + WebView2Bridge.JsonEncode(unit) + ", " + count + ")");
        }

        public void scrollIntoView(bool startToTop = true)
        {
            _bridge.ExecuteScript("OLW.textRange.scrollIntoView(" + WebView2Bridge.JsonEncode(_rangeId) + ", " + (startToTop ? "true" : "false") + ")");
        }

        public void setEndPoint(string how, WebView2TextRange sourceRange)
        {
            if (sourceRange != null)
            {
                _bridge.ExecuteScript("OLW.textRange.setEndPoint(" + WebView2Bridge.JsonEncode(_rangeId) + ", " + WebView2Bridge.JsonEncode(how) + ", " + WebView2Bridge.JsonEncode(sourceRange.RangeId) + ")");
            }
        }

        public int compareEndPoints(string how, WebView2TextRange sourceRange)
        {
            if (sourceRange == null) return 0;
            return _bridge.ExecuteScript<int>("OLW.textRange.compareEndPoints(" + WebView2Bridge.JsonEncode(_rangeId) + ", " + WebView2Bridge.JsonEncode(how) + ", " + WebView2Bridge.JsonEncode(sourceRange.RangeId) + ")");
        }

        public bool findText(string textToFind, int count = 0, int flags = 0)
        {
            return _bridge.ExecuteScript<bool>("OLW.textRange.findText(" + WebView2Bridge.JsonEncode(_rangeId) + ", " + WebView2Bridge.JsonEncode(textToFind) + ", " + count + ", " + flags + ")");
        }

        public void moveToPoint(int x, int y)
        {
            _bridge.ExecuteScript("OLW.textRange.moveToPoint(" + WebView2Bridge.JsonEncode(_rangeId) + ", " + x + ", " + y + ")");
        }

        public string getBookmark()
        {
            return _bridge.ExecuteScript<string>("OLW.textRange.getBookmark(" + WebView2Bridge.JsonEncode(_rangeId) + ")");
        }

        public bool moveToBookmark(string bookmark)
        {
            return _bridge.ExecuteScript<bool>("OLW.textRange.moveToBookmark(" + WebView2Bridge.JsonEncode(_rangeId) + ", " + WebView2Bridge.JsonEncode(bookmark) + ")");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _bridge.TextRangeDispose(_rangeId);
                _disposed = true;
            }
        }

        public override string ToString() => "WebView2TextRange[" + _rangeId + "]";
    }
}
