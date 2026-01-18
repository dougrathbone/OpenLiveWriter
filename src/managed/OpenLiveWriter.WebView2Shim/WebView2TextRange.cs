// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using mshtml;

namespace OpenLiveWriter.WebView2Shim
{
    /// <summary>
    /// WebView2 implementation of IHTMLTxtRange using modern Range/Selection APIs.
    /// </summary>
    public class WebView2TextRange : IHTMLTxtRange, IDisposable
    {
        private readonly WebView2Bridge _bridge;
        private readonly WebView2Document _document;
        private string _rangeId;
        private bool _disposed;

        public WebView2TextRange(WebView2Bridge bridge, WebView2Document document)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _document = document ?? throw new ArgumentNullException(nameof(document));
            
            // Create a range from the current selection
            _rangeId = _bridge.TextRangeCreateFromSelection();
            if (string.IsNullOrEmpty(_rangeId))
            {
                _rangeId = _bridge.TextRangeCreate();
            }
        }

        private WebView2TextRange(WebView2Bridge bridge, WebView2Document document, string rangeId)
        {
            _bridge = bridge;
            _document = document;
            _rangeId = rangeId;
        }

        public string htmlText => _bridge.TextRangeGetHtml(_rangeId);

        public string text
        {
            get => _bridge.TextRangeGetText(_rangeId);
            set => _bridge.TextRangeSetText(_rangeId, value);
        }

        public IHTMLElement parentElement()
        {
            var elementId = _bridge.TextRangeGetParentElement(_rangeId);
            if (string.IsNullOrEmpty(elementId)) return null;
            var el = new WebView2Element(_bridge, elementId);
            el.ParentDocument = _document;
            return el;
        }

        public IHTMLTxtRange duplicate()
        {
            var newId = _bridge.ExecuteScript<string>($@"
                (function() {{
                    var range = OLW.textRange.ranges ? OLW.textRange.ranges.get({WebView2Bridge.JsonEncode(_rangeId)}) : null;
                    if (!range) return null;
                    var clone = range.cloneRange();
                    var id = 'range-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
                    if (!OLW.textRange.ranges) OLW.textRange.ranges = new Map();
                    OLW.textRange.ranges.set(id, clone);
                    return id;
                }})()");
            
            if (string.IsNullOrEmpty(newId))
            {
                newId = _bridge.TextRangeCreate();
            }
            
            return new WebView2TextRange(_bridge, _document, newId);
        }

        public bool inRange(IHTMLTxtRange range)
        {
            if (range is WebView2TextRange other)
            {
                return _bridge.ExecuteScript<bool>($@"
                    (function() {{
                        var r1 = OLW.textRange.ranges?.get({WebView2Bridge.JsonEncode(_rangeId)});
                        var r2 = OLW.textRange.ranges?.get({WebView2Bridge.JsonEncode(other._rangeId)});
                        if (!r1 || !r2) return false;
                        return r1.compareBoundaryPoints(Range.START_TO_START, r2) <= 0 &&
                               r1.compareBoundaryPoints(Range.END_TO_END, r2) >= 0;
                    }})()");
            }
            return false;
        }

        public bool isEqual(IHTMLTxtRange range)
        {
            if (range is WebView2TextRange other)
            {
                return _bridge.ExecuteScript<bool>($@"
                    (function() {{
                        var r1 = OLW.textRange.ranges?.get({WebView2Bridge.JsonEncode(_rangeId)});
                        var r2 = OLW.textRange.ranges?.get({WebView2Bridge.JsonEncode(other._rangeId)});
                        if (!r1 || !r2) return false;
                        return r1.compareBoundaryPoints(Range.START_TO_START, r2) === 0 &&
                               r1.compareBoundaryPoints(Range.END_TO_END, r2) === 0;
                    }})()");
            }
            return false;
        }

        public void scrollIntoView(bool fStart = true)
        {
            _bridge.ExecuteScript($@"
                (function() {{
                    var range = OLW.textRange.ranges?.get({WebView2Bridge.JsonEncode(_rangeId)});
                    if (!range) return;
                    var rect = range.getBoundingClientRect();
                    if (rect) {{
                        window.scrollTo({{
                            top: window.scrollY + rect.top + ({(fStart ? "0" : "rect.height")}),
                            behavior: 'smooth'
                        }});
                    }}
                }})()");
        }

        public void collapse(bool Start = true)
        {
            _bridge.TextRangeCollapse(_rangeId, Start);
        }

        public bool expand(string Unit)
        {
            // Expand the range to encompass the specified unit (word, sentence, etc.)
            return _bridge.ExecuteScript<bool>($@"
                (function() {{
                    var range = OLW.textRange.ranges?.get({WebView2Bridge.JsonEncode(_rangeId)});
                    if (!range) return false;
                    
                    var unit = {WebView2Bridge.JsonEncode(Unit)}.toLowerCase();
                    var sel = window.getSelection();
                    sel.removeAllRanges();
                    sel.addRange(range);
                    
                    if (unit === 'word') {{
                        // Expand to word boundaries
                        sel.modify('extend', 'forward', 'word');
                        sel.modify('extend', 'backward', 'word');
                        sel.modify('extend', 'forward', 'word');
                    }} else if (unit === 'sentence') {{
                        sel.modify('extend', 'forward', 'sentenceboundary');
                        sel.modify('extend', 'backward', 'sentenceboundary');
                    }} else if (unit === 'textedit') {{
                        // Select all content
                        sel.selectAllChildren(document.body);
                    }}
                    
                    if (sel.rangeCount > 0) {{
                        var newRange = sel.getRangeAt(0).cloneRange();
                        OLW.textRange.ranges.set({WebView2Bridge.JsonEncode(_rangeId)}, newRange);
                        return true;
                    }}
                    return false;
                }})()");
        }

        public int move(string Unit, int Count = 1)
        {
            // Move both start and end of range
            return _bridge.ExecuteScript<int>($@"
                (function() {{
                    var range = OLW.textRange.ranges?.get({WebView2Bridge.JsonEncode(_rangeId)});
                    if (!range) return 0;
                    
                    var unit = {WebView2Bridge.JsonEncode(Unit)}.toLowerCase();
                    var count = {Count};
                    var direction = count > 0 ? 'forward' : 'backward';
                    var absCount = Math.abs(count);
                    
                    var sel = window.getSelection();
                    sel.removeAllRanges();
                    range.collapse(count > 0);
                    sel.addRange(range);
                    
                    var moved = 0;
                    for (var i = 0; i < absCount; i++) {{
                        var before = sel.toString().length;
                        sel.modify('move', direction, unit === 'character' ? 'character' : unit === 'word' ? 'word' : 'line');
                        moved++;
                    }}
                    
                    if (sel.rangeCount > 0) {{
                        var newRange = sel.getRangeAt(0).cloneRange();
                        newRange.collapse(true);
                        OLW.textRange.ranges.set({WebView2Bridge.JsonEncode(_rangeId)}, newRange);
                    }}
                    
                    return count > 0 ? moved : -moved;
                }})()");
        }

        public int moveStart(string Unit, int Count = 1)
        {
            return _bridge.ExecuteScript<int>($@"
                (function() {{
                    var range = OLW.textRange.ranges?.get({WebView2Bridge.JsonEncode(_rangeId)});
                    if (!range) return 0;
                    
                    var unit = {WebView2Bridge.JsonEncode(Unit)}.toLowerCase();
                    var count = {Count};
                    var direction = count > 0 ? 'forward' : 'backward';
                    var absCount = Math.abs(count);
                    
                    // For character movement, use setStart/setEnd directly
                    if (unit === 'character') {{
                        var container = range.startContainer;
                        var offset = range.startOffset + count;
                        if (container.nodeType === 3) {{
                            offset = Math.max(0, Math.min(offset, container.length));
                            range.setStart(container, offset);
                        }}
                        return count;
                    }}
                    
                    return count;
                }})()");
        }

        public int moveEnd(string Unit, int Count = 1)
        {
            return _bridge.ExecuteScript<int>($@"
                (function() {{
                    var range = OLW.textRange.ranges?.get({WebView2Bridge.JsonEncode(_rangeId)});
                    if (!range) return 0;
                    
                    var unit = {WebView2Bridge.JsonEncode(Unit)}.toLowerCase();
                    var count = {Count};
                    
                    if (unit === 'character') {{
                        var container = range.endContainer;
                        var offset = range.endOffset + count;
                        if (container.nodeType === 3) {{
                            offset = Math.max(0, Math.min(offset, container.length));
                            range.setEnd(container, offset);
                        }}
                        return count;
                    }}
                    
                    return count;
                }})()");
        }

        public void select()
        {
            _bridge.TextRangeSelect(_rangeId);
        }

        public void pasteHTML(string html)
        {
            _bridge.TextRangeSetHtml(_rangeId, html);
        }

        public void moveToElementText(IHTMLElement element)
        {
            if (element is WebView2Element w2e)
            {
                _bridge.TextRangeMoveToElement(_rangeId, w2e.ElementId);
            }
        }

        public void setEndPoint(string how, IHTMLTxtRange SourceRange)
        {
            if (SourceRange is WebView2TextRange other)
            {
                _bridge.ExecuteScript($@"
                    (function() {{
                        var range = OLW.textRange.ranges?.get({WebView2Bridge.JsonEncode(_rangeId)});
                        var source = OLW.textRange.ranges?.get({WebView2Bridge.JsonEncode(other._rangeId)});
                        if (!range || !source) return;
                        
                        var how = {WebView2Bridge.JsonEncode(how)}.toLowerCase();
                        if (how === 'starttostart') {{
                            range.setStart(source.startContainer, source.startOffset);
                        }} else if (how === 'starttoend') {{
                            range.setStart(source.endContainer, source.endOffset);
                        }} else if (how === 'endtostart') {{
                            range.setEnd(source.startContainer, source.startOffset);
                        }} else if (how === 'endtoend') {{
                            range.setEnd(source.endContainer, source.endOffset);
                        }}
                    }})()");
            }
        }

        public int compareEndPoints(string how, IHTMLTxtRange SourceRange)
        {
            if (SourceRange is WebView2TextRange other)
            {
                return _bridge.ExecuteScript<int>($@"
                    (function() {{
                        var range = OLW.textRange.ranges?.get({WebView2Bridge.JsonEncode(_rangeId)});
                        var source = OLW.textRange.ranges?.get({WebView2Bridge.JsonEncode(other._rangeId)});
                        if (!range || !source) return 0;
                        
                        var how = {WebView2Bridge.JsonEncode(how)}.toLowerCase();
                        var type;
                        if (how === 'starttostart') type = Range.START_TO_START;
                        else if (how === 'starttoend') type = Range.START_TO_END;
                        else if (how === 'endtostart') type = Range.END_TO_START;
                        else type = Range.END_TO_END;
                        
                        return range.compareBoundaryPoints(type, source);
                    }})()");
            }
            return 0;
        }

        public bool findText(string String, int Count = 0x3fffffff, int Flags = 0)
        {
            return _bridge.ExecuteScript<bool>($@"
                (function() {{
                    var searchText = {WebView2Bridge.JsonEncode(String)};
                    var count = {Count};
                    var flags = {Flags};
                    var caseSensitive = (flags & 4) !== 0;
                    var wholeWord = (flags & 2) !== 0;
                    
                    // Use window.find if available
                    if (window.find) {{
                        var found = window.find(searchText, caseSensitive, count < 0);
                        if (found) {{
                            var sel = window.getSelection();
                            if (sel.rangeCount > 0) {{
                                var newRange = sel.getRangeAt(0).cloneRange();
                                OLW.textRange.ranges.set({WebView2Bridge.JsonEncode(_rangeId)}, newRange);
                                return true;
                            }}
                        }}
                    }}
                    return false;
                }})()");
        }

        public void moveToPoint(int x, int y)
        {
            _bridge.ExecuteScript($@"
                (function() {{
                    var doc = document;
                    var range;
                    
                    if (doc.caretPositionFromPoint) {{
                        var pos = doc.caretPositionFromPoint({x}, {y});
                        if (pos) {{
                            range = doc.createRange();
                            range.setStart(pos.offsetNode, pos.offset);
                            range.collapse(true);
                        }}
                    }} else if (doc.caretRangeFromPoint) {{
                        range = doc.caretRangeFromPoint({x}, {y});
                    }}
                    
                    if (range) {{
                        OLW.textRange.ranges.set({WebView2Bridge.JsonEncode(_rangeId)}, range);
                    }}
                }})()");
        }

        public string getBookmark()
        {
            // Return a serialized bookmark
            return _bridge.ExecuteScript<string>($@"
                (function() {{
                    var range = OLW.textRange.ranges?.get({WebView2Bridge.JsonEncode(_rangeId)});
                    if (!range) return '';
                    
                    // Create a simple bookmark based on text position
                    var bookmark = {{
                        startOffset: range.startOffset,
                        endOffset: range.endOffset,
                        startPath: getNodePath(range.startContainer),
                        endPath: getNodePath(range.endContainer)
                    }};
                    
                    function getNodePath(node) {{
                        var path = [];
                        while (node && node !== document.body) {{
                            var parent = node.parentNode;
                            if (parent) {{
                                var index = Array.from(parent.childNodes).indexOf(node);
                                path.unshift(index);
                            }}
                            node = parent;
                        }}
                        return path;
                    }}
                    
                    return JSON.stringify(bookmark);
                }})()");
        }

        public bool moveToBookmark(string Bookmark)
        {
            return _bridge.ExecuteScript<bool>($@"
                (function() {{
                    try {{
                        var bookmark = JSON.parse({WebView2Bridge.JsonEncode(Bookmark)});
                        if (!bookmark) return false;
                        
                        function getNodeFromPath(path) {{
                            var node = document.body;
                            for (var i = 0; i < path.length && node; i++) {{
                                node = node.childNodes[path[i]];
                            }}
                            return node;
                        }}
                        
                        var startNode = getNodeFromPath(bookmark.startPath);
                        var endNode = getNodeFromPath(bookmark.endPath);
                        
                        if (startNode && endNode) {{
                            var range = document.createRange();
                            range.setStart(startNode, bookmark.startOffset);
                            range.setEnd(endNode, bookmark.endOffset);
                            OLW.textRange.ranges.set({WebView2Bridge.JsonEncode(_rangeId)}, range);
                            return true;
                        }}
                    }} catch (e) {{}}
                    return false;
                }})()");
        }

        public bool queryCommandSupported(string cmdID)
        {
            return _bridge.ExecuteScript<bool>($"document.queryCommandSupported({WebView2Bridge.JsonEncode(cmdID)})");
        }

        public bool queryCommandEnabled(string cmdID)
        {
            return _bridge.DocumentQueryCommandEnabled(cmdID);
        }

        public bool queryCommandState(string cmdID)
        {
            return _bridge.DocumentQueryCommandState(cmdID);
        }

        public bool queryCommandIndeterm(string cmdID)
        {
            return _bridge.ExecuteScript<bool>($"document.queryCommandIndeterm({WebView2Bridge.JsonEncode(cmdID)})");
        }

        public string queryCommandText(string cmdID)
        {
            return cmdID; // Not a standard method
        }

        public object queryCommandValue(string cmdID)
        {
            return _bridge.DocumentQueryCommandValue(cmdID);
        }

        public bool execCommand(string cmdID, bool showUI = false, object value = null)
        {
            return _bridge.TextRangeExecCommand(_rangeId, cmdID, value?.ToString());
        }

        public bool execCommandShowHelp(string cmdID)
        {
            return false; // Not supported
        }

        public void Dispose()
        {
            if (!_disposed && !string.IsNullOrEmpty(_rangeId))
            {
                _bridge.TextRangeDispose(_rangeId);
                _disposed = true;
            }
        }

        public override string ToString() => $"WebView2TextRange[{_rangeId}]: \"{text}\"";
    }
}
