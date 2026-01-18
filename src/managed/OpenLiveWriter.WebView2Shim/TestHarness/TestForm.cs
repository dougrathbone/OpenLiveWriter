// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace OpenLiveWriter.WebView2Shim.TestHarness
{
    /// <summary>
    /// Simple test form to validate the WebView2 DOM shim functionality.
    /// </summary>
    public class TestForm : Form
    {
        private WebView2 _webView;
        private WebView2Bridge _bridge;
        private WebView2Document _document;
        private TextBox _output;
        private Panel _buttonPanel;

        public TestForm()
        {
            Text = "WebView2 DOM Shim Test Harness";
            Size = new Size(900, 700);
            StartPosition = FormStartPosition.CenterScreen;

            InitializeControls();
            InitializeWebView();
        }

        private void InitializeControls()
        {
            // Button panel at top
            _buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5)
            };

            AddButton("Init Bridge", (s, e) => InitBridge());
            AddButton("Get Body", (s, e) => TestGetBody());
            AddButton("Get Editor", (s, e) => TestGetEditor());
            AddButton("Bold", (s, e) => TestExecCommand("bold"));
            AddButton("Italic", (s, e) => TestExecCommand("italic"));
            AddButton("Get Selection", (s, e) => TestGetSelection());
            AddButton("Get innerHTML", (s, e) => TestGetInnerHTML());
            AddButton("Set innerHTML", (s, e) => TestSetInnerHTML());

            Controls.Add(_buttonPanel);

            // Output textbox at bottom
            _output = new TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                ReadOnly = true
            };
            Controls.Add(_output);

            // WebView2 in center
            _webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(_webView);
        }

        private void AddButton(string text, EventHandler handler)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(3)
            };
            btn.Click += handler;
            _buttonPanel.Controls.Add(btn);
            
            // Flow layout
            if (_buttonPanel.Controls.Count > 1)
            {
                var prev = _buttonPanel.Controls[_buttonPanel.Controls.Count - 2];
                btn.Left = prev.Right + 5;
            }
            btn.Top = 5;
        }

        private async void InitializeWebView()
        {
            try
            {
                Log("Initializing WebView2...");
                await _webView.EnsureCoreWebView2Async();
                Log("WebView2 initialized.");

                // Load the test editor HTML
                var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                    @"..\..\..\..\OpenLiveWriter.WebView2Shim\TestHarness\editor.html");
                
                if (File.Exists(htmlPath))
                {
                    _webView.CoreWebView2.Navigate(new Uri(Path.GetFullPath(htmlPath)).ToString());
                    Log($"Loading: {htmlPath}");
                }
                else
                {
                    // Try alternate path
                    htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "editor.html");
                    if (File.Exists(htmlPath))
                    {
                        _webView.CoreWebView2.Navigate(new Uri(Path.GetFullPath(htmlPath)).ToString());
                        Log($"Loading: {htmlPath}");
                    }
                    else
                    {
                        // Load inline HTML
                        var html = GetInlineEditorHtml();
                        _webView.CoreWebView2.NavigateToString(html);
                        Log("Loading inline HTML (editor.html not found)");
                    }
                }

                _webView.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    Log($"Navigation completed. Success: {e.IsSuccess}");
                    if (e.IsSuccess)
                    {
                        Log("Click 'Init Bridge' to initialize the DOM shim.");
                    }
                };
            }
            catch (Exception ex)
            {
                Log($"Error initializing WebView2: {ex.Message}");
            }
        }

        private void InitBridge()
        {
            try
            {
                _bridge = new WebView2Bridge(_webView);
                _bridge.Initialize();
                _document = new WebView2Document(_bridge, _webView);
                Log("Bridge initialized successfully!");
                Log($"Document ready state: {_document.readyState}");
                Log($"Document URL: {_document.url}");
            }
            catch (Exception ex)
            {
                Log($"Error initializing bridge: {ex.Message}");
            }
        }

        private void TestGetBody()
        {
            if (!CheckBridge()) return;
            try
            {
                var body = _document.body;
                if (body != null)
                {
                    Log($"Body element ID: {body.ElementId}");
                    Log($"Body tagName: {body.tagName}");
                    Log($"Body children count: {body.children.length}");
                }
                else
                {
                    Log("Body is null!");
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }

        private void TestGetEditor()
        {
            if (!CheckBridge()) return;
            try
            {
                var editor = _document.getElementById("editor");
                if (editor != null)
                {
                    Log($"Editor element ID: {editor.ElementId}");
                    Log($"Editor HTML id: {editor.id}");
                    Log($"Editor tagName: {editor.tagName}");
                    Log($"Editor isContentEditable: {editor.isContentEditable}");
                    Log($"Editor innerHTML length: {editor.innerHTML?.Length ?? 0}");
                }
                else
                {
                    Log("Editor element not found!");
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }

        private void TestExecCommand(string cmd)
        {
            if (!CheckBridge()) return;
            try
            {
                var result = _document.execCommand(cmd);
                var state = _document.queryCommandState(cmd);
                Log($"execCommand('{cmd}'): {result}, state: {state}");
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }

        private void TestGetSelection()
        {
            if (!CheckBridge()) return;
            try
            {
                var selection = _document.selection;
                Log($"Selection type: {selection.type}");
                
                var range = selection.createRange();
                Log($"Range text: '{range.text}'");
                Log($"Range HTML: '{range.htmlText}'");
                range.Dispose();
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }

        private void TestGetInnerHTML()
        {
            if (!CheckBridge()) return;
            try
            {
                var editor = _document.getElementById("editor");
                if (editor != null)
                {
                    var html = editor.innerHTML;
                    Log($"Editor innerHTML ({html?.Length ?? 0} chars):");
                    Log(html?.Substring(0, Math.Min(200, html?.Length ?? 0)) + "...");
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }

        private void TestSetInnerHTML()
        {
            if (!CheckBridge()) return;
            try
            {
                var editor = _document.getElementById("editor");
                if (editor != null)
                {
                    editor.innerHTML = "<p><strong>Content set via C# shim!</strong></p><p>This proves the bridge works.</p>";
                    Log("Set innerHTML via C# shim - check the editor!");
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }

        private bool CheckBridge()
        {
            if (_bridge == null || _document == null)
            {
                Log("Bridge not initialized! Click 'Init Bridge' first.");
                return false;
            }
            return true;
        }

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Log(message)));
                return;
            }
            _output.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            _output.SelectionStart = _output.TextLength;
            _output.ScrollToCaret();
        }

        private string GetInlineEditorHtml()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Segoe UI, Arial; margin: 10px; }
        #editor { border: 1px solid #ccc; min-height: 200px; padding: 10px; }
    </style>
</head>
<body>
    <div id=""editor"" contenteditable=""true"">
        <p>Hello <strong>World</strong>! This is a test editor.</p>
        <p>Type here to test the WebView2 DOM shim.</p>
    </div>
</body>
</html>";
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TestForm());
        }
    }
}
