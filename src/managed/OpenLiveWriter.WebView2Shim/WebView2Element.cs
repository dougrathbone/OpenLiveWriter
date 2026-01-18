// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using mshtml;

namespace OpenLiveWriter.WebView2Shim
{
    /// <summary>
    /// WebView2 element wrapper implementing IHTMLElement.
    /// Translates MSHTML interface calls to JavaScript DOM operations via WebView2.
    /// </summary>
    public class WebView2Element : IHTMLElement
    {
        private readonly WebView2Bridge _bridge;
        private readonly string _elementId;
        private WebView2Document _document;

        public WebView2Element(WebView2Bridge bridge, string elementId)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _elementId = elementId ?? throw new ArgumentNullException(nameof(elementId));
        }

        /// <summary>
        /// The unique OLW element ID (data-olw-id attribute value).
        /// </summary>
        public string ElementId => _elementId;

        /// <summary>
        /// Get the parent document (set by the document that created this element).
        /// </summary>
        internal WebView2Document ParentDocument 
        { 
            get => _document;
            set => _document = value;
        }

        // Helper to create child element
        private WebView2Element CreateElement(string childId)
        {
            if (string.IsNullOrEmpty(childId)) return null;
            var el = new WebView2Element(_bridge, childId);
            el.ParentDocument = _document;
            return el;
        }

        // ===== IHTMLElement Implementation =====

        public void setAttribute(string strAttributeName, object AttributeValue, int lFlags = 1)
        {
            _bridge.ElementSetAttribute(_elementId, strAttributeName, AttributeValue?.ToString());
        }

        public object getAttribute(string strAttributeName, int lFlags = 0)
        {
            return _bridge.ElementGetAttribute(_elementId, strAttributeName);
        }

        public bool removeAttribute(string strAttributeName, int lFlags = 1)
        {
            _bridge.ElementRemoveAttribute(_elementId, strAttributeName);
            return true;
        }

        public string className
        {
            get => _bridge.ElementGetClassName(_elementId);
            set => _bridge.ElementSetClassName(_elementId, value);
        }

        public string id
        {
            get => _bridge.ElementGetId(_elementId);
            set => _bridge.ElementSetAttribute(_elementId, "id", value);
        }

        public string tagName => _bridge.ElementGetTagName(_elementId);

        IHTMLElement IHTMLElement.parentElement => CreateElement(_bridge.ElementGetParentElement(_elementId));
        
        public WebView2Element parentElement => CreateElement(_bridge.ElementGetParentElement(_elementId));

        public IHTMLStyle style => new WebView2Style(_bridge, _elementId);

        // Event handlers - stub implementations (not typically used in OLW)
        public object onhelp { get; set; }
        public object onclick { get; set; }
        public object ondblclick { get; set; }
        public object onkeydown { get; set; }
        public object onkeyup { get; set; }
        public object onkeypress { get; set; }
        public object onmouseout { get; set; }
        public object onmouseover { get; set; }
        public object onmousemove { get; set; }
        public object onmousedown { get; set; }
        public object onmouseup { get; set; }
        public object onselectstart { get; set; }
        public object ondragstart { get; set; }
        public object onbeforeupdate { get; set; }
        public object onafterupdate { get; set; }
        public object onerrorupdate { get; set; }
        public object onrowexit { get; set; }
        public object onrowenter { get; set; }
        public object ondatasetchanged { get; set; }
        public object ondataavailable { get; set; }
        public object ondatasetcomplete { get; set; }
        public object onfilterchange { get; set; }

        public object document => _document;

        public string title
        {
            get => _bridge.ElementGetTitle(_elementId);
            set => _bridge.ElementSetTitle(_elementId, value);
        }

        public string language
        {
            get => _bridge.ElementGetAttribute(_elementId, "language");
            set => _bridge.ElementSetAttribute(_elementId, "language", value);
        }

        public void scrollIntoView(object varargStart = null)
        {
            bool alignTop = varargStart == null || (varargStart is bool b && b);
            _bridge.ElementScrollIntoView(_elementId, alignTop);
        }

        public bool contains(IHTMLElement pChild)
        {
            if (pChild == null) return false;
            if (pChild is WebView2Element wv2Child)
                return _bridge.ElementContains(_elementId, wv2Child.ElementId);
            return false;
        }

        public int sourceIndex => _bridge.ExecuteScript<int>($@"
            (function() {{
                var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                if (!el) return -1;
                var all = document.getElementsByTagName('*');
                for (var i = 0; i < all.length; i++) {{
                    if (all[i] === el) return i;
                }}
                return -1;
            }})()");

        public object recordNumber => null;

        public string lang
        {
            get => _bridge.ElementGetAttribute(_elementId, "lang");
            set => _bridge.ElementSetAttribute(_elementId, "lang", value);
        }

        public int offsetLeft => _bridge.ElementGetOffsetLeft(_elementId);
        public int offsetTop => _bridge.ElementGetOffsetTop(_elementId);
        public int offsetWidth => _bridge.ElementGetOffsetWidth(_elementId);
        public int offsetHeight => _bridge.ElementGetOffsetHeight(_elementId);
        
        IHTMLElement IHTMLElement.offsetParent => CreateElement(_bridge.ElementGetOffsetParent(_elementId));
        
        public WebView2Element offsetParent => CreateElement(_bridge.ElementGetOffsetParent(_elementId));

        public string innerHTML
        {
            get => _bridge.ElementGetInnerHTML(_elementId);
            set => _bridge.ElementSetInnerHTML(_elementId, value);
        }

        public string innerText
        {
            get => _bridge.ElementGetInnerText(_elementId);
            set => _bridge.ElementSetInnerText(_elementId, value);
        }

        public string outerHTML
        {
            get => _bridge.ElementGetOuterHTML(_elementId);
            set => _bridge.ElementSetOuterHTML(_elementId, value);
        }

        public string outerText
        {
            get => _bridge.ExecuteScript<string>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.outerText ?? ''");
            set => _bridge.ExecuteScript($"var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)}); if(el) el.outerText = {WebView2Bridge.JsonEncode(value)}");
        }

        public void insertAdjacentHTML(string where, string html)
        {
            _bridge.ElementInsertAdjacentHTML(_elementId, where, html);
        }

        public void insertAdjacentText(string where, string text)
        {
            _bridge.ExecuteScript($@"
                var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                if (el) el.insertAdjacentText({WebView2Bridge.JsonEncode(where)}, {WebView2Bridge.JsonEncode(text)})");
        }

        IHTMLElement IHTMLElement.parentTextEdit => null; // Not typically used
        
        public WebView2Element parentTextEdit => null;

        public bool isTextEdit => _bridge.ElementIsTextEdit(_elementId);

        public void click() => _bridge.ElementClick(_elementId);

        public IHTMLFiltersCollection filters => null; // Deprecated IE feature

        string IHTMLElement.toString() => ToString();

        public object children
        {
            get
            {
                var childIds = _bridge.ElementGetChildren(_elementId);
                return new WebView2ElementCollection(_bridge, childIds, _document);
            }
        }

        public object all
        {
            get
            {
                var ids = _bridge.ExecuteScript<string[]>($@"
                    (function() {{
                        var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                        if (!el) return [];
                        var elems = el.getElementsByTagName('*');
                        var result = [];
                        for (var i = 0; i < elems.length; i++) {{
                            var id = elems[i].getAttribute('data-olw-id');
                            if (!id) {{
                                id = 'olw-' + Date.now() + '-' + i;
                                elems[i].setAttribute('data-olw-id', id);
                            }}
                            result.push(id);
                        }}
                        return result;
                    }})()") ?? Array.Empty<string>();
                return new WebView2ElementCollection(_bridge, ids, _document);
            }
        }

        // ===== IHTMLElement2-like members =====

        public void focus() => _bridge.ExecuteScript($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.focus()");
        public void blur() => _bridge.ExecuteScript($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.blur()");

        public int clientHeight => _bridge.ExecuteScript<int>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.clientHeight ?? 0");
        public int clientWidth => _bridge.ExecuteScript<int>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.clientWidth ?? 0");
        public int clientTop => _bridge.ExecuteScript<int>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.clientTop ?? 0");
        public int clientLeft => _bridge.ExecuteScript<int>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.clientLeft ?? 0");

        public int scrollHeight => _bridge.ExecuteScript<int>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.scrollHeight ?? 0");
        public int scrollWidth => _bridge.ExecuteScript<int>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.scrollWidth ?? 0");
        
        public int scrollTop
        {
            get => _bridge.ExecuteScript<int>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.scrollTop ?? 0");
            set => _bridge.ExecuteScript($"var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)}); if(el) el.scrollTop = {value}");
        }

        public int scrollLeft
        {
            get => _bridge.ExecuteScript<int>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.scrollLeft ?? 0");
            set => _bridge.ExecuteScript($"var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)}); if(el) el.scrollLeft = {value}");
        }

        public string dir
        {
            get => _bridge.ElementGetAttribute(_elementId, "dir");
            set => _bridge.ElementSetAttribute(_elementId, "dir", value);
        }

        public WebView2ElementCollection getElementsByTagName(string tagName)
        {
            var ids = _bridge.ExecuteScript<string[]>($@"
                (function() {{
                    var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                    if (!el) return [];
                    var elems = el.getElementsByTagName({WebView2Bridge.JsonEncode(tagName)});
                    var result = [];
                    for (var i = 0; i < elems.length; i++) {{
                        var id = elems[i].getAttribute('data-olw-id');
                        if (!id) {{
                            id = 'olw-' + Date.now() + '-' + i;
                            elems[i].setAttribute('data-olw-id', id);
                        }}
                        result.push(id);
                    }}
                    return result;
                }})()") ?? Array.Empty<string>();
            return new WebView2ElementCollection(_bridge, ids, _document);
        }

        // ===== IHTMLElement3-like members =====

        public string contentEditable
        {
            get => _bridge.ElementGetAttribute(_elementId, "contenteditable") ?? "inherit";
            set => _bridge.ElementSetAttribute(_elementId, "contenteditable", value);
        }

        public bool isContentEditable => _bridge.ExecuteScript<bool>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.isContentEditable ?? false");

        public bool disabled
        {
            get => _bridge.ExecuteScript<bool>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.disabled ?? false");
            set => _bridge.ExecuteScript($"var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)}); if(el) el.disabled = {(value ? "true" : "false")}");
        }

        // ===== DOM Node-like members =====

        public WebView2Element firstChild => CreateElement(_bridge.ExecuteScript<string>($@"
            (function() {{
                var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                if (!el || !el.firstElementChild) return null;
                var id = el.firstElementChild.getAttribute('data-olw-id');
                if (!id) {{
                    id = 'olw-' + Date.now();
                    el.firstElementChild.setAttribute('data-olw-id', id);
                }}
                return id;
            }})()"));

        public WebView2Element lastChild => CreateElement(_bridge.ExecuteScript<string>($@"
            (function() {{
                var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                if (!el || !el.lastElementChild) return null;
                var id = el.lastElementChild.getAttribute('data-olw-id');
                if (!id) {{
                    id = 'olw-' + Date.now();
                    el.lastElementChild.setAttribute('data-olw-id', id);
                }}
                return id;
            }})()"));

        public WebView2Element nextSibling => CreateElement(_bridge.ExecuteScript<string>($@"
            (function() {{
                var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                if (!el || !el.nextElementSibling) return null;
                var id = el.nextElementSibling.getAttribute('data-olw-id');
                if (!id) {{
                    id = 'olw-' + Date.now();
                    el.nextElementSibling.setAttribute('data-olw-id', id);
                }}
                return id;
            }})()"));

        public WebView2Element previousSibling => CreateElement(_bridge.ExecuteScript<string>($@"
            (function() {{
                var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                if (!el || !el.previousElementSibling) return null;
                var id = el.previousElementSibling.getAttribute('data-olw-id');
                if (!id) {{
                    id = 'olw-' + Date.now();
                    el.previousElementSibling.setAttribute('data-olw-id', id);
                }}
                return id;
            }})()"));

        public bool hasChildNodes()
        {
            return _bridge.ExecuteScript<bool>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.hasChildNodes() ?? false");
        }

        public WebView2Element appendChild(WebView2Element child)
        {
            if (child == null) return null;
            _bridge.ExecuteScript($@"
                var parent = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                var child = OLW.util.getById({WebView2Bridge.JsonEncode(child.ElementId)});
                if (parent && child) parent.appendChild(child);");
            return child;
        }

        public WebView2Element removeChild(WebView2Element child)
        {
            if (child == null) return null;
            _bridge.ExecuteScript($@"
                var parent = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                var child = OLW.util.getById({WebView2Bridge.JsonEncode(child.ElementId)});
                if (parent && child) parent.removeChild(child);");
            return child;
        }

        public override string ToString()
        {
            return $"WebView2Element[{_elementId}]: <{tagName}>";
        }
    }
}
