// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Microsoft.Web.WebView2.WinForms;
using mshtml;

namespace OpenLiveWriter.WebView2Shim
{
    /// <summary>
    /// WebView2 implementation of IHTMLDocument2/3 that delegates to JavaScript via the bridge.
    /// </summary>
    public class WebView2Document : IHTMLDocument2, IHTMLDocument3, IHTMLDocument4
    {
        private readonly WebView2Bridge _bridge;
        private readonly WebView2 _webView;

        public WebView2Document(WebView2Bridge bridge, WebView2 webView)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        }

        /// <summary>
        /// The underlying WebView2Bridge for direct script execution.
        /// </summary>
        public WebView2Bridge Bridge => _bridge;

        /// <summary>
        /// The underlying WebView2 control.
        /// </summary>
        public WebView2 WebView => _webView;

        // Helper to create element
        private WebView2Element CreateElement(string elementId)
        {
            if (string.IsNullOrEmpty(elementId)) return null;
            var el = new WebView2Element(_bridge, elementId);
            el.ParentDocument = this;
            return el;
        }

        // Helper to create element collection
        private WebView2ElementCollection CreateCollection(string[] ids)
        {
            return new WebView2ElementCollection(_bridge, ids ?? Array.Empty<string>(), this);
        }

        // ===== IHTMLDocument Implementation =====

        public object Script => null; // JavaScript interop not exposed this way

        // ===== IHTMLDocument2 Implementation =====

        public IHTMLElementCollection all
        {
            get
            {
                var ids = _bridge.DocumentGetElementsByTagName("*");
                return CreateCollection(ids);
            }
        }

        public IHTMLElement body => CreateElement(_bridge.DocumentGetBody());

        public IHTMLElement activeElement => CreateElement(_bridge.DocumentGetActiveElement());

        public IHTMLElementCollection images
        {
            get => CreateCollection(_bridge.DocumentGetElementsByTagName("img"));
        }

        public IHTMLElementCollection applets
        {
            get => CreateCollection(_bridge.DocumentGetElementsByTagName("applet"));
        }

        public IHTMLElementCollection links
        {
            get => CreateCollection(_bridge.ExecuteScript<string[]>(@"
                (function() {
                    var result = [];
                    var links = document.querySelectorAll('a[href], area[href]');
                    for (var i = 0; i < links.length; i++) {
                        var id = links[i].getAttribute('data-olw-id');
                        if (!id) {
                            id = 'olw-' + Date.now() + '-' + i;
                            links[i].setAttribute('data-olw-id', id);
                        }
                        result.push(id);
                    }
                    return result;
                })()"));
        }

        public IHTMLElementCollection forms
        {
            get => CreateCollection(_bridge.DocumentGetElementsByTagName("form"));
        }

        public IHTMLElementCollection anchors
        {
            get => CreateCollection(_bridge.ExecuteScript<string[]>(@"
                (function() {
                    var result = [];
                    var anchors = document.querySelectorAll('a[name]');
                    for (var i = 0; i < anchors.length; i++) {
                        var id = anchors[i].getAttribute('data-olw-id');
                        if (!id) {
                            id = 'olw-' + Date.now() + '-' + i;
                            anchors[i].setAttribute('data-olw-id', id);
                        }
                        result.push(id);
                    }
                    return result;
                })()"));
        }

        public string title
        {
            get => _bridge.DocumentGetTitle();
            set => _bridge.DocumentSetTitle(value);
        }

        public IHTMLElementCollection scripts
        {
            get => CreateCollection(_bridge.DocumentGetElementsByTagName("script"));
        }

        public string designMode
        {
            get => _bridge.ExecuteScript<string>("document.designMode");
            set => _bridge.ExecuteScript($"document.designMode = {WebView2Bridge.JsonEncode(value)}");
        }

        public IHTMLSelectionObject selection => new WebView2Selection(_bridge, this);

        public string readyState => _bridge.DocumentGetReadyState();

        public FramesCollection frames => null; // Not implemented

        public IHTMLElementCollection embeds
        {
            get => CreateCollection(_bridge.DocumentGetElementsByTagName("embed"));
        }

        public IHTMLElementCollection plugins
        {
            get => CreateCollection(_bridge.DocumentGetElementsByTagName("object"));
        }

        public object alinkColor
        {
            get => _bridge.ExecuteScript<string>("document.alinkColor");
            set => _bridge.ExecuteScript($"document.alinkColor = {WebView2Bridge.JsonEncode(value?.ToString())}");
        }

        public object bgColor
        {
            get => _bridge.ExecuteScript<string>("document.bgColor");
            set => _bridge.ExecuteScript($"document.bgColor = {WebView2Bridge.JsonEncode(value?.ToString())}");
        }

        public object fgColor
        {
            get => _bridge.ExecuteScript<string>("document.fgColor");
            set => _bridge.ExecuteScript($"document.fgColor = {WebView2Bridge.JsonEncode(value?.ToString())}");
        }

        public object linkColor
        {
            get => _bridge.ExecuteScript<string>("document.linkColor");
            set => _bridge.ExecuteScript($"document.linkColor = {WebView2Bridge.JsonEncode(value?.ToString())}");
        }

        public object vlinkColor
        {
            get => _bridge.ExecuteScript<string>("document.vlinkColor");
            set => _bridge.ExecuteScript($"document.vlinkColor = {WebView2Bridge.JsonEncode(value?.ToString())}");
        }

        public string referrer => _bridge.ExecuteScript<string>("document.referrer");

        public HTMLLocation location => null; // Would need shim

        public string lastModified => _bridge.ExecuteScript<string>("document.lastModified");

        public string url
        {
            get => _bridge.DocumentGetURL();
            set
            {
                // Navigate to new URL
                _webView.CoreWebView2?.Navigate(value);
            }
        }

        public string domain
        {
            get => _bridge.ExecuteScript<string>("document.domain");
            set => _bridge.ExecuteScript($"document.domain = {WebView2Bridge.JsonEncode(value)}");
        }

        public string cookie
        {
            get => _bridge.ExecuteScript<string>("document.cookie");
            set => _bridge.ExecuteScript($"document.cookie = {WebView2Bridge.JsonEncode(value)}");
        }

        public bool expando
        {
            get => true; // Always true in modern browsers
            set { /* Ignored */ }
        }

        public string charset
        {
            get => _bridge.ExecuteScript<string>("document.charset || document.characterSet");
            set { /* Read-only in modern browsers */ }
        }

        public string defaultCharset
        {
            get => _bridge.ExecuteScript<string>("document.characterSet");
            set { /* Read-only in modern browsers */ }
        }

        public string mimeType => _bridge.ExecuteScript<string>("document.contentType");
        public string fileSize => "0"; // Not available in modern DOM
        public string fileCreatedDate => ""; // Not available in modern DOM
        public string fileModifiedDate => _bridge.ExecuteScript<string>("document.lastModified");
        public string fileUpdatedDate => _bridge.ExecuteScript<string>("document.lastModified");
        public string security => ""; // Not available in modern DOM
        public string protocol => _bridge.ExecuteScript<string>("document.location.protocol");
        public string nameProp => title;

        public void write(params object[] psarray)
        {
            if (psarray != null && psarray.Length > 0)
            {
                var html = string.Join("", psarray);
                _bridge.ExecuteScript($"document.write({WebView2Bridge.JsonEncode(html)})");
            }
        }

        public void writeln(params object[] psarray)
        {
            if (psarray != null && psarray.Length > 0)
            {
                var html = string.Join("", psarray) + "\n";
                _bridge.ExecuteScript($"document.write({WebView2Bridge.JsonEncode(html)})");
            }
        }

        public object open(string url = "text/html", object name = null, object features = null, object replace = null)
        {
            _bridge.ExecuteScript("document.open()");
            return this;
        }

        public void close()
        {
            _bridge.ExecuteScript("document.close()");
        }

        public void clear()
        {
            _bridge.ExecuteScript("document.body.innerHTML = ''");
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
            // Not a standard DOM method, return command name
            return cmdID;
        }

        public object queryCommandValue(string cmdID)
        {
            return _bridge.DocumentQueryCommandValue(cmdID);
        }

        public bool execCommand(string cmdID, bool showUI = false, object value = null)
        {
            return _bridge.DocumentExecCommand(cmdID, showUI, value?.ToString());
        }

        public bool execCommandShowHelp(string cmdID)
        {
            return false; // Not supported in modern browsers
        }

        public IHTMLElement createElement(string eTag)
        {
            var id = _bridge.DocumentCreateElement(eTag);
            return CreateElement(id);
        }

        // Event handlers - not implemented
        public object onhelp { get; set; }
        public object onclick { get; set; }
        public object ondblclick { get; set; }
        public object onkeyup { get; set; }
        public object onkeydown { get; set; }
        public object onkeypress { get; set; }
        public object onmouseup { get; set; }
        public object onmousedown { get; set; }
        public object onmousemove { get; set; }
        public object onmouseout { get; set; }
        public object onmouseover { get; set; }
        public object onreadystatechange { get; set; }
        public object onafterupdate { get; set; }
        public object onrowexit { get; set; }
        public object onrowenter { get; set; }
        public object ondragstart { get; set; }
        public object onselectstart { get; set; }
        public object onbeforeupdate { get; set; }
        public object onerrorupdate { get; set; }

        public IHTMLElement elementFromPoint(int x, int y)
        {
            return CreateElement(_bridge.DocumentElementFromPoint(x, y));
        }

        public IHTMLWindow2 parentWindow => null; // Would need shim

        public HTMLStyleSheetsCollection styleSheets => null; // Would need shim

        public string toString() => "[object HTMLDocument]";

        public IHTMLStyleSheet createStyleSheet(string bstrHref = "", int lIndex = -1)
        {
            // Create a style element
            _bridge.ExecuteScript($@"
                (function() {{
                    var style = document.createElement('style');
                    if ({WebView2Bridge.JsonEncode(bstrHref)}) {{
                        var link = document.createElement('link');
                        link.rel = 'stylesheet';
                        link.href = {WebView2Bridge.JsonEncode(bstrHref)};
                        document.head.appendChild(link);
                    }} else {{
                        document.head.appendChild(style);
                    }}
                }})()");
            return null; // Would need shim
        }

        // ===== IHTMLDocument3 Implementation =====

        public void releaseCapture()
        {
            _bridge.ExecuteScript("document.releaseCapture && document.releaseCapture()");
        }

        public void recalc(bool fForce = false)
        {
            // Not supported in modern browsers - layout is automatic
        }

        public IHTMLDOMNode createTextNode(string text)
        {
            // Create a text node wrapped in a span for tracking
            var id = _bridge.ExecuteScript<string>($@"
                (function() {{
                    var span = document.createElement('span');
                    span.appendChild(document.createTextNode({WebView2Bridge.JsonEncode(text)}));
                    var id = 'olw-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
                    span.setAttribute('data-olw-id', id);
                    return id;
                }})()");
            return CreateElement(id);
        }

        public IHTMLElement documentElement => CreateElement(_bridge.DocumentGetDocumentElement());

        public string uniqueID => "document-" + GetHashCode();

        public bool attachEvent(string @event, object pdisp) => false;
        public void detachEvent(string @event, object pdisp) { }

        public object onrowsdelete { get; set; }
        public object onrowsinserted { get; set; }
        public object oncellchange { get; set; }
        public object ondatasetchanged { get; set; }
        public object ondataavailable { get; set; }
        public object ondatasetcomplete { get; set; }
        public object onpropertychange { get; set; }

        public string dir
        {
            get => _bridge.ExecuteScript<string>("document.dir");
            set => _bridge.ExecuteScript($"document.dir = {WebView2Bridge.JsonEncode(value)}");
        }

        public object oncontextmenu { get; set; }
        public object onstop { get; set; }

        public IHTMLDocument2 createDocumentFragment()
        {
            // Return a new empty document - not fully implemented
            return null;
        }

        public IHTMLDocument2 parentDocument => null;

        public bool enableDownload
        {
            get => true;
            set { /* Not implemented */ }
        }

        public string baseUrl
        {
            get => _bridge.ExecuteScript<string>("document.baseURI");
            set
            {
                _bridge.ExecuteScript($@"
                    var base = document.querySelector('base') || document.createElement('base');
                    base.href = {WebView2Bridge.JsonEncode(value)};
                    if (!base.parentNode) document.head.appendChild(base);");
            }
        }

        public object childNodes => all;

        public bool inheritStyleSheets
        {
            get => true;
            set { /* Not implemented */ }
        }

        public object onbeforeeditfocus { get; set; }

        public IHTMLElementCollection getElementsByName(string v)
        {
            var ids = _bridge.ExecuteScript<string[]>($@"
                (function() {{
                    var result = [];
                    var elems = document.getElementsByName({WebView2Bridge.JsonEncode(v)});
                    for (var i = 0; i < elems.length; i++) {{
                        var id = elems[i].getAttribute('data-olw-id');
                        if (!id) {{
                            id = 'olw-' + Date.now() + '-' + i;
                            elems[i].setAttribute('data-olw-id', id);
                        }}
                        result.push(id);
                    }}
                    return result;
                }})()");
            return CreateCollection(ids);
        }

        public IHTMLElement getElementById(string v)
        {
            return CreateElement(_bridge.DocumentGetElementById(v));
        }

        public IHTMLElementCollection getElementsByTagName(string v)
        {
            return CreateCollection(_bridge.DocumentGetElementsByTagName(v));
        }

        // ===== IHTMLDocument4 Implementation =====

        public void focus()
        {
            _bridge.ExecuteScript("document.body?.focus()");
        }

        public bool hasFocus()
        {
            return _bridge.ExecuteScript<bool>("document.hasFocus()");
        }

        public object onselectionchange { get; set; }

        public object namespaces => null; // IE-specific

        public IHTMLDocument2 createDocumentFromUrl(string bstrUrl, string bstrOptions)
        {
            // Not directly supported
            return null;
        }

        public string media
        {
            get => "screen";
            set { /* Not supported */ }
        }

        public IHTMLEventObj createEventObject(object pvarEventObject = null)
        {
            return null; // Would need shim
        }

        public bool fireEvent(string bstrEventName, object pvarEventObject = null)
        {
            _bridge.ExecuteScript($@"
                document.dispatchEvent(new Event({WebView2Bridge.JsonEncode(bstrEventName)}))");
            return true;
        }

        public IHTMLRenderStyle createRenderStyle(string v)
        {
            return null; // IE-specific
        }

        public object onfocusin { get; set; }
        public object onfocusout { get; set; }
        public object onactivate { get; set; }
        public object ondeactivate { get; set; }
        public object onbeforeactivate { get; set; }
        public object onbeforedeactivate { get; set; }

        public string URLUnencoded => _bridge.ExecuteScript<string>("decodeURIComponent(document.URL)");

        // ===== Additional Helper Methods =====

        /// <summary>
        /// Query selector - returns first matching element.
        /// </summary>
        public WebView2Element QuerySelector(string selector)
        {
            return CreateElement(_bridge.DocumentQuerySelector(selector));
        }

        /// <summary>
        /// Query selector all - returns all matching elements.
        /// </summary>
        public WebView2ElementCollection QuerySelectorAll(string selector)
        {
            return CreateCollection(_bridge.DocumentQuerySelectorAll(selector));
        }

        public override string ToString() => $"WebView2Document[{url}]";
    }
}
