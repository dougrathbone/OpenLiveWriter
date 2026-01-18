// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Microsoft.Web.WebView2.WinForms;

namespace OpenLiveWriter.WebView2Shim
{
    /// <summary>
    /// WebView2 document wrapper - standalone class for now.
    /// Provides DOM document operations via JavaScript bridge.
    /// </summary>
    public class WebView2Document
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

        // ===== Document Properties =====

        public string title
        {
            get => _bridge.DocumentGetTitle();
            set => _bridge.ExecuteScript("document.title = " + WebView2Bridge.JsonEncode(value ?? ""));
        }

        public string readyState => _bridge.DocumentGetReadyState();
        public string url => _bridge.DocumentGetURL();
        public string domain => _bridge.ExecuteScript<string>("document.domain || ''");
        public string cookie
        {
            get => _bridge.ExecuteScript<string>("document.cookie || ''");
            set => _bridge.ExecuteScript("document.cookie = " + WebView2Bridge.JsonEncode(value ?? ""));
        }
        public string referrer => _bridge.ExecuteScript<string>("document.referrer || ''");
        public string designMode
        {
            get => _bridge.ExecuteScript<string>("document.designMode || 'off'");
            set => _bridge.ExecuteScript("document.designMode = " + WebView2Bridge.JsonEncode(value ?? "off"));
        }

        // ===== Element Access =====

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
        public WebView2Element createElement(string tagName) => CreateElement(_bridge.DocumentCreateElement(tagName));
        public WebView2Element elementFromPoint(int x, int y) => CreateElement(_bridge.DocumentElementFromPoint(x, y));

        // ===== Selection =====

        public WebView2Selection selection => new WebView2Selection(_bridge, this);

        public WebView2TextRange createTextRange()
        {
            var rangeId = _bridge.TextRangeCreate();
            return new WebView2TextRange(_bridge, rangeId, this);
        }

        // ===== Commands =====

        public bool execCommand(string cmdID, bool showUI = false, object value = null)
        {
            return _bridge.DocumentExecCommand(cmdID, showUI, value?.ToString());
        }

        public bool queryCommandState(string cmdID) => _bridge.DocumentQueryCommandState(cmdID);
        public bool queryCommandEnabled(string cmdID) => _bridge.DocumentQueryCommandEnabled(cmdID);
        public string queryCommandValue(string cmdID) => _bridge.DocumentQueryCommandValue(cmdID);
        public bool queryCommandSupported(string cmdID) => _bridge.ExecuteScript<bool>("document.queryCommandSupported(" + WebView2Bridge.JsonEncode(cmdID) + ")");

        // ===== Document Methods =====

        public void write(string html) => _bridge.ExecuteScript("document.write(" + WebView2Bridge.JsonEncode(html ?? "") + ")");
        public void writeln(string html) => _bridge.ExecuteScript("document.writeln(" + WebView2Bridge.JsonEncode(html ?? "") + ")");
        public void open() => _bridge.ExecuteScript("document.open()");
        public void close() => _bridge.ExecuteScript("document.close()");
        public void clear() => _bridge.ExecuteScript("document.body.innerHTML = ''");

        // ===== Collections =====

        public WebView2ElementCollection all
        {
            get
            {
                var ids = _bridge.ExecuteScript<string[]>(@"
                    (function() {
                        var result = [];
                        var all = document.getElementsByTagName('*');
                        for (var i = 0; i < all.length; i++) {
                            var id = all[i].getAttribute('data-olw-id');
                            if (!id) { id = 'olw-' + Date.now() + '-' + i; all[i].setAttribute('data-olw-id', id); }
                            result.push(id);
                        }
                        return result;
                    })()") ?? Array.Empty<string>();
                return CreateCollection(ids);
            }
        }

        public WebView2ElementCollection images => getElementsByTagName("img");
        public WebView2ElementCollection links => getElementsByTagName("a");
        public WebView2ElementCollection forms => getElementsByTagName("form");
        public WebView2ElementCollection scripts => getElementsByTagName("script");

        public override string ToString() => "WebView2Document[" + (url ?? "about:blank") + "]";
    }
}
