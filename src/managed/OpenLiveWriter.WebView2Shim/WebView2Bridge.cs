// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;

namespace OpenLiveWriter.WebView2Shim
{
    /// <summary>
    /// Provides synchronous JavaScript execution for the WebView2 DOM shim.
    /// This bridges the async WebView2 API to the sync MSHTML-style API.
    /// </summary>
    public class WebView2Bridge : IDisposable
    {
        private readonly WebView2 _webView;
        private bool _isInitialized;
        private string _bridgeScript;

        public WebView2Bridge(WebView2 webView)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
            LoadBridgeScript();
        }

        private void LoadBridgeScript()
        {
            // Load the bridge script from embedded resources
            var assembly = typeof(WebView2Bridge).Assembly;
            var resourceName = "OpenLiveWriter.WebView2Shim.Resources.olw-dom-bridge.js";
            
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (var reader = new System.IO.StreamReader(stream))
                    {
                        _bridgeScript = reader.ReadToEnd();
                    }
                }
            }
            
            // Fallback: try loading from file system during development
            if (string.IsNullOrEmpty(_bridgeScript))
            {
                var devPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    @"..\..\..\..\OpenLiveWriter.WebView2Shim\Resources\olw-dom-bridge.js");
                if (System.IO.File.Exists(devPath))
                {
                    _bridgeScript = System.IO.File.ReadAllText(devPath);
                }
            }
        }

        /// <summary>
        /// Initialize the bridge by injecting the JavaScript into the page.
        /// Must be called after WebView2 navigation completes.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            
            if (string.IsNullOrEmpty(_bridgeScript))
            {
                throw new InvalidOperationException("Bridge script not loaded. Check embedded resources.");
            }

            ExecuteScript(_bridgeScript);
            _isInitialized = true;
        }

        /// <summary>
        /// Execute JavaScript synchronously and return the result.
        /// Uses a nested message loop to avoid deadlocking the UI thread.
        /// </summary>
        public string ExecuteScript(string script)
        {
            string result = null;
            Exception error = null;
            bool completed = false;

            // Use async/await with a continuation that doesn't deadlock
            var task = _webView.CoreWebView2.ExecuteScriptAsync(script);
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    error = t.Exception?.InnerException ?? t.Exception;
                else
                    result = t.Result;
                completed = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            // Pump messages while waiting (prevents UI deadlock)
            while (!completed)
            {
                System.Windows.Forms.Application.DoEvents();
                System.Threading.Thread.Sleep(1);
            }

            if (error != null)
                throw error;

            return result;
        }

        /// <summary>
        /// Execute JavaScript and parse the result as the specified type.
        /// </summary>
        public T ExecuteScript<T>(string script)
        {
            var json = ExecuteScript(script);
            return ParseJsonResult<T>(json);
        }

        /// <summary>
        /// Parse a JSON result from ExecuteScriptAsync.
        /// WebView2 returns results as JSON strings.
        /// </summary>
        public static T ParseJsonResult<T>(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "null" || json == "undefined")
            {
                return default(T);
            }

            // For string results, WebView2 returns them double-encoded
            if (typeof(T) == typeof(string))
            {
                if (json.StartsWith("\"") && json.EndsWith("\""))
                {
                    // Unescape the JSON string
                    return (T)(object)JsonConvert.DeserializeObject<string>(json);
                }
                return (T)(object)json;
            }

            // For other types, deserialize normally
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Encode a value for use in JavaScript.
        /// </summary>
        public static string JsonEncode(object value)
        {
            if (value == null) return "null";
            return JsonConvert.SerializeObject(value);
        }

        /// <summary>
        /// HTML-encode a string for safe inclusion in HTML attributes/content.
        /// </summary>
        public static string HtmlEncode(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return System.Web.HttpUtility.HtmlEncode(value);
        }

        // ===== Element Operations =====
        
        public string ElementGetInnerHTML(string elementId) =>
            ExecuteScript<string>($"OLW.element.getInnerHTML({JsonEncode(elementId)})");

        public void ElementSetInnerHTML(string elementId, string value) =>
            ExecuteScript($"OLW.element.setInnerHTML({JsonEncode(elementId)}, {JsonEncode(value)})");

        public string ElementGetInnerText(string elementId) =>
            ExecuteScript<string>($"OLW.element.getInnerText({JsonEncode(elementId)})");

        public void ElementSetInnerText(string elementId, string value) =>
            ExecuteScript($"OLW.element.setInnerText({JsonEncode(elementId)}, {JsonEncode(value)})");

        public string ElementGetOuterHTML(string elementId) =>
            ExecuteScript<string>($"OLW.element.getOuterHTML({JsonEncode(elementId)})");

        public void ElementSetOuterHTML(string elementId, string value) =>
            ExecuteScript($"OLW.element.setOuterHTML({JsonEncode(elementId)}, {JsonEncode(value)})");

        public string ElementGetClassName(string elementId) =>
            ExecuteScript<string>($"OLW.element.getClassName({JsonEncode(elementId)})");

        public void ElementSetClassName(string elementId, string value) =>
            ExecuteScript($"OLW.element.setClassName({JsonEncode(elementId)}, {JsonEncode(value)})");

        public string ElementGetId(string elementId) =>
            ExecuteScript<string>($"OLW.element.getId({JsonEncode(elementId)})");

        public string ElementGetTagName(string elementId) =>
            ExecuteScript<string>($"OLW.element.getTagName({JsonEncode(elementId)})");

        public string ElementGetTitle(string elementId) =>
            ExecuteScript<string>($"OLW.element.getTitle({JsonEncode(elementId)})");

        public void ElementSetTitle(string elementId, string value) =>
            ExecuteScript($"OLW.element.setTitle({JsonEncode(elementId)}, {JsonEncode(value)})");

        public int ElementGetOffsetLeft(string elementId) =>
            ExecuteScript<int>($"OLW.element.getOffsetLeft({JsonEncode(elementId)})");

        public int ElementGetOffsetTop(string elementId) =>
            ExecuteScript<int>($"OLW.element.getOffsetTop({JsonEncode(elementId)})");

        public int ElementGetOffsetWidth(string elementId) =>
            ExecuteScript<int>($"OLW.element.getOffsetWidth({JsonEncode(elementId)})");

        public int ElementGetOffsetHeight(string elementId) =>
            ExecuteScript<int>($"OLW.element.getOffsetHeight({JsonEncode(elementId)})");

        public string ElementGetOffsetParent(string elementId) =>
            ExecuteScript<string>($"OLW.element.getOffsetParent({JsonEncode(elementId)})");

        public string ElementGetParentElement(string elementId) =>
            ExecuteScript<string>($"OLW.element.getParentElement({JsonEncode(elementId)})");

        public string[] ElementGetChildren(string elementId) =>
            ExecuteScript<string[]>($"OLW.element.getChildren({JsonEncode(elementId)})") ?? Array.Empty<string>();

        public string ElementGetAttribute(string elementId, string name) =>
            ExecuteScript<string>($"OLW.element.getAttribute({JsonEncode(elementId)}, {JsonEncode(name)})");

        public void ElementSetAttribute(string elementId, string name, string value) =>
            ExecuteScript($"OLW.element.setAttribute({JsonEncode(elementId)}, {JsonEncode(name)}, {JsonEncode(value)})");

        public void ElementRemoveAttribute(string elementId, string name) =>
            ExecuteScript($"OLW.element.removeAttribute({JsonEncode(elementId)}, {JsonEncode(name)})");

        public void ElementClick(string elementId) =>
            ExecuteScript($"OLW.element.click({JsonEncode(elementId)})");

        public void ElementScrollIntoView(string elementId, bool alignTop = true) =>
            ExecuteScript($"OLW.element.scrollIntoView({JsonEncode(elementId)}, {(alignTop ? "true" : "false")})");

        public bool ElementContains(string elementId, string childId) =>
            ExecuteScript<bool>($"OLW.element.contains({JsonEncode(elementId)}, {JsonEncode(childId)})");

        public void ElementInsertAdjacentHTML(string elementId, string position, string html) =>
            ExecuteScript($"OLW.element.insertAdjacentHTML({JsonEncode(elementId)}, {JsonEncode(position)}, {JsonEncode(html)})");

        public string ElementGetStyleProperty(string elementId, string property) =>
            ExecuteScript<string>($"OLW.element.getStyleProperty({JsonEncode(elementId)}, {JsonEncode(property)})");

        public void ElementSetStyleProperty(string elementId, string property, string value) =>
            ExecuteScript($"OLW.element.setStyleProperty({JsonEncode(elementId)}, {JsonEncode(property)}, {JsonEncode(value)})");

        public string ElementGetComputedStyle(string elementId, string property) =>
            ExecuteScript<string>($"OLW.element.getComputedStyle({JsonEncode(elementId)}, {JsonEncode(property)})");

        public bool ElementIsTextEdit(string elementId) =>
            ExecuteScript<bool>($"OLW.element.isTextEdit({JsonEncode(elementId)})");

        // ===== Document Operations =====

        public string DocumentGetBody() =>
            ExecuteScript<string>("OLW.document.getBody()");

        public string DocumentGetDocumentElement() =>
            ExecuteScript<string>("OLW.document.getDocumentElement()");

        public string DocumentGetActiveElement() =>
            ExecuteScript<string>("OLW.document.getActiveElement()");

        public string DocumentGetElementById(string id) =>
            ExecuteScript<string>($"OLW.document.getElementById({JsonEncode(id)})");

        public string[] DocumentGetElementsByTagName(string tagName) =>
            ExecuteScript<string[]>($"OLW.document.getElementsByTagName({JsonEncode(tagName)})") ?? Array.Empty<string>();

        public string DocumentQuerySelector(string selector) =>
            ExecuteScript<string>($"OLW.document.querySelector({JsonEncode(selector)})");

        public string[] DocumentQuerySelectorAll(string selector) =>
            ExecuteScript<string[]>($"OLW.document.querySelectorAll({JsonEncode(selector)})") ?? Array.Empty<string>();

        public string DocumentCreateElement(string tagName) =>
            ExecuteScript<string>($"OLW.document.createElement({JsonEncode(tagName)})");

        public string DocumentGetReadyState() =>
            ExecuteScript<string>("OLW.document.getReadyState()");

        public string DocumentGetTitle() =>
            ExecuteScript<string>("OLW.document.getTitle()");

        public void DocumentSetTitle(string value) =>
            ExecuteScript($"OLW.document.setTitle({JsonEncode(value)})");

        public string DocumentGetURL() =>
            ExecuteScript<string>("OLW.document.getURL()");

        public string DocumentElementFromPoint(int x, int y) =>
            ExecuteScript<string>($"OLW.document.elementFromPoint({x}, {y})");

        public bool DocumentExecCommand(string command, bool showUI = false, string value = null) =>
            ExecuteScript<bool>($"OLW.document.execCommand({JsonEncode(command)}, {(showUI ? "true" : "false")}, {JsonEncode(value)})");

        public bool DocumentQueryCommandState(string command) =>
            ExecuteScript<bool>($"OLW.document.queryCommandState({JsonEncode(command)})");

        public string DocumentQueryCommandValue(string command) =>
            ExecuteScript<string>($"OLW.document.queryCommandValue({JsonEncode(command)})");

        public bool DocumentQueryCommandEnabled(string command) =>
            ExecuteScript<bool>($"OLW.document.queryCommandEnabled({JsonEncode(command)})");

        // ===== Selection Operations =====

        public string SelectionGetType() =>
            ExecuteScript<string>("OLW.selection.getType()");

        public string SelectionGetText() =>
            ExecuteScript<string>("OLW.selection.getText()");

        public string SelectionGetHtml() =>
            ExecuteScript<string>("OLW.selection.getHtml()");

        public string SelectionGetAnchorElement() =>
            ExecuteScript<string>("OLW.selection.getAnchorElement()");

        public string SelectionGetFocusElement() =>
            ExecuteScript<string>("OLW.selection.getFocusElement()");

        public void SelectionCollapse(bool toStart) =>
            ExecuteScript($"OLW.selection.collapse({(toStart ? "true" : "false")})");

        public void SelectionSelectAll() =>
            ExecuteScript("OLW.selection.selectAll()");

        public void SelectionClear() =>
            ExecuteScript("OLW.selection.clear()");

        public void SelectionSelectElement(string elementId) =>
            ExecuteScript($"OLW.selection.selectElement({JsonEncode(elementId)})");

        // ===== TextRange Operations =====

        public string TextRangeCreate() =>
            ExecuteScript<string>("OLW.textRange.create()");

        public string TextRangeCreateFromSelection() =>
            ExecuteScript<string>("OLW.textRange.createFromSelection()");

        public void TextRangeDispose(string rangeId) =>
            ExecuteScript($"OLW.textRange.dispose({JsonEncode(rangeId)})");

        public string TextRangeGetText(string rangeId) =>
            ExecuteScript<string>($"OLW.textRange.getText({JsonEncode(rangeId)})");

        public void TextRangeSetText(string rangeId, string text) =>
            ExecuteScript($"OLW.textRange.setText({JsonEncode(rangeId)}, {JsonEncode(text)})");

        public string TextRangeGetHtml(string rangeId) =>
            ExecuteScript<string>($"OLW.textRange.getHtml({JsonEncode(rangeId)})");

        public void TextRangeSetHtml(string rangeId, string html) =>
            ExecuteScript($"OLW.textRange.setHtml({JsonEncode(rangeId)}, {JsonEncode(html)})");

        public void TextRangeSelect(string rangeId) =>
            ExecuteScript($"OLW.textRange.select({JsonEncode(rangeId)})");

        public void TextRangeCollapse(string rangeId, bool toStart) =>
            ExecuteScript($"OLW.textRange.collapse({JsonEncode(rangeId)}, {(toStart ? "true" : "false")})");

        public void TextRangeMoveToElement(string rangeId, string elementId) =>
            ExecuteScript($"OLW.textRange.moveToElement({JsonEncode(rangeId)}, {JsonEncode(elementId)})");

        public string TextRangeGetParentElement(string rangeId) =>
            ExecuteScript<string>($"OLW.textRange.getParentElement({JsonEncode(rangeId)})");

        public bool TextRangeExecCommand(string rangeId, string command, string value = null) =>
            ExecuteScript<bool>($"OLW.textRange.execCommand({JsonEncode(rangeId)}, {JsonEncode(command)}, {JsonEncode(value)})");

        // ===== Utility =====

        public int GarbageCollect() =>
            ExecuteScript<int>("OLW.util.gc()");

        public void Dispose()
        {
            // No explicit cleanup needed - WebView2 owns the browser instance
        }
    }
}
