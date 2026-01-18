// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Drawing;
using System.Windows.Forms;
using OpenLiveWriter.Api;
using OpenLiveWriter.ApplicationFramework;
using OpenLiveWriter.BlogClient.Detection;
using OpenLiveWriter.HtmlEditor;
using OpenLiveWriter.WebView2Shim;

namespace OpenLiveWriter.PostEditor.PostHtmlEditing
{
    /// <summary>
    /// WebView2-based blog post HTML editor that implements IBlogPostHtmlEditor.
    /// This wraps WebView2HtmlEditorControl to provide blog-specific functionality.
    /// </summary>
    internal class WebView2BlogPostHtmlEditorControl : IBlogPostHtmlEditor
    {
        private readonly WebView2HtmlEditorControl _editor;
        private readonly Panel _container;
        private string _baseUrl;
        private string _title;
        private bool _fullyEditableRegionActive;

#pragma warning disable CS0067 // Events not used yet - will be wired up when WebView2 editor is fully implemented
        public event EventHandler TitleChanged;
        public event EventHandler EditableRegionFocusChanged;
        public event EventHandler IsDirtyEvent;
#pragma warning restore CS0067

        public WebView2BlogPostHtmlEditorControl()
        {
            _container = new Panel { Dock = DockStyle.Fill };
            _editor = new WebView2HtmlEditorControl { Dock = DockStyle.Fill };
            _container.Controls.Add(_editor);
            
            System.Diagnostics.Debug.WriteLine("[OLW-DEBUG] WebView2BlogPostHtmlEditorControl created");
        }

        #region IBlogPostHtmlEditor Implementation

        public void Focus()
        {
            _editor.EditorControl.Focus();
        }

        public void FocusTitle()
        {
            // WebView2 doesn't have separate title region yet - just focus body
            Focus();
        }

        public void FocusBody()
        {
            Focus();
        }

        public bool DocumentHasFocus()
        {
            return _editor.EditorControl.ContainsFocus;
        }

        public IFocusableControl FocusControl => new WebView2FocusableControl(_editor.EditorControl);

        public void LoadHtmlFragment(string title, string postBodyHtml, string baseUrl, BlogEditingTemplate editingTemplate)
        {
            _title = title ?? "";
            _baseUrl = baseUrl ?? "";
            
            // For now, just load the body HTML into the editor
            var html = $@"<!DOCTYPE html>
<html>
<head>
    <base href=""{_baseUrl}"" />
    <style>
        html, body {{ 
            background-color: #ffffff; 
            color: #000000;
            margin: 0;
            padding: 0;
        }}
        body {{ 
            font-family: Segoe UI, Arial, sans-serif; 
            font-size: 14px; 
            padding: 10px; 
        }}
        #olw-title {{ 
            font-size: 24px; 
            font-weight: bold; 
            margin-bottom: 10px; 
            border-bottom: 1px solid #ccc; 
            padding-bottom: 10px; 
            background-color: #ffffff;
        }}
        #olw-body {{ 
            min-height: 300px; 
            background-color: #ffffff;
        }}
    </style>
</head>
<body>
    <div id=""olw-title"" contenteditable=""true"">{System.Web.HttpUtility.HtmlEncode(_title)}</div>
    <div id=""olw-body"" contenteditable=""true"">{postBodyHtml}</div>
</body>
</html>";
            
            // Use temporary file approach for now
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"olw_edit_{Guid.NewGuid():N}.html");
            System.IO.File.WriteAllText(tempPath, html);
            _editor.LoadHtmlFile(tempPath);
        }

        public string GetEditedTitleHtml()
        {
            // TODO: Extract title from #olw-title element
            return _title;
        }

        public bool FullyEditableRegionActive
        {
            get => _fullyEditableRegionActive;
            set => _fullyEditableRegionActive = value;
        }

        public void UpdateEditingContext()
        {
            // No-op for now
        }

        public void InsertExtendedEntryBreak()
        {
            InsertHtml("<!--more-->", true);
        }

        public void InsertHorizontalLine(bool plainText)
        {
            InsertHtml("<hr />", true);
        }

        public void InsertClearBreak()
        {
            InsertHtml("<br clear=\"all\" />", true);
        }

        public void ChangeSelection(SelectionPosition position)
        {
            // TODO: Implement selection positioning
        }

        public SmartContentEditor CurrentEditor => null;

        #endregion

        #region IHtmlEditor Implementation

        public Control EditorControl => _container;

        public void LoadHtmlFile(string filePath)
        {
            _editor.LoadHtmlFile(filePath);
        }

        public string GetEditedHtml(bool preferWellFormed)
        {
            return _editor.GetEditedHtml(preferWellFormed);
        }

        public string GetEditedHtmlFast()
        {
            return _editor.GetEditedHtmlFast();
        }

        public string SelectedText => _editor.SelectedText;

        public string SelectedHtml => _editor.SelectedHtml;

        public void EmptySelection()
        {
            _editor.EmptySelection();
        }

        public void InsertHtml(string content, bool moveSelectionRight)
        {
            _editor.InsertHtml(content, moveSelectionRight);
        }

        public void InsertHtml(string content, HtmlInsertionOptions options)
        {
            _editor.InsertHtml(content, options);
        }

        public void InsertLink(string url, string linkText, string linkTitle, string rel, bool newWindow)
        {
            _editor.InsertLink(url, linkText, linkTitle, rel, newWindow);
        }

        public bool IsDirty
        {
            get => _editor.IsDirty;
            set => _editor.IsDirty = value;
        }

        public IHtmlEditorCommandSource CommandSource => _editor.CommandSource;

        public bool SuspendAutoSave => false;

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _editor?.Dispose();
            _container?.Dispose();
        }

        #endregion

        /// <summary>
        /// Simple focusable control wrapper
        /// </summary>
        private class WebView2FocusableControl : IFocusableControl
        {
            private readonly Control _control;

            public WebView2FocusableControl(Control control)
            {
                _control = control;
            }

            public bool Focus()
            {
                return _control.Focus();
            }

            public bool ContainsFocus => _control.ContainsFocus;
            public bool Visible => _control.Visible;
        }
    }
}
