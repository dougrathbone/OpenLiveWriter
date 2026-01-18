// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using mshtml;
using OpenLiveWriter.ApplicationFramework;
using OpenLiveWriter.CoreServices;
using OpenLiveWriter.HtmlEditor;
using OpenLiveWriter.HtmlEditor.Linking;
using OpenLiveWriter.Localization;
using OpenLiveWriter.Mshtml;

namespace OpenLiveWriter.WebView2Shim
{
    /// <summary>
    /// COM-visible class exposed to JavaScript for bidirectional communication.
    /// JS updates these properties on input, C# reads them synchronously.
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class EditorContentBridge
    {
        private string _title = "";
        private string _body = "";
        
        public string Title 
        { 
            get => _title;
            set
            {
                _title = value;
                System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] Bridge.Title SET: {value?.Length ?? 0} chars");
            }
        }
        
        public string Body 
        { 
            get => _body;
            set
            {
                _body = value;
                System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] Bridge.Body SET: {value?.Length ?? 0} chars");
            }
        }
        
        public bool IsDirty { get; set; } = false;
        
        public void MarkDirty()
        {
            IsDirty = true;
            System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] Bridge.MarkDirty() called");
        }
    }
    
    /// <summary>
    /// WebView2-based HTML editor control that implements IHtmlEditor.
    /// This is designed to be a drop-in replacement for the MSHTML-based editor.
    /// </summary>
    public class WebView2HtmlEditorControl : UserControl, IHtmlEditor
    {
        private static int _instanceCounter;
        private readonly int _instanceId;
        private WebView2 _webView;
        private WebView2Bridge _bridge;
        private WebView2Document _document;
        private bool _isInitialized;
        private bool _isDirty;
        private string _pendingHtml;
        private string _pendingFilePath;
        private WebView2HtmlEditorCommandSource _commandSource;
        private EditorContentBridge _contentBridge;

        public WebView2HtmlEditorControl()
        {
            _instanceId = ++_instanceCounter;
            System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] WebView2HtmlEditorControl#{_instanceId} created");
            _contentBridge = new EditorContentBridge();
            InitializeComponent();
            // Create command source immediately so it's never null
            _commandSource = new WebView2HtmlEditorCommandSource(this, null);
            InitializeWebView();
        }

        private void InitializeComponent()
        {
            BackColor = System.Drawing.Color.White;
            
            _webView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = System.Drawing.Color.White
            };
            Controls.Add(_webView);
        }

        private async void InitializeWebView()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] WebView2HtmlEditorControl#{_instanceId}.InitializeWebView starting");
                await _webView.EnsureCoreWebView2Async();
                System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] WebView2HtmlEditorControl#{_instanceId}.EnsureCoreWebView2Async completed");
                
                // Set background color after initialization
                _webView.DefaultBackgroundColor = System.Drawing.Color.White;
                
                // Expose the content bridge to JavaScript - this allows synchronous read/write
                _webView.CoreWebView2.AddHostObjectToScript("olw", _contentBridge);
                System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] #{_instanceId} Host object 'olw' added to script");
                
                // Mark as initialized once CoreWebView2 is ready - we can now navigate
                _isInitialized = true;
                
                _webView.CoreWebView2.NavigationStarting += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] #{_instanceId} NavigationStarting - URL: {e.Uri}");
                };
                
                _webView.CoreWebView2.NavigationCompleted += async (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] #{_instanceId} NavigationCompleted - IsSuccess: {e.IsSuccess}, URL: {_webView.CoreWebView2.Source}");
                    if (e.IsSuccess)
                    {
                        // Inject host object sync listeners after navigation completes
                        await SetupHostObjectListeners();
                        
                        // Notify command source that WebView2 is ready
                        _commandSource.SetWebView(_webView);
                    }
                };

                // Check if we have pending html to load
                if (!string.IsNullOrEmpty(_pendingHtml))
                {
                    System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] #{_instanceId} Loading pending html, length: {_pendingHtml.Length}");
                    var html = _pendingHtml;
                    _pendingHtml = null;
                    _webView.CoreWebView2.NavigateToString(html);
                }
                else if (!string.IsNullOrEmpty(_pendingFilePath) && File.Exists(_pendingFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] #{_instanceId} Loading pending file: {_pendingFilePath}");
                    var html = File.ReadAllText(_pendingFilePath);
                    _pendingFilePath = null;
                    _webView.CoreWebView2.NavigateToString(html);
                }
                else
                {
                    // Load the editor template
                    var editorHtml = GetEditorTemplate();
                    _webView.CoreWebView2.NavigateToString(editorHtml);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] WebView2HtmlEditorControl init error: {ex.Message}");
            }
        }
        
        private async Task SetupHostObjectListeners()
        {
            try
            {
                // Inject JavaScript that sets up input listeners to sync content to the host object
                var script = @"
                    (function() {
                        if (window.olwListenersSetup) return 'already setup';
                        
                        var titleEl = document.getElementById('olw-title');
                        var bodyEl = document.getElementById('olw-body');
                        
                        if (!titleEl || !bodyEl) return 'elements not found';
                        if (!window.chrome || !window.chrome.webview || !window.chrome.webview.hostObjects) return 'hostObjects not available';
                        
                        window.olwListenersSetup = true;
                        var olw = window.chrome.webview.hostObjects.sync.olw;
                        
                        function syncContent() {
                            olw.Title = titleEl.innerHTML;
                            olw.Body = bodyEl.innerHTML;
                            olw.MarkDirty();
                        }
                        
                        titleEl.addEventListener('input', syncContent);
                        bodyEl.addEventListener('input', syncContent);
                        
                        // Sync initial content
                        syncContent();
                        
                        return 'listeners setup ok';
                    })();
                ";
                
                var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] #{_instanceId} SetupHostObjectListeners result: {result}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] #{_instanceId} SetupHostObjectListeners error: {ex.Message}");
            }
        }

        private void InitializeBridge()
        {
            try
            {
                _bridge = new WebView2Bridge(_webView);
                _bridge.Initialize();
                _document = new WebView2Document(_bridge, _webView);
                _commandSource.SetDocument(_document);
                
                // Set up content change monitoring for olw-body element
                _bridge.ExecuteScript(@"
                    var body = document.getElementById('olw-body');
                    if (body) {
                        body.addEventListener('input', function() {
                            window.chrome.webview.postMessage(JSON.stringify({ type: 'contentChanged' }));
                        });
                    }
                ");
                
                _webView.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    try
                    {
                        // Simple check without dynamic - just look for the contentChanged message
                        if (e.WebMessageAsJson?.Contains("contentChanged") == true)
                        {
                            IsDirty = true;
                        }
                    }
                    catch { }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] Bridge init error: {ex.Message}");
            }
        }

        private string GetEditorTemplate()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        html, body { 
            margin: 0; 
            padding: 0; 
            height: 100%;
            background-color: #ffffff;
        }
        body {
            font-family: Segoe UI, Arial, sans-serif;
            font-size: 14px;
            padding: 10px;
        }
        #olw-title {
            font-size: 24px;
            font-weight: bold;
            margin-bottom: 10px;
            border-bottom: 1px solid #ccc;
            padding-bottom: 10px;
            outline: none;
        }
        #olw-body {
            min-height: 300px;
            outline: none;
        }
        [contenteditable]:focus {
            outline: none;
        }
    </style>
</head>
<body>
    <div id='olw-title' contenteditable='true'></div>
    <div id='olw-body' contenteditable='true'></div>
</body>
</html>";
        }

        private void SetEditorContent(string html)
        {
            if (_isInitialized && _document != null)
            {
                var editor = _document.getElementById("olw-editor");
                if (editor != null)
                {
                    editor.innerHTML = html ?? "";
                }
            }
        }
        
        private string GetEditorContent()
        {
            return _contentBridge.Body ?? "";
        }
        
        public string GetEditedTitleHtml()
        {
            System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] #{_instanceId} GetEditedTitleHtml - bridge title length: {_contentBridge.Title?.Length ?? 0}, value: '{_contentBridge.Title}'");
            return _contentBridge.Title ?? "";
        }

        public WebView2Document Document => _document;
        public bool IsInitialized => _isInitialized;

        #region IHtmlEditor Implementation

        public Control EditorControl => this;

        public void LoadHtmlFile(string filePath)
        {
            System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] #{_instanceId} LoadHtmlFile called - path: {filePath}, exists: {File.Exists(filePath)}, isInitialized: {_isInitialized}");
            if (File.Exists(filePath))
            {
                // Read the HTML content directly
                var htmlContent = File.ReadAllText(filePath);
                _pendingHtml = htmlContent;
                
                if (_isInitialized && _webView.CoreWebView2 != null)
                {
                    // Use JavaScript to update the content directly
                    System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] #{_instanceId} LoadHtmlFile - updating content via JS, html length: {htmlContent.Length}");
                    UpdateContentViaJavaScript(htmlContent);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] #{_instanceId} LoadHtmlFile - NOT READY, storing pending html");
                }
            }
        }
        
        private async void UpdateContentViaJavaScript(string html)
        {
            try
            {
                // Extract title and body from the HTML
                var titleMatch = System.Text.RegularExpressions.Regex.Match(html, @"<div id=""olw-title""[^>]*>(.*?)</div>", System.Text.RegularExpressions.RegexOptions.Singleline);
                var bodyMatch = System.Text.RegularExpressions.Regex.Match(html, @"<div id=""olw-body""[^>]*>(.*?)</div>", System.Text.RegularExpressions.RegexOptions.Singleline);
                
                var title = titleMatch.Success ? titleMatch.Groups[1].Value : "";
                var body = bodyMatch.Success ? bodyMatch.Groups[1].Value : "";
                
                // Update the content bridge so C# has initial values
                _contentBridge.Title = title;
                _contentBridge.Body = body;
                
                System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] #{_instanceId} UpdateContentViaJavaScript - title length: {title.Length}, body length: {body.Length}");
                
                // Escape for JavaScript string
                var escapedTitle = title.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");
                var escapedBody = body.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");
                
                // Update content AND setup host object sync
                var script = $@"
                    document.getElementById('olw-title').innerHTML = '{escapedTitle}';
                    document.getElementById('olw-body').innerHTML = '{escapedBody}';
                    
                    // Setup change listeners using host object (sync to C#)
                    if (!window.olwListenersSetup) {{
                        window.olwListenersSetup = true;
                        
                        // Get the host object - this is synchronous COM bridge to C#
                        const olw = window.chrome.webview.hostObjects.sync.olw;
                        
                        function syncContentToHost() {{
                            olw.Title = document.getElementById('olw-title').innerHTML;
                            olw.Body = document.getElementById('olw-body').innerHTML;
                            olw.MarkDirty();
                        }}
                        
                        document.getElementById('olw-title').addEventListener('input', syncContentToHost);
                        document.getElementById('olw-body').addEventListener('input', syncContentToHost);
                        
                        console.log('OLW: Host object sync setup');
                    }}
                    'done';
                ";
                
                var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] #{_instanceId} UpdateContentViaJavaScript - ExecuteScriptAsync returned: {result}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] #{_instanceId} UpdateContentViaJavaScript error: {ex.Message}");
            }
        }

        public string GetEditedHtml(bool preferWellFormed)
        {
            // Read directly from the content bridge - JS syncs on every input event
            System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] #{_instanceId} GetEditedHtml - bridge body length: {_contentBridge.Body?.Length ?? 0}");
            return _contentBridge.Body ?? "";
        }

        public string GetEditedHtmlFast()
        {
            // Same as GetEditedHtml - bridge is always current
            return _contentBridge.Body ?? "";
        }

        public string SelectedText
        {
            get
            {
                if (_isInitialized && _document != null)
                {
                    var selection = _document.selection;
                    if (selection.type != "None")
                    {
                        var range = selection.createRange();
                        var text = range.text;
                        range.Dispose();
                        return text;
                    }
                }
                return "";
            }
        }

        public string SelectedHtml
        {
            get
            {
                if (_isInitialized && _document != null)
                {
                    var selection = _document.selection;
                    if (selection.type != "None")
                    {
                        var range = selection.createRange();
                        var html = range.htmlText;
                        range.Dispose();
                        return html;
                    }
                }
                return "";
            }
        }

        public void EmptySelection()
        {
            if (_isInitialized && _document != null)
            {
                _document.selection.empty();
            }
        }

        public void InsertHtml(string content, bool moveSelectionRight)
        {
            InsertHtml(content, moveSelectionRight ? HtmlInsertionOptions.MoveCursorAfter : HtmlInsertionOptions.Default);
        }

        public void InsertHtml(string content, HtmlInsertionOptions options)
        {
            if (_isInitialized && _webView?.CoreWebView2 != null)
            {
                // Use execCommand insertHTML to insert at cursor position
                var escapedContent = content.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "");
                var script = $"document.execCommand('insertHTML', false, '{escapedContent}')";
                System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] InsertHtml: {script}");
                _ = _webView.CoreWebView2.ExecuteScriptAsync(script);
                IsDirty = true;
            }
        }

        public void InsertLink(string url, string linkText, string linkTitle, string rel, bool newWindow)
        {
            if (_isInitialized && _webView?.CoreWebView2 != null)
            {
                var target = newWindow ? " target=\"_blank\"" : "";
                var relAttr = !string.IsNullOrEmpty(rel) ? $" rel=\"{System.Net.WebUtility.HtmlEncode(rel)}\"" : "";
                var titleAttr = !string.IsNullOrEmpty(linkTitle) ? $" title=\"{System.Net.WebUtility.HtmlEncode(linkTitle)}\"" : "";
                var html = $"<a href=\"{System.Net.WebUtility.HtmlEncode(url)}\"{titleAttr}{relAttr}{target}>{System.Net.WebUtility.HtmlEncode(linkText)}</a>";
                System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] InsertLink: {html}");
                InsertHtml(html, HtmlInsertionOptions.MoveCursorAfter);
            }
        }

        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    IsDirtyEvent?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public IHtmlEditorCommandSource CommandSource => _commandSource;

        public event EventHandler IsDirtyEvent;

        public bool SuspendAutoSave => false;

        public new void Dispose()
        {
            _bridge?.Dispose();
            _webView?.Dispose();
            base.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// Command source for WebView2 editor, handling formatting commands.
    /// </summary>
    internal class WebView2HtmlEditorCommandSource : IHtmlEditorCommandSource
    {
        private readonly WebView2HtmlEditorControl _editor;
        private WebView2Document _document;
        private WebView2 _webView;

        public WebView2HtmlEditorCommandSource(WebView2HtmlEditorControl editor, WebView2Document document)
        {
            _editor = editor;
            _document = document;
        }

        /// <summary>
        /// Updates the document reference after WebView2 initialization.
        /// </summary>
        public void SetDocument(WebView2Document document)
        {
            _document = document;
        }
        
        /// <summary>
        /// Sets the WebView2 reference for direct JavaScript execution.
        /// </summary>
        public void SetWebView(WebView2 webView)
        {
            _webView = webView;
            System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] CommandSource.SetWebView called, webView null: {webView == null}");
        }
        
        private void ExecuteCommand(string command, string value = null)
        {
            if (_webView?.CoreWebView2 == null) return;
            
            var script = value != null 
                ? $"document.execCommand('{command}', false, '{value}')"
                : $"document.execCommand('{command}')";
            
            System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] ExecuteCommand: {script}");
            _ = _webView.CoreWebView2.ExecuteScriptAsync(script);
        }
        
        private bool QueryCommandState(string command)
        {
            // For now, return false - async query is complex
            return false;
        }
        
        private bool QueryCommandEnabled(string command)
        {
            return _webView?.CoreWebView2 != null;
        }

        // ISimpleTextEditorCommandSource
        public bool HasFocus => _editor?.ContainsFocus ?? false;
        public bool CanUndo => QueryCommandEnabled("undo");
        public void Undo() => ExecuteCommand("undo");
        public bool CanRedo => QueryCommandEnabled("redo");
        public void Redo() => ExecuteCommand("redo");
        public bool CanCut => QueryCommandEnabled("cut");
        public void Cut() => ExecuteCommand("cut");
        public bool CanCopy => QueryCommandEnabled("copy");
        public void Copy() => ExecuteCommand("copy");
        public bool CanPaste => true; // WebView2 handles paste internally
        public void Paste() => ExecuteCommand("paste");
        public bool CanClear => true;
        public void Clear() => ExecuteCommand("delete");
        public void SelectAll() => ExecuteCommand("selectAll");
        public void InsertEuroSymbol() => _editor?.InsertHtml("€", false);
        public bool ReadOnly => false;
        public event EventHandler CommandStateChanged;
        public event EventHandler AggressiveCommandStateChanged;

        // IHtmlEditorCommandSource
        public void ViewSource() { /* TODO */ }
        public void ClearFormatting() => ExecuteCommand("removeFormat");
        public bool CanApplyFormatting(CommandId? commandId) => _webView?.CoreWebView2 != null;

        public string SelectionFontFamily => null; // TODO
        public void ApplyFontFamily(string fontFamily) => ExecuteCommand("fontName", fontFamily);

        public float SelectionFontSize => 0; // TODO
        public void ApplyFontSize(float fontSize) => ExecuteCommand("fontSize", ((int)fontSize).ToString());

        public int SelectionForeColor => 0;
        public void ApplyFontForeColor(int color) 
        {
            var c = Color.FromArgb(color);
            ExecuteCommand("foreColor", $"#{c.R:X2}{c.G:X2}{c.B:X2}");
        }

        public int SelectionBackColor => 0;
        public void ApplyFontBackColor(int? color)
        {
            if (color.HasValue)
            {
                var c = Color.FromArgb(color.Value);
                ExecuteCommand("hiliteColor", $"#{c.R:X2}{c.G:X2}{c.B:X2}");
            }
        }

        public string SelectionStyleName => null;
        public void ApplyHtmlFormattingStyle(IHtmlFormattingStyle style) { /* TODO */ }

        public bool SelectionBold => QueryCommandState("bold");
        public void ApplyBold()
        {
            System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] ApplyBold called, _webView null: {_webView == null}");
            ExecuteCommand("bold");
        }

        public bool SelectionItalic => QueryCommandState("italic");
        public void ApplyItalic()
        {
            System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] ApplyItalic called, _webView null: {_webView == null}");
            ExecuteCommand("italic");
        }

        public bool SelectionUnderlined => QueryCommandState("underline");
        public void ApplyUnderline()
        {
            System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] ApplyUnderline called, _webView null: {_webView == null}");
            ExecuteCommand("underline");
        }

        public bool SelectionStrikethrough => QueryCommandState("strikeThrough");
        public void ApplyStrikethrough() => ExecuteCommand("strikeThrough");

        public bool SelectionSuperscript => QueryCommandState("superscript");
        public void ApplySuperscript() => ExecuteCommand("superscript");

        public bool SelectionSubscript => QueryCommandState("subscript");
        public void ApplySubscript() => ExecuteCommand("subscript");

        public bool SelectionIsLTR => true;
        public void InsertLTRTextBlock() { /* TODO */ }
        public bool SelectionIsRTL => false;
        public void InsertRTLTextBlock() { /* TODO */ }

        public EditorTextAlignment GetSelectionAlignment() => EditorTextAlignment.None; // TODO
        public void ApplyAlignment(EditorTextAlignment alignment)
        {
            switch (alignment)
            {
                case EditorTextAlignment.Left: ExecuteCommand("justifyLeft"); break;
                case EditorTextAlignment.Center: ExecuteCommand("justifyCenter"); break;
                case EditorTextAlignment.Right: ExecuteCommand("justifyRight"); break;
                case EditorTextAlignment.Justify: ExecuteCommand("justifyFull"); break;
            }
        }

        public bool SelectionBulleted => QueryCommandState("insertUnorderedList");
        public void ApplyBullets() => ExecuteCommand("insertUnorderedList");

        public bool SelectionNumbered => QueryCommandState("insertOrderedList");
        public void ApplyNumbers() => ExecuteCommand("insertOrderedList");

        public bool CanIndent => true;
        public void ApplyIndent() => ExecuteCommand("indent");

        public bool CanOutdent => true;
        public void ApplyOutdent() => ExecuteCommand("outdent");

        public void ApplyBlockquote() { /* TODO: wrap selection in blockquote */ }
        public bool SelectionBlockquoted => false; // TODO

        public bool CanInsertLink => _webView?.CoreWebView2 != null;
        public void InsertLink()
        {
            using (new WaitCursor())
            {
                // Get selected text for the link text
                string selectedText = GetSelectedText();
                
                // Create a temporary CommandManager for the dialog
                using (var commandManager = new CommandManager())
                using (var hyperlinkForm = new HyperlinkForm(commandManager, true))
                {
                    if (!string.IsNullOrEmpty(selectedText))
                        hyperlinkForm.LinkText = selectedText;
                    
                    hyperlinkForm.EditStyle = false;
                    
                    var owner = _editor?.FindForm();
                    if (hyperlinkForm.ShowDialog(owner) == DialogResult.OK)
                    {
                        _editor?.InsertLink(
                            hyperlinkForm.Hyperlink, 
                            hyperlinkForm.LinkText, 
                            hyperlinkForm.LinkTitle, 
                            hyperlinkForm.Rel, 
                            hyperlinkForm.NewWindow);
                    }
                }
            }
        }
        
        private string GetSelectedText()
        {
            if (_webView?.CoreWebView2 == null) return "";
            
            try
            {
                var task = _webView.CoreWebView2.ExecuteScriptAsync("window.getSelection().toString()");
                task.Wait(1000);
                if (task.IsCompleted)
                {
                    var result = task.Result;
                    // Result is JSON-encoded string
                    if (result != null && result.StartsWith("\"") && result.EndsWith("\""))
                        return result.Substring(1, result.Length - 2).Replace("\\n", "\n").Replace("\\\"", "\"");
                }
            }
            catch { }
            return "";
        }

        public bool CanRemoveLink => false; // TODO
        public void RemoveLink() => ExecuteCommand("unlink");

        public void OpenLink() { /* TODO */ }
        public void AddToGlossary() { /* TODO */ }

        public bool CanPasteSpecial => false;
        public bool AllowPasteSpecial => false;
        public void PasteSpecial() { /* TODO */ }

        public bool CanFind => false;
        public void Find() { /* TODO */ }

        public bool CanPrint => false;
        public void Print() { /* TODO */ }
        public void PrintPreview() { /* TODO */ }

        public LinkInfo DiscoverCurrentLink() => null; // TODO

        public bool CheckSpelling() => true; // TODO

        public bool FullyEditableRegionActive => true;

        public CommandManager CommandManager => null; // TODO

        protected void OnCommandStateChanged()
        {
            CommandStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
