# WebView2 HTML Editor Migration Plan

## Executive Summary

This document details the strategy for replacing the MSHTML-based HTML editor in Open Live Writer with WebView2. This is the **critical blocker** for .NET 10 migration, as MSHTML's COM interop (specifically `EnumeratorToEnumVariantMarshaler`) was removed in .NET Core.

**Estimated Effort:** 4-8 weeks for a senior developer  
**Risk Level:** High - this is the core functionality of the application  
**Recommendation:** Use an existing JavaScript editor library (TinyMCE) rather than building from scratch

---

## Current Architecture

### Class Hierarchy

```
MshtmlEditor (UserControl)
    ↓ used by
HtmlEditorControl (abstract)
    ↓ inherited by  
BlogPostHtmlEditorControl
    ↓ used by
BlogPostHtmlEditor
    ↓ contained in
ContentEditor / PostEditorForm
```

### Key Files

| File | Lines | Purpose |
|------|-------|---------|
| `OpenLiveWriter.Mshtml/MshtmlEditor.cs` | ~1,477 | Core MSHTML wrapper, implements `IDocHostUIHandler2`, `IHTMLEditDesignerRaw` |
| `OpenLiveWriter.HtmlEditor/HtmlEditorControl.cs` | ~4,500+ | Abstract editor with commands, selection, paste handling |
| `OpenLiveWriter.PostEditor/PostHtmlEditing/BlogPostHtmlEditorControl.cs` | ~2,700 | Blog-specific editor with spell check, plugins |

### MSHTML Interfaces Used

| Interface | Purpose | WebView2 Equivalent |
|-----------|---------|---------------------|
| `IHTMLDocument2/3/4` | DOM access | `ExecuteScriptAsync("document...")` |
| `IHTMLElement/2/3` | Element manipulation | JavaScript DOM APIs |
| `IHTMLTxtRange` | Text selection/ranges | `window.getSelection()` |
| `IMarkupServices` | Low-level markup editing | DOM Range API |
| `IHTMLEditDesignerRaw` | Event interception | `WebMessageReceived` + JS event listeners |
| `IDocHostUIHandler2` | Context menus, UI behavior | WebView2 events |
| `IHTMLChangeSink` | Dirty state tracking | MutationObserver or editor events |

### Key Editor Operations

1. **Loading/Saving HTML** - Load from file/string, get edited HTML
2. **Selection Management** - Get/set selection, find elements
3. **Formatting Commands** - Bold, italic, lists, alignment (via `execCommand`)
4. **Paste Handling** - Clean HTML, handle images, smart paste
5. **Drag & Drop** - Images, files, HTML content
6. **Undo/Redo** - Built into MSHTML, need equivalent
7. **Spell Checking** - Custom integration with squiggly underlines
8. **Element Behaviors** - Resize handles, smart content, tables
9. **Context Menus** - Custom right-click handling

---

## Migration Strategies

### Option A: Custom contenteditable (NOT RECOMMENDED)

Build our own editor using `<div contenteditable="true">` and custom JavaScript.

**Pros:**
- Full control
- No external dependencies
- Can match existing behavior exactly

**Cons:**
- **Months of work** - contenteditable is notoriously buggy
- Need to handle all edge cases (paste, undo, tables, etc.)
- Browser quirks and cross-platform issues
- Spell checking integration is complex
- We'd be reinventing what TinyMCE/CKEditor spent years building

**Verdict:** ❌ Too risky and time-consuming

---

### Option B: TinyMCE Integration (RECOMMENDED)

Embed TinyMCE (MIT licensed) inside WebView2 and bridge to C#.

**Pros:**
- Battle-tested editor with 15+ years of development
- Handles all contenteditable quirks
- Built-in paste cleaning, undo/redo, tables, formatting
- Plugin architecture for extensibility
- MIT license allows free commercial use
- Active development and community
- Works with local files (no CDN required)

**Cons:**
- Learning curve for customization
- Some features may differ from original behavior
- Bundle size (~1-2MB for full editor)

**Verdict:** ✅ Best balance of effort vs. reliability

---

### Option C: CKEditor 5 (ALTERNATIVE)

Similar to TinyMCE but with different architecture.

**Pros:**
- Modern architecture
- Strong collaboration features
- Good documentation

**Cons:**
- GPL license for open source (compatible with OLW's MIT)
- Steeper learning curve
- Heavier than TinyMCE

**Verdict:** ⚠️ Good alternative if TinyMCE doesn't work out

---

### Option D: Quill (NOT RECOMMENDED)

Lightweight modern editor.

**Pros:**
- Small and fast
- Clean API

**Cons:**
- Last major release was 2019
- Missing features (tables, advanced formatting)
- Less mature than TinyMCE/CKEditor

**Verdict:** ❌ Not feature-complete enough

---

## Recommended Architecture (Option B)

### Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    C# WinForms Application                   │
├─────────────────────────────────────────────────────────────┤
│  WebView2HtmlEditorControl : IHtmlEditor                    │
│  ├── WebView2 Control                                       │
│  ├── EditorBridge (C# ↔ JS communication)                   │
│  └── Command handlers                                       │
├─────────────────────────────────────────────────────────────┤
│                         WebView2                             │
├─────────────────────────────────────────────────────────────┤
│  editor.html                                                 │
│  ├── TinyMCE instance                                       │
│  ├── olw-bridge.js (message passing)                        │
│  └── Custom plugins for OLW features                        │
└─────────────────────────────────────────────────────────────┘
```

### C# Side Components

#### 1. `WebView2HtmlEditorControl.cs`
New implementation of `IHtmlEditor` using WebView2 + TinyMCE.

```csharp
public class WebView2HtmlEditorControl : UserControl, IHtmlEditor, IHtmlEditorCommandSource
{
    private WebView2 _webView;
    private EditorBridge _bridge;
    private bool _isDirty;
    
    public async Task InitializeAsync()
    {
        await _webView.EnsureCoreWebView2Async(null);
        
        // Inject bridge object for JS→C# calls
        _webView.CoreWebView2.AddHostObjectToScript("olwBridge", _bridge);
        
        // Handle JS→C# messages
        _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        
        // Load the editor HTML
        _webView.CoreWebView2.Navigate("file:///editor/editor.html");
    }
    
    // IHtmlEditor implementation
    public async Task<string> GetEditedHtml(bool preferWellFormed)
    {
        string html = await _webView.ExecuteScriptAsync("tinymce.activeEditor.getContent()");
        return JsonUnquote(html);
    }
    
    public async Task InsertHtml(string content, HtmlInsertionOptions options)
    {
        string escapedContent = JsonEscape(content);
        await _webView.ExecuteScriptAsync($"tinymce.activeEditor.insertContent({escapedContent})");
    }
    
    // Commands
    public async Task Bold() => await ExecuteCommand("Bold");
    public async Task Italic() => await ExecuteCommand("Italic");
    
    private async Task ExecuteCommand(string command)
    {
        await _webView.ExecuteScriptAsync($"tinymce.activeEditor.execCommand('{command}')");
    }
}
```

#### 2. `EditorBridge.cs`
COM-visible object exposed to JavaScript for callbacks.

```csharp
[ComVisible(true)]
public class EditorBridge
{
    public event EventHandler ContentChanged;
    public event EventHandler<string> LinkClicked;
    public event EventHandler<PasteEventArgs> PasteRequested;
    
    // Called from JavaScript
    public void NotifyContentChanged()
    {
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public void NotifyLinkClicked(string url)
    {
        LinkClicked?.Invoke(this, url);
    }
    
    public string ProcessPaste(string html)
    {
        // Clean HTML, handle images, etc.
        return CleanHtml(html);
    }
}
```

### JavaScript Side Components

#### 1. `editor.html`
Main editor page loaded in WebView2.

```html
<!DOCTYPE html>
<html>
<head>
    <script src="tinymce/tinymce.min.js"></script>
    <script src="olw-bridge.js"></script>
    <style>
        html, body { margin: 0; padding: 0; height: 100%; }
        #editor { height: 100%; }
    </style>
</head>
<body>
    <div id="editor"></div>
    <script>
        tinymce.init({
            selector: '#editor',
            plugins: 'lists link image table paste',
            toolbar: false, // OLW uses ribbon
            menubar: false,
            statusbar: false,
            paste_preprocess: function(plugin, args) {
                // Call C# for paste processing
                args.content = window.chrome.webview.hostObjects.sync.olwBridge.ProcessPaste(args.content);
            },
            setup: function(editor) {
                editor.on('Change', function() {
                    window.chrome.webview.hostObjects.olwBridge.NotifyContentChanged();
                });
            }
        });
    </script>
</body>
</html>
```

#### 2. `olw-bridge.js`
Helper functions for C#↔JS communication.

```javascript
const OLW = {
    // Called from C# via ExecuteScriptAsync
    getContent: () => tinymce.activeEditor.getContent(),
    setContent: (html) => tinymce.activeEditor.setContent(html),
    insertContent: (html) => tinymce.activeEditor.insertContent(html),
    
    execCommand: (cmd, value) => tinymce.activeEditor.execCommand(cmd, false, value),
    
    getSelection: () => tinymce.activeEditor.selection.getContent(),
    
    // More complex operations
    insertLink: (url, text, title, rel, newWindow) => {
        const attrs = { href: url, title: title };
        if (rel) attrs.rel = rel;
        if (newWindow) attrs.target = '_blank';
        tinymce.activeEditor.execCommand('mceInsertLink', false, attrs);
    },
    
    isDirty: () => tinymce.activeEditor.isDirty(),
    clearDirty: () => tinymce.activeEditor.setDirty(false)
};
```

---

## Migration Phases

### Phase 1: Prototype (1-2 weeks)
**Goal:** Prove the concept works

1. Create standalone `WebView2HtmlEditorControl` prototype
2. Embed TinyMCE and verify basic editing works
3. Implement core `IHtmlEditor` interface methods
4. Test bidirectional C#↔JS communication
5. Verify HTML round-trips correctly (load → edit → save)

**Deliverable:** Working prototype that can edit HTML

### Phase 2: Feature Parity - Core Editing (1-2 weeks)
**Goal:** Match essential editor functionality

1. Implement all `IHtmlEditorCommandSource` commands:
   - Formatting (bold, italic, underline, strikethrough)
   - Alignment (left, center, right, justify)
   - Lists (bullet, numbered)
   - Indentation
   - Undo/Redo
   
2. Implement selection management:
   - Get/set selection
   - Select all
   - Find element at point
   
3. Implement clipboard operations:
   - Copy, Cut, Paste
   - Paste special handling (clean HTML from Word, etc.)
   
4. Verify spell checking works (TinyMCE has browser spell check support)

**Deliverable:** Editor with all basic commands working

### Phase 3: Feature Parity - Advanced (2-3 weeks)
**Goal:** Match OLW-specific features

1. **Smart Content / Element Behaviors**
   - Image resize handles
   - Table editing
   - Plugin content (embeds)
   
2. **Drag and Drop**
   - Images from file system
   - HTML content
   - Files
   
3. **Context Menus**
   - Custom right-click menus
   - Element-specific options
   
4. **Link Handling**
   - Link insertion dialog integration
   - Link glossary
   
5. **Template/Theme Support**
   - Load blog template CSS
   - Match blog appearance

**Deliverable:** Full feature parity with existing editor

### Phase 4: Integration & Testing (1-2 weeks)
**Goal:** Replace MshtmlEditor in the application

1. Create `HtmlEditorFactory` to choose implementation
2. Update `HtmlEditorControl` to work with `WebView2HtmlEditorControl`
3. Add `OLW_USE_WEBVIEW2_EDITOR` environment variable toggle
4. Extensive testing:
   - New post creation
   - Edit existing post
   - Copy/paste from various sources
   - Image handling
   - Table editing
   - Multiple blogs
   
5. Performance testing and optimization

**Deliverable:** Working application with WebView2 editor

### Phase 5: Cleanup & Polish (1 week)
**Goal:** Production ready

1. Remove MSHTML fallback code (optional, or keep for comparison)
2. Fix edge cases and bugs found in testing
3. Update documentation
4. Performance optimization

**Deliverable:** Ready for .NET 10 migration

---

## Technical Challenges & Solutions

### Challenge 1: Async Nature of WebView2
**Problem:** MSHTML was synchronous, WebView2 is async.

**Solution:**
- Use `async/await` throughout
- For properties that must be sync, cache values and update via events
- Consider `TaskCompletionSource` for request-response patterns

### Challenge 2: Element Behaviors (Resize Handles, etc.)
**Problem:** MSHTML had native element behaviors for resize handles.

**Solution:**
- TinyMCE has built-in image resize
- For custom elements, use TinyMCE's `noneditable_noneditable_class` and custom UI
- May need custom JavaScript for OLW-specific smart content

### Challenge 3: Spell Checking Integration
**Problem:** OLW has custom spell check integration.

**Solution:**
- Use browser's native spell check (works in WebView2/Edge)
- Or integrate TinyMCE's spell check plugin
- Custom dictionary support may need additional work

### Challenge 4: Template Loading
**Problem:** Blog templates are loaded as local HTML files.

**Solution:**
- TinyMCE's `content_css` option loads external CSS
- Can inject template HTML as editor wrapper
- May need `file://` protocol handling in WebView2

### Challenge 5: Paste Handling
**Problem:** OLW has sophisticated paste cleaning (Word, web content, etc.)

**Solution:**
- TinyMCE has `paste_preprocess` hook
- Call C# code via bridge for complex cleaning
- Reuse existing paste cleaning logic

---

## Files to Create

```
src/managed/
├── OpenLiveWriter.WebView2Editor/           # New project
│   ├── WebView2HtmlEditorControl.cs         # Main editor control
│   ├── EditorBridge.cs                      # C#↔JS bridge
│   ├── WebView2EditorCommands.cs            # Command implementations
│   ├── WebView2EditorSelection.cs           # Selection management
│   └── Resources/
│       ├── editor.html                      # Editor HTML page
│       ├── olw-bridge.js                    # JS bridge code
│       └── tinymce/                         # TinyMCE files
│           ├── tinymce.min.js
│           └── themes/skins/plugins/
│
├── OpenLiveWriter.HtmlEditor/
│   └── HtmlEditorFactory.cs                 # Factory for editor selection
```

## Files to Modify

1. `OpenLiveWriter.HtmlEditor/HtmlEditorControl.cs`
   - Make more methods virtual for override
   - Add factory pattern integration
   
2. `OpenLiveWriter.PostEditor/PostHtmlEditing/BlogPostHtmlEditorControl.cs`
   - Create WebView2 version or make abstract

3. Various files using `MshtmlEditor` directly
   - Audit and update to use factory/interface

---

## Dependencies to Add

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.Web.WebView2` | Latest | WebView2 runtime |
| TinyMCE | 7.x | Editor (embedded, not NuGet) |

TinyMCE is self-hosted (download and include in Resources), not loaded from CDN.

---

## Testing Strategy

### Unit Tests
- Editor bridge message passing
- HTML content round-trip
- Command execution

### Integration Tests
- Create new post
- Edit existing post
- Paste from clipboard
- Drag and drop
- Multiple editor instances

### Manual Testing
- All ribbon commands
- Context menus
- Spell checking
- Template/theme loading
- Different blog providers

---

## Rollback Plan

Keep `MshtmlEditor` code intact and use factory pattern:

```csharp
public static IHtmlEditor CreateEditor(...)
{
    if (UseWebView2Editor)
        return new WebView2HtmlEditorControl(...);
    else
        return new MshtmlHtmlEditorControl(...); // Existing code
}
```

Can toggle via environment variable or config during testing.

---

## Success Criteria

1. ✅ All existing editor features work
2. ✅ HTML content saves correctly (no corruption)
3. ✅ Performance is acceptable (< 2 second load time)
4. ✅ Copy/paste from various sources works
5. ✅ Images can be inserted and resized
6. ✅ Tables work
7. ✅ Spell check works
8. ✅ No regressions in existing functionality

---

## Open Questions

1. **Spell Check Dictionary:** Do we need custom dictionary support? How to integrate?

2. **Smart Content:** How deep is the smart content integration? Need to analyze plugins.

3. **Performance:** Is TinyMCE fast enough for large posts? Need benchmarking.

4. **Offline:** TinyMCE must work fully offline (self-hosted). Verify no CDN calls.

5. **Accessibility:** Does TinyMCE meet accessibility requirements?

---

## References

- [TinyMCE Documentation](https://www.tiny.cloud/docs/)
- [WebView2 Documentation](https://learn.microsoft.com/en-us/microsoft-edge/webview2/)
- [WebView2 Host Objects](https://learn.microsoft.com/en-us/microsoft-edge/webview2/how-to/hostobject)
- [TinyMCE GitHub](https://github.com/tinymce/tinymce) - MIT License
- [Meziantou's WebView2 Bridge](https://www.meziantou.net/sharing-object-between-dotnet-host-and-webview2.htm)

---

## Appendix: MSHTML Removal Checklist

After WebView2 editor is complete, these files can be cleaned up:

### Can Remove
- [ ] `OpenLiveWriter.Mshtml/MshtmlEditor.cs`
- [ ] `OpenLiveWriter.Mshtml/MshtmlControl.cs`
- [ ] `OpenLiveWriter.Mshtml/Mshtml_Interop/*`
- [ ] Most of `OpenLiveWriter.Interop/Com/Mshtml*.cs`

### Must Keep (used elsewhere)
- [ ] Audit all `using mshtml;` statements
- [ ] Some HTML parsing may still use MSHTML types
- [ ] `LightWeightHTMLDocument` already has WebView2-compatible methods

### Final Step
Once all MSHTML references removed:
- Convert to SDK-style projects
- Retarget to .NET 10
- Remove COM references
- Update build scripts
