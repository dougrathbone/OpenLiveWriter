// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Microsoft.Web.WebView2.WinForms;
using mshtml;

namespace OpenLiveWriter.WebView2Shim
{
    /// <summary>
    /// WebView2 document wrapper implementing IHTMLDocument2.
    /// Provides DOM document operations via JavaScript bridge.
    /// </summary>
    public class WebView2Document : IHTMLDocument2
    {
        private readonly WebView2Bridge _bridge;
        private readonly WebView2 _webView;

        public WebView2Document(WebView2Bridge bridge, WebView2 webView)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        }

        public WebView2Bridge Bridge => _bridge;
        public WebView2 WebView => _webView;

        private WebView2Element CreateElement(string elementId)
        {
            if (string.IsNullOrEmpty(elementId)) return null;
            var el = new WebView2Element(_bridge, elementId);
            el.ParentDocument = this;
            return el;
        }

        private WebView2ElementCollection CreateCollection(string[] ids)
        {
            return new WebView2ElementCollection(_bridge, ids ?? Array.Empty<string>(), this);
        }

        // ===== IHTMLDocument Implementation =====

        object IHTMLDocument.Script => null; // Not implemented - script object
        object IHTMLDocument2.Script => null; // Not implemented - script object (shadow)

        // ===== IHTMLDocument2 Properties =====

        public string title
        {
            get => _bridge.DocumentGetTitle();
            set => _bridge.ExecuteScript("document.title = " + WebView2Bridge.JsonEncode(value ?? ""));
        }

        public string readyState => _bridge.DocumentGetReadyState();
        
        public string url
        {
            get => _bridge.DocumentGetURL();
            set { } // URL is read-only in modern browsers
        }
        
        public string domain
        {
            get => _bridge.ExecuteScript<string>("document.domain || ''");
            set => _bridge.ExecuteScript("document.domain = " + WebView2Bridge.JsonEncode(value ?? ""));
        }
        
        public string cookie
        {
            get => _bridge.ExecuteScript<string>("document.cookie || ''");
            set => _bridge.ExecuteScript("document.cookie = " + WebView2Bridge.JsonEncode(value ?? ""));
        }
        
        public string referrer => _bridge.ExecuteScript<string>("document.referrer || ''");

        /// <summary>
        /// Global setting for paragraph separator. When true, uses &lt;p&gt; tags (default).
        /// When false, uses &lt;div&gt; tags (modern browser default).
        /// </summary>
        public static bool UseParagraphTags { get; set; } = true;
        
        public string designMode
        {
            get => _bridge.ExecuteScript<string>("document.designMode || 'off'");
            set
            {
                System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] WebView2Document.designMode setter called with: {value}");
                System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] UseParagraphTags = {UseParagraphTags}");
                _bridge.ExecuteScript("document.designMode = " + WebView2Bridge.JsonEncode(value ?? "off"));
                // Set paragraph separator based on user preference
                if (value?.ToLower() == "on")
                {
                    string separator = UseParagraphTags ? "p" : "div";
                    System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] Setting defaultParagraphSeparator to: {separator}");
                    _bridge.ExecuteScript($"document.execCommand('defaultParagraphSeparator', false, '{separator}')");
                }
            }
        }

        public bool expando { get; set; } = true;
        
        public string charset
        {
            get => _bridge.ExecuteScript<string>("document.charset || document.characterSet || ''");
            set { } // Read-only in modern browsers
        }
        
        public string defaultCharset { get; set; } = "UTF-8";
        
        public string mimeType => _bridge.ExecuteScript<string>("document.contentType || 'text/html'");
        public string fileSize => "0";
        public string fileCreatedDate => "";
        public string fileModifiedDate => "";
        public string fileUpdatedDate => "";
        public string security => "";
        public string protocol => _bridge.ExecuteScript<string>("window.location.protocol || 'https:'");
        public string nameProp => "";
        public string lastModified => _bridge.ExecuteScript<string>("document.lastModified || ''");

        // Color properties (legacy)
        public object alinkColor { get; set; }
        public object bgColor { get; set; }
        public object fgColor { get; set; }
        public object linkColor { get; set; }
        public object vlinkColor { get; set; }

        // Event handlers (stubs)
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

        // ===== Element Access =====

        IHTMLElementCollection IHTMLDocument2.all => CreateCollection(_bridge.ExecuteScript<string[]>(@"
            (function() {
                var result = [];
                var all = document.getElementsByTagName('*');
                for (var i = 0; i < all.length; i++) {
                    var id = all[i].getAttribute('data-olw-id');
                    if (!id) { id = 'olw-' + Date.now() + '-' + i; all[i].setAttribute('data-olw-id', id); }
                    result.push(id);
                }
                return result;
            })()") ?? Array.Empty<string>());

        IHTMLElement IHTMLDocument2.body => CreateElement(_bridge.DocumentGetBody());
        IHTMLElement IHTMLDocument2.activeElement => CreateElement(_bridge.DocumentGetActiveElement());

        IHTMLElementCollection IHTMLDocument2.images => CreateCollection(_bridge.DocumentGetElementsByTagName("img"));
        IHTMLElementCollection IHTMLDocument2.applets => CreateCollection(Array.Empty<string>()); // Deprecated
        IHTMLElementCollection IHTMLDocument2.links => CreateCollection(_bridge.DocumentGetElementsByTagName("a"));
        IHTMLElementCollection IHTMLDocument2.forms => CreateCollection(_bridge.DocumentGetElementsByTagName("form"));
        IHTMLElementCollection IHTMLDocument2.anchors => CreateCollection(_bridge.ExecuteScript<string[]>(@"
            (function() {
                var result = [];
                var elems = document.getElementsByTagName('a');
                for (var i = 0; i < elems.length; i++) {
                    if (elems[i].name) {
                        var id = elems[i].getAttribute('data-olw-id');
                        if (!id) { id = 'olw-' + Date.now() + '-' + i; elems[i].setAttribute('data-olw-id', id); }
                        result.push(id);
                    }
                }
                return result;
            })()") ?? Array.Empty<string>());
        IHTMLElementCollection IHTMLDocument2.scripts => CreateCollection(_bridge.DocumentGetElementsByTagName("script"));
        IHTMLElementCollection IHTMLDocument2.embeds => CreateCollection(_bridge.DocumentGetElementsByTagName("embed"));
        IHTMLElementCollection IHTMLDocument2.plugins => CreateCollection(_bridge.DocumentGetElementsByTagName("embed"));

        IHTMLSelectionObject IHTMLDocument2.selection => new WebView2Selection(_bridge, this);
        
        FramesCollection IHTMLDocument2.frames => null; // Not implemented
        HTMLLocation IHTMLDocument2.location => null; // Not implemented
        IHTMLWindow2 IHTMLDocument2.parentWindow => null; // Not implemented
        HTMLStyleSheetsCollection IHTMLDocument2.styleSheets => null; // Not implemented

        // ===== Convenience Properties (typed as shim classes) =====

        public WebView2Element documentElement => CreateElement(_bridge.DocumentGetDocumentElement());
        public WebView2Element body => CreateElement(_bridge.DocumentGetBody());
        public WebView2Element activeElement => CreateElement(_bridge.DocumentGetActiveElement());
        
        public WebView2Element head
        {
            get
            {
                var id = _bridge.ExecuteScript<string>(@"
                    (function() {
                        var el = document.head;
                        if (!el) return null;
                        var id = el.getAttribute('data-olw-id');
                        if (!id) { id = 'olw-head'; el.setAttribute('data-olw-id', id); }
                        return id;
                    })()");
                return CreateElement(id);
            }
        }

        public WebView2Element getElementById(string elementId) => CreateElement(_bridge.DocumentGetElementById(elementId));
        public WebView2ElementCollection getElementsByTagName(string tagName) => CreateCollection(_bridge.DocumentGetElementsByTagName(tagName));
        
        public WebView2ElementCollection getElementsByName(string name)
        {
            var ids = _bridge.ExecuteScript<string[]>(@"
                (function() {
                    var elems = document.getElementsByName(" + WebView2Bridge.JsonEncode(name) + @");
                    var result = [];
                    for (var i = 0; i < elems.length; i++) {
                        var id = elems[i].getAttribute('data-olw-id');
                        if (!id) { id = 'olw-' + Date.now() + '-' + i; elems[i].setAttribute('data-olw-id', id); }
                        result.push(id);
                    }
                    return result;
                })()") ?? Array.Empty<string>();
            return CreateCollection(ids);
        }

        public WebView2Element querySelector(string selector) => CreateElement(_bridge.DocumentQuerySelector(selector));
        public WebView2ElementCollection querySelectorAll(string selector) => CreateCollection(_bridge.DocumentQuerySelectorAll(selector));

        public WebView2ElementCollection all => CreateCollection(_bridge.ExecuteScript<string[]>(@"
            (function() {
                var result = [];
                var all = document.getElementsByTagName('*');
                for (var i = 0; i < all.length; i++) {
                    var id = all[i].getAttribute('data-olw-id');
                    if (!id) { id = 'olw-' + Date.now() + '-' + i; all[i].setAttribute('data-olw-id', id); }
                    result.push(id);
                }
                return result;
            })()") ?? Array.Empty<string>());

        public WebView2ElementCollection images => getElementsByTagName("img");
        public WebView2ElementCollection links => getElementsByTagName("a");
        public WebView2ElementCollection forms => getElementsByTagName("form");
        public WebView2ElementCollection scripts => getElementsByTagName("script");

        // ===== Selection =====

        public WebView2Selection selection => new WebView2Selection(_bridge, this);

        public WebView2TextRange createTextRange()
        {
            var rangeId = _bridge.TextRangeCreate();
            return new WebView2TextRange(_bridge, rangeId, this);
        }

        // ===== IHTMLDocument2 Methods =====

        IHTMLElement IHTMLDocument2.createElement(string eTag) => CreateElement(_bridge.DocumentCreateElement(eTag));
        
        public WebView2Element createElement(string tagName) => CreateElement(_bridge.DocumentCreateElement(tagName));
        
        IHTMLElement IHTMLDocument2.elementFromPoint(int x, int y) => CreateElement(_bridge.DocumentElementFromPoint(x, y));
        
        public WebView2Element elementFromPoint(int x, int y) => CreateElement(_bridge.DocumentElementFromPoint(x, y));

        public bool execCommand(string cmdID, bool showUI = false, object value = null)
        {
            return _bridge.DocumentExecCommand(cmdID, showUI, value?.ToString());
        }

        public bool queryCommandSupported(string cmdID) => _bridge.ExecuteScript<bool>("document.queryCommandSupported(" + WebView2Bridge.JsonEncode(cmdID) + ")");
        public bool queryCommandEnabled(string cmdID) => _bridge.DocumentQueryCommandEnabled(cmdID);
        public bool queryCommandState(string cmdID) => _bridge.DocumentQueryCommandState(cmdID);
        public bool queryCommandIndeterm(string cmdID) => false;
        public string queryCommandText(string cmdID) => cmdID;
        public object queryCommandValue(string cmdID) => _bridge.DocumentQueryCommandValue(cmdID);
        public bool execCommandShowHelp(string cmdID) => false;

        public void write(params object[] psarray)
        {
            if (psarray != null && psarray.Length > 0)
            {
                var html = string.Join("", psarray);
                _bridge.ExecuteScript("document.write(" + WebView2Bridge.JsonEncode(html) + ")");
            }
        }

        public void writeln(params object[] psarray)
        {
            if (psarray != null && psarray.Length > 0)
            {
                var html = string.Join("", psarray) + "\n";
                _bridge.ExecuteScript("document.writeln(" + WebView2Bridge.JsonEncode(html) + ")");
            }
        }

        public object open(string url = "text/html", object name = null, object features = null, object replace = null)
        {
            _bridge.ExecuteScript("document.open()");
            return this;
        }

        public void close() => _bridge.ExecuteScript("document.close()");
        public void clear() => _bridge.ExecuteScript("document.body.innerHTML = ''");

        string IHTMLDocument2.toString() => ToString();

        public IHTMLStyleSheet createStyleSheet(string bstrHref = "", int lIndex = -1)
        {
            // Not fully implemented - returns null
            return null;
        }

        public override string ToString() => "WebView2Document[" + (url ?? "about:blank") + "]";
    }
}
