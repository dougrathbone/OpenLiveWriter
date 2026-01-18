// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections;
using mshtml;

namespace OpenLiveWriter.WebView2Shim
{
    /// <summary>
    /// WebView2 implementation of IHTMLElement that delegates to JavaScript via the bridge.
    /// </summary>
    public class WebView2Element : IHTMLElement, IHTMLElement2, IHTMLElement3
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
            return true; // MSHTML always returns true
        }

        public string className
        {
            get => _bridge.ElementGetClassName(_elementId);
            set => _bridge.ElementSetClassName(_elementId, value);
        }

        public string id
        {
            // Note: This is the HTML id attribute, not our internal OLW element ID
            get => _bridge.ElementGetId(_elementId);
            set => _bridge.ElementSetAttribute(_elementId, "id", value);
        }

        public string tagName => _bridge.ElementGetTagName(_elementId);

        public IHTMLElement parentElement => CreateElement(_bridge.ElementGetParentElement(_elementId));

        public IHTMLStyle style
        {
            get
            {
                // Return a shim that wraps style property access
                return new WebView2Style(_bridge, _elementId);
            }
        }

        // Event handlers - not implemented, would require complex JS event forwarding
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
            bool alignTop = true;
            if (varargStart is bool b) alignTop = b;
            _bridge.ElementScrollIntoView(_elementId, alignTop);
        }

        public bool contains(IHTMLElement pChild)
        {
            if (pChild is WebView2Element w2e)
            {
                return _bridge.ElementContains(_elementId, w2e.ElementId);
            }
            return false;
        }

        public int sourceIndex => -1; // Would need expensive calculation

        public object recordNumber => null; // Legacy data binding, not implemented

        public string lang
        {
            get => _bridge.ElementGetAttribute(_elementId, "lang");
            set => _bridge.ElementSetAttribute(_elementId, "lang", value);
        }

        public int offsetLeft => _bridge.ElementGetOffsetLeft(_elementId);
        public int offsetTop => _bridge.ElementGetOffsetTop(_elementId);
        public int offsetWidth => _bridge.ElementGetOffsetWidth(_elementId);
        public int offsetHeight => _bridge.ElementGetOffsetHeight(_elementId);
        public IHTMLElement offsetParent => CreateElement(_bridge.ElementGetOffsetParent(_elementId));

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
            get => _bridge.ElementGetAttribute(_elementId, "outerText"); // Less common
            set => _bridge.ElementSetAttribute(_elementId, "outerText", value);
        }

        public void insertAdjacentHTML(string where, string html)
        {
            _bridge.ElementInsertAdjacentHTML(_elementId, where, html);
        }

        public void insertAdjacentText(string where, string text)
        {
            _bridge.ExecuteScript($"OLW.element.insertAdjacentText({WebView2Bridge.JsonEncode(_elementId)}, {WebView2Bridge.JsonEncode(where)}, {WebView2Bridge.JsonEncode(text)})");
        }

        public IHTMLElement parentTextEdit
        {
            get
            {
                // Find the nearest ancestor that is editable
                var parentId = _bridge.ExecuteScript<string>($@"
                    (function() {{
                        var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                        while (el && el.parentElement) {{
                            el = el.parentElement;
                            if (el.isContentEditable || el.tagName === 'INPUT' || el.tagName === 'TEXTAREA') {{
                                return OLW.element.getId(el) || null;
                            }}
                        }}
                        return null;
                    }})()");
                return CreateElement(parentId);
            }
        }

        public bool isTextEdit => _bridge.ElementIsTextEdit(_elementId);

        public void click() => _bridge.ElementClick(_elementId);

        public IHTMLFiltersCollection filters => null; // Legacy IE filters, not implemented

        public string toString() => $"[object {tagName}]";

        public object children
        {
            get
            {
                var childIds = _bridge.ElementGetChildren(_elementId);
                var elements = new WebView2ElementCollection(_bridge, childIds, _document);
                return elements;
            }
        }

        public object all
        {
            get
            {
                var descendantIds = _bridge.ExecuteScript<string[]>($"OLW.element.getAllDescendants({WebView2Bridge.JsonEncode(_elementId)})") 
                    ?? Array.Empty<string>();
                return new WebView2ElementCollection(_bridge, descendantIds, _document);
            }
        }

        // ===== IHTMLElement2 Implementation =====

        public string scopeName => tagName.Contains(":") ? tagName.Split(':')[0] : null;

        public void setCapture(bool containerCapture = true)
        {
            // Mouse capture - not typically needed in WebView2
        }

        public void releaseCapture()
        {
            // Mouse capture - not typically needed in WebView2
        }

        public object onlosecapture { get; set; }
        public object onscroll { get; set; }

        public bool dragDrop() => false; // OLE drag-drop, not implemented

        public void blur() => _bridge.ExecuteScript($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.blur()");
        
        public void focus() => _bridge.ExecuteScript($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.focus()");

        public object onblur { get; set; }
        public object onfocus { get; set; }
        public object onresize { get; set; }

        public void addFilter(object pUnk) { /* IE filters, not implemented */ }
        public void removeFilter(object pUnk) { /* IE filters, not implemented */ }

        public int clientHeight => _bridge.ExecuteScript<int>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.clientHeight ?? 0");
        public int clientWidth => _bridge.ExecuteScript<int>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.clientWidth ?? 0");
        public int clientTop => _bridge.ExecuteScript<int>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.clientTop ?? 0");
        public int clientLeft => _bridge.ExecuteScript<int>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.clientLeft ?? 0");

        public bool attachEvent(string @event, object pdisp) => false; // Legacy IE event model
        public void detachEvent(string @event, object pdisp) { /* Legacy IE event model */ }

        public object readyState => _bridge.DocumentGetReadyState();

        public object onreadystatechange { get; set; }
        public object onrowsdelete { get; set; }
        public object onrowsinserted { get; set; }
        public object oncellchange { get; set; }

        public string dir
        {
            get => _bridge.ElementGetAttribute(_elementId, "dir");
            set => _bridge.ElementSetAttribute(_elementId, "dir", value);
        }

        public object createControlRange() => null; // Legacy, not implemented

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

        public void clearAttributes()
        {
            _bridge.ExecuteScript($@"
                var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                if (el) {{
                    while (el.attributes.length > 0) {{
                        el.removeAttribute(el.attributes[0].name);
                    }}
                }}");
        }

        public void mergeAttributes(IHTMLElement mergeThis, object pvarFlags = null)
        {
            // Copy attributes from another element
            if (mergeThis is WebView2Element other)
            {
                _bridge.ExecuteScript($@"
                    var src = OLW.util.getById({WebView2Bridge.JsonEncode(other.ElementId)});
                    var dest = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                    if (src && dest) {{
                        for (var i = 0; i < src.attributes.length; i++) {{
                            dest.setAttribute(src.attributes[i].name, src.attributes[i].value);
                        }}
                    }}");
            }
        }

        public object oncontextmenu { get; set; }

        public IHTMLElement insertAdjacentElement(string where, IHTMLElement insertedElement)
        {
            if (insertedElement is WebView2Element w2e)
            {
                _bridge.ExecuteScript($@"
                    var target = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                    var toInsert = OLW.util.getById({WebView2Bridge.JsonEncode(w2e.ElementId)});
                    if (target && toInsert) {{
                        target.insertAdjacentElement({WebView2Bridge.JsonEncode(where)}, toInsert);
                    }}");
                return insertedElement;
            }
            return null;
        }

        public IHTMLElement applyElement(IHTMLElement apply, string where = "outside")
        {
            // Wrap this element with another element
            // Not commonly used, basic implementation
            return this;
        }

        public string getAdjacentText(string where)
        {
            return _bridge.ExecuteScript<string>($@"
                var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                if (!el) return '';
                switch({WebView2Bridge.JsonEncode(where)}.toLowerCase()) {{
                    case 'beforebegin': return el.previousSibling?.textContent ?? '';
                    case 'afterbegin': return el.firstChild?.textContent ?? '';
                    case 'beforeend': return el.lastChild?.textContent ?? '';
                    case 'afterend': return el.nextSibling?.textContent ?? '';
                    default: return '';
                }}");
        }

        public string replaceAdjacentText(string where, string newText)
        {
            var oldText = getAdjacentText(where);
            _bridge.ExecuteScript($@"
                var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                if (el) el.insertAdjacentText({WebView2Bridge.JsonEncode(where)}, {WebView2Bridge.JsonEncode(newText)});");
            return oldText;
        }

        public bool canHaveChildren
        {
            get
            {
                // Elements like IMG, BR, HR can't have children
                var tag = tagName?.ToUpperInvariant();
                return tag != "IMG" && tag != "BR" && tag != "HR" && tag != "INPUT" && 
                       tag != "META" && tag != "LINK" && tag != "BASE" && tag != "AREA";
            }
        }

        public int addBehavior(string bstrUrl, ref object pvarFactory) => 0; // IE behaviors, not implemented
        public bool removeBehavior(int cookie) => false; // IE behaviors, not implemented

        public IHTMLStyle runtimeStyle => style; // Return same style object

        public object behaviorUrns => null; // IE behaviors, not implemented

        public string tagUrn
        {
            get => _bridge.ElementGetAttribute(_elementId, "xmlns");
            set => _bridge.ElementSetAttribute(_elementId, "xmlns", value);
        }

        public object onbeforeeditfocus { get; set; }

        public int readyStateValue => 4; // 4 = complete

        public IHTMLElementCollection getElementsByTagName(string v)
        {
            var ids = _bridge.ExecuteScript<string[]>($@"
                (function() {{
                    var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                    if (!el) return [];
                    var elems = el.getElementsByTagName({WebView2Bridge.JsonEncode(v)});
                    var result = [];
                    for (var i = 0; i < elems.length; i++) {{
                        result.push(OLW.element.getId(elems[i]) || '');
                    }}
                    return result.filter(x => x);
                }})()") ?? Array.Empty<string>();
            return new WebView2ElementCollection(_bridge, ids, _document);
        }

        // ===== IHTMLElement3 Implementation =====

        public void mergeAttributes2(IHTMLElement mergeThis, object pvarFlags = null)
        {
            mergeAttributes(mergeThis, pvarFlags);
        }

        public bool isMultiLine => _bridge.ExecuteScript<bool>($@"
            var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
            return el && (el.tagName === 'TEXTAREA' || el.isContentEditable);");

        public bool canHaveHTML => canHaveChildren;

        public object onlayoutcomplete { get; set; }
        public object onpage { get; set; }

        public bool inflateBlock
        {
            get => false;
            set { /* Not implemented */ }
        }

        public object onbeforedeactivate { get; set; }

        public void setActive()
        {
            focus();
        }

        public string contentEditable
        {
            get => _bridge.ElementGetAttribute(_elementId, "contenteditable") ?? "inherit";
            set => _bridge.ElementSetAttribute(_elementId, "contenteditable", value);
        }

        public bool isContentEditable => _bridge.ExecuteScript<bool>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.isContentEditable ?? false");

        public bool hideFocus
        {
            get => _bridge.ElementGetAttribute(_elementId, "hidefocus") == "true";
            set => _bridge.ElementSetAttribute(_elementId, "hidefocus", value ? "true" : "false");
        }

        public bool disabled
        {
            get => _bridge.ExecuteScript<bool>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.disabled ?? false");
            set => _bridge.ExecuteScript($"var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)}); if(el) el.disabled = {(value ? "true" : "false")}");
        }

        public bool isDisabled => disabled;

        public object onmove { get; set; }
        public object oncontrolselect { get; set; }

        public bool fireEvent(string bstrEventName, ref object pvarEventObject) => false;

        public object onresizestart { get; set; }
        public object onresizeend { get; set; }
        public object onmovestart { get; set; }
        public object onmoveend { get; set; }
        public object onmouseenter { get; set; }
        public object onmouseleave { get; set; }
        public object onactivate { get; set; }
        public object ondeactivate { get; set; }

        public bool setExpression(string propname, string expression, string language = "")
        {
            // IE expression, not implemented
            return false;
        }

        public object getExpression(string propname) => null;

        public bool removeExpression(string propname) => false;

        public short tabIndex
        {
            get => _bridge.ExecuteScript<short>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.tabIndex ?? 0");
            set => _bridge.ExecuteScript($"var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)}); if(el) el.tabIndex = {value}");
        }

        public string accessKey
        {
            get => _bridge.ElementGetAttribute(_elementId, "accesskey");
            set => _bridge.ElementSetAttribute(_elementId, "accesskey", value);
        }

        public object onpaste { get; set; }

        public IHTMLCurrentStyle currentStyle => null; // Would need another shim class

        public object oncopy { get; set; }
        public object oncut { get; set; }
        public object onbeforecopy { get; set; }
        public object onbeforecut { get; set; }
        public object onbeforepaste { get; set; }

        public bool hasLayout => true; // All elements in modern browsers have layout
        public object onpropertychange { get; set; }

        public object filters2 => null;

        public IHTMLAttributeCollection attributes => null; // Would need shim

        public IHTMLStyle2 runtimeStyle2 => null; // Would need shim

        public object onfocusin { get; set; }
        public object onfocusout { get; set; }

        public int uniqueNumber => _elementId.GetHashCode();

        public string uniqueID => _elementId;

        public int nodeType => 1; // ELEMENT_NODE

        public IHTMLDOMNode parentNode => parentElement as IHTMLDOMNode;

        public bool hasChildNodes()
        {
            return _bridge.ExecuteScript<bool>($"OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)})?.hasChildNodes() ?? false");
        }

        public object childNodes => children;
        
        public IHTMLDOMAttribute getAttributeNode(string bstrname)
        {
            return null; // Would need shim
        }

        public IHTMLDOMAttribute setAttributeNode(IHTMLDOMAttribute pattr)
        {
            return null; // Would need shim
        }

        public IHTMLDOMAttribute removeAttributeNode(IHTMLDOMAttribute pattr)
        {
            return null; // Would need shim
        }

        public IHTMLDOMNode firstChild => CreateElement(_bridge.ExecuteScript<string>($@"
            (function() {{
                var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                if (!el || !el.firstElementChild) return null;
                return OLW.element.getId(el.firstElementChild);
            }})()")) as IHTMLDOMNode;

        public IHTMLDOMNode lastChild => CreateElement(_bridge.ExecuteScript<string>($@"
            (function() {{
                var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                if (!el || !el.lastElementChild) return null;
                return OLW.element.getId(el.lastElementChild);
            }})()")) as IHTMLDOMNode;

        public IHTMLDOMNode previousSibling => CreateElement(_bridge.ExecuteScript<string>($@"
            (function() {{
                var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                if (!el || !el.previousElementSibling) return null;
                return OLW.element.getId(el.previousElementSibling);
            }})()")) as IHTMLDOMNode;

        public IHTMLDOMNode nextSibling => CreateElement(_bridge.ExecuteScript<string>($@"
            (function() {{
                var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                if (!el || !el.nextElementSibling) return null;
                return OLW.element.getId(el.nextElementSibling);
            }})()")) as IHTMLDOMNode;

        public IHTMLDocument2 ownerDocument => _document;

        public object nodeValue
        {
            get => null; // Element nodes don't have nodeValue
            set { /* Element nodes don't have nodeValue */ }
        }

        public string nodeName => tagName;

        public IHTMLDOMNode appendChild(IHTMLDOMNode newChild)
        {
            if (newChild is WebView2Element child)
            {
                _bridge.ExecuteScript($@"
                    var parent = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                    var child = OLW.util.getById({WebView2Bridge.JsonEncode(child.ElementId)});
                    if (parent && child) parent.appendChild(child);");
                return newChild;
            }
            return null;
        }

        public IHTMLDOMNode insertBefore(IHTMLDOMNode newChild, object refChild = null)
        {
            if (newChild is WebView2Element child)
            {
                string refId = (refChild as WebView2Element)?.ElementId;
                _bridge.ExecuteScript($@"
                    var parent = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                    var child = OLW.util.getById({WebView2Bridge.JsonEncode(child.ElementId)});
                    var ref = {(refId != null ? $"OLW.util.getById({WebView2Bridge.JsonEncode(refId)})" : "null")};
                    if (parent && child) parent.insertBefore(child, ref);");
                return newChild;
            }
            return null;
        }

        public IHTMLDOMNode removeChild(IHTMLDOMNode oldChild)
        {
            if (oldChild is WebView2Element child)
            {
                _bridge.ExecuteScript($@"
                    var parent = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                    var child = OLW.util.getById({WebView2Bridge.JsonEncode(child.ElementId)});
                    if (parent && child) parent.removeChild(child);");
                return oldChild;
            }
            return null;
        }

        public IHTMLDOMNode replaceChild(IHTMLDOMNode newChild, IHTMLDOMNode oldChild)
        {
            if (newChild is WebView2Element newEl && oldChild is WebView2Element oldEl)
            {
                _bridge.ExecuteScript($@"
                    var parent = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                    var newC = OLW.util.getById({WebView2Bridge.JsonEncode(newEl.ElementId)});
                    var oldC = OLW.util.getById({WebView2Bridge.JsonEncode(oldEl.ElementId)});
                    if (parent && newC && oldC) parent.replaceChild(newC, oldC);");
                return oldChild;
            }
            return null;
        }

        public IHTMLDOMNode cloneNode(bool fDeep)
        {
            var newId = _bridge.ExecuteScript<string>($@"
                (function() {{
                    var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                    if (!el) return null;
                    var clone = el.cloneNode({(fDeep ? "true" : "false")});
                    clone.removeAttribute('data-olw-id');
                    document.body.appendChild(clone); // Temporarily add to DOM so we can track it
                    var id = 'olw-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
                    clone.setAttribute('data-olw-id', id);
                    return id;
                }})()");
            return CreateElement(newId);
        }

        public IHTMLDOMNode removeNode(bool fDeep = false)
        {
            _bridge.ExecuteScript($@"
                var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                if (el && el.parentNode) el.parentNode.removeChild(el);");
            return this;
        }

        public IHTMLDOMNode swapNode(IHTMLDOMNode otherNode)
        {
            if (otherNode is WebView2Element other)
            {
                _bridge.ExecuteScript($@"
                    var el1 = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                    var el2 = OLW.util.getById({WebView2Bridge.JsonEncode(other.ElementId)});
                    if (el1 && el2) {{
                        var parent1 = el1.parentNode;
                        var next1 = el1.nextSibling;
                        var parent2 = el2.parentNode;
                        var next2 = el2.nextSibling;
                        parent1.insertBefore(el2, next1);
                        parent2.insertBefore(el1, next2);
                    }}");
                return otherNode;
            }
            return null;
        }

        public IHTMLDOMNode replaceNode(IHTMLDOMNode replacement)
        {
            if (replacement is WebView2Element other)
            {
                _bridge.ExecuteScript($@"
                    var el = OLW.util.getById({WebView2Bridge.JsonEncode(_elementId)});
                    var repl = OLW.util.getById({WebView2Bridge.JsonEncode(other.ElementId)});
                    if (el && el.parentNode && repl) el.parentNode.replaceChild(repl, el);");
                return this;
            }
            return null;
        }

        // ToString for debugging
        public override string ToString()
        {
            return $"WebView2Element[{_elementId}]: <{tagName}>";
        }
    }
}
