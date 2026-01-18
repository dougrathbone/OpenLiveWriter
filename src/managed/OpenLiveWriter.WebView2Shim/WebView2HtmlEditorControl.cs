// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using mshtml;
using OpenLiveWriter.ApplicationFramework;
using OpenLiveWriter.HtmlEditor;
using OpenLiveWriter.Localization;
using OpenLiveWriter.Mshtml;

namespace OpenLiveWriter.WebView2Shim
{
    /// <summary>
    /// WebView2-based HTML editor control that implements IHtmlEditor.
    /// This is designed to be a drop-in replacement for the MSHTML-based editor.
    /// </summary>
    public class WebView2HtmlEditorControl : UserControl, IHtmlEditor
    {
        private WebView2 _webView;
        private WebView2Bridge _bridge;
        private WebView2Document _document;
        private bool _isInitialized;
        private bool _isDirty;
        private string _pendingHtml;
        private WebView2HtmlEditorCommandSource _commandSource;

        public WebView2HtmlEditorControl()
        {
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
                await _webView.EnsureCoreWebView2Async();
                
                // Set background color after initialization
                _webView.DefaultBackgroundColor = System.Drawing.Color.White;
                
                _webView.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    if (e.IsSuccess)
                    {
                        InitializeBridge();
                        if (!string.IsNullOrEmpty(_pendingHtml))
                        {
                            SetEditorContent(_pendingHtml);
                            _pendingHtml = null;
                        }
                    }
                };

                // Load the editor template
                var editorHtml = GetEditorTemplate();
                _webView.CoreWebView2.NavigateToString(editorHtml);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OLW-DEBUG] WebView2HtmlEditorControl init error: {ex.Message}");
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
                _isInitialized = true;
                
                // Set up content change monitoring
                _bridge.ExecuteScript(@"
                    document.getElementById('olw-editor').addEventListener('input', function() {
                        window.chrome.webview.postMessage(JSON.stringify({ type: 'contentChanged' }));
                    });
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
        html, body { margin: 0; padding: 0; height: 100%; }
        #olw-editor {
            min-height: 100%;
            padding: 10px;
            box-sizing: border-box;
            font-family: Segoe UI, Arial, sans-serif;
            font-size: 14px;
            outline: none;
        }
    </style>
</head>
<body>
    <div id='olw-editor' contenteditable='true'></div>
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
            if (_isInitialized && _document != null)
            {
                var editor = _document.getElementById("olw-editor");
                return editor?.innerHTML ?? "";
            }
            return "";
        }

        public WebView2Document Document => _document;
        public bool IsInitialized => _isInitialized;

        #region IHtmlEditor Implementation

        public Control EditorControl => this;

        public void LoadHtmlFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                var html = File.ReadAllText(filePath);
                if (_isInitialized)
                {
                    SetEditorContent(html);
                }
                else
                {
                    _pendingHtml = html;
                }
            }
        }

        public string GetEditedHtml(bool preferWellFormed)
        {
            return GetEditorContent();
        }

        public string GetEditedHtmlFast()
        {
            return GetEditorContent();
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
            if (_isInitialized && _document != null)
            {
                var selection = _document.selection;
                var range = selection.createRange();
                
                if (options.HasFlag(HtmlInsertionOptions.PlainText))
                {
                    range.text = content;
                }
                else
                {
                    range.pasteHTML(content);
                }
                
                if (options.HasFlag(HtmlInsertionOptions.MoveCursorAfter))
                {
                    range.collapse(false);
                    range.select();
                }
                
                range.Dispose();
                IsDirty = true;
            }
        }

        public void InsertLink(string url, string linkText, string linkTitle, string rel, bool newWindow)
        {
            if (_isInitialized && _document != null)
            {
                var target = newWindow ? " target=\"_blank\"" : "";
                var relAttr = !string.IsNullOrEmpty(rel) ? $" rel=\"{WebView2Bridge.HtmlEncode(rel)}\"" : "";
                var titleAttr = !string.IsNullOrEmpty(linkTitle) ? $" title=\"{WebView2Bridge.HtmlEncode(linkTitle)}\"" : "";
                var html = $"<a href=\"{WebView2Bridge.HtmlEncode(url)}\"{titleAttr}{relAttr}{target}>{WebView2Bridge.HtmlEncode(linkText)}</a>";
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

        // ISimpleTextEditorCommandSource
        public bool HasFocus => _editor?.ContainsFocus ?? false;
        public bool CanUndo => _document?.queryCommandEnabled("undo") ?? false;
        public void Undo() => _document?.execCommand("undo");
        public bool CanRedo => _document?.queryCommandEnabled("redo") ?? false;
        public void Redo() => _document?.execCommand("redo");
        public bool CanCut => _document?.queryCommandEnabled("cut") ?? false;
        public void Cut() => _document?.execCommand("cut");
        public bool CanCopy => _document?.queryCommandEnabled("copy") ?? false;
        public void Copy() => _document?.execCommand("copy");
        public bool CanPaste => true; // WebView2 handles paste internally
        public void Paste() => _document?.execCommand("paste");
        public bool CanClear => true;
        public void Clear() => _document?.execCommand("delete");
        public void SelectAll() => _document?.execCommand("selectAll");
        public void InsertEuroSymbol() => _editor?.InsertHtml("€", false);
        public bool ReadOnly => false;
        public event EventHandler CommandStateChanged;
        public event EventHandler AggressiveCommandStateChanged;

        // IHtmlEditorCommandSource
        public void ViewSource() { /* TODO */ }
        public void ClearFormatting() => _document?.execCommand("removeFormat");
        public bool CanApplyFormatting(CommandId? commandId) => _editor?.IsInitialized ?? false;

        public string SelectionFontFamily => null; // TODO
        public void ApplyFontFamily(string fontFamily) => _document?.execCommand("fontName", false, fontFamily);

        public float SelectionFontSize => 0; // TODO
        public void ApplyFontSize(float fontSize) => _document?.execCommand("fontSize", false, ((int)fontSize).ToString());

        public int SelectionForeColor => 0;
        public void ApplyFontForeColor(int color) 
        {
            var c = Color.FromArgb(color);
            _document?.execCommand("foreColor", false, $"#{c.R:X2}{c.G:X2}{c.B:X2}");
        }

        public int SelectionBackColor => 0;
        public void ApplyFontBackColor(int? color)
        {
            if (color.HasValue)
            {
                var c = Color.FromArgb(color.Value);
                _document?.execCommand("hiliteColor", false, $"#{c.R:X2}{c.G:X2}{c.B:X2}");
            }
        }

        public string SelectionStyleName => null;
        public void ApplyHtmlFormattingStyle(IHtmlFormattingStyle style) { /* TODO */ }

        public bool SelectionBold => _document?.queryCommandState("bold") ?? false;
        public void ApplyBold() => _document?.execCommand("bold");

        public bool SelectionItalic => _document?.queryCommandState("italic") ?? false;
        public void ApplyItalic() => _document?.execCommand("italic");

        public bool SelectionUnderlined => _document?.queryCommandState("underline") ?? false;
        public void ApplyUnderline() => _document?.execCommand("underline");

        public bool SelectionStrikethrough => _document?.queryCommandState("strikeThrough") ?? false;
        public void ApplyStrikethrough() => _document?.execCommand("strikeThrough");

        public bool SelectionSuperscript => _document?.queryCommandState("superscript") ?? false;
        public void ApplySuperscript() => _document?.execCommand("superscript");

        public bool SelectionSubscript => _document?.queryCommandState("subscript") ?? false;
        public void ApplySubscript() => _document?.execCommand("subscript");

        public bool SelectionIsLTR => true;
        public void InsertLTRTextBlock() { /* TODO */ }
        public bool SelectionIsRTL => false;
        public void InsertRTLTextBlock() { /* TODO */ }

        public EditorTextAlignment GetSelectionAlignment() => EditorTextAlignment.None; // TODO
        public void ApplyAlignment(EditorTextAlignment alignment)
        {
            switch (alignment)
            {
                case EditorTextAlignment.Left: _document?.execCommand("justifyLeft"); break;
                case EditorTextAlignment.Center: _document?.execCommand("justifyCenter"); break;
                case EditorTextAlignment.Right: _document?.execCommand("justifyRight"); break;
                case EditorTextAlignment.Justify: _document?.execCommand("justifyFull"); break;
            }
        }

        public bool SelectionBulleted => _document?.queryCommandState("insertUnorderedList") ?? false;
        public void ApplyBullets() => _document?.execCommand("insertUnorderedList");

        public bool SelectionNumbered => _document?.queryCommandState("insertOrderedList") ?? false;
        public void ApplyNumbers() => _document?.execCommand("insertOrderedList");

        public bool CanIndent => true;
        public void ApplyIndent() => _document?.execCommand("indent");

        public bool CanOutdent => true;
        public void ApplyOutdent() => _document?.execCommand("outdent");

        public void ApplyBlockquote() { /* TODO: wrap selection in blockquote */ }
        public bool SelectionBlockquoted => false; // TODO

        public bool CanInsertLink => true;
        public void InsertLink() { /* Handled by caller */ }

        public bool CanRemoveLink => false; // TODO
        public void RemoveLink() => _document?.execCommand("unlink");

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
