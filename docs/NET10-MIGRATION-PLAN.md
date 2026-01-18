# Open Live Writer - .NET 10 Migration Plan

## Overview
Upgrading Open Live Writer from .NET Framework 4.6.1 to .NET 10, with WebView2 replacing MSHTML (IE) as the HTML rendering/editing engine.

## Current State
- **Branch:** `feature/webview2` - https://github.com/OpenLiveWriter/OpenLiveWriter/tree/feature/webview2
- **Target Framework:** .NET Framework 4.7.2 (upgraded from 4.6.1 for WebView2 compatibility)
- **Build Command:** `.\build.ps1 /p:PlatformToolset=v145`

## The Problem
MSHTML (Internet Explorer's HTML engine) is deeply embedded throughout the codebase (~200+ files) and uses COM interop that is **not compatible with .NET Core/.NET 10**:
- `EnumeratorToEnumVariantMarshaler` was removed in .NET Core
- `IHTMLDocument2/3/4` and related interfaces require this marshaler
- Microsoft.Windows.Compatibility package does NOT include this marshaler

## Strategy
**Phase 1: WebView2 on .NET Framework (Current)**
- Replace MSHTML with WebView2 while staying on .NET Framework
- Can test incrementally, lower risk
- WebView2 works on both .NET Framework 4.6.2+ AND .NET 10

**Phase 2: .NET 10 Migration (Future)**
- Once MSHTML is removed, convert projects to SDK-style
- Retarget to .NET 10
- Update NuGet packages

---

## Phase 1 Progress: WebView2 Integration

### Completed ✅
1. **WebView2 NuGet Package Added** - `Microsoft.Web.WebView2.1.0.2903.40`
2. **WebView2BrowserControl.cs** - Implements `IBrowserControl` interface
3. **BrowserControlFactory.cs** - Toggle via `OLW_USE_WEBVIEW2=1` env var
4. **BrowserMiniForm.cs** - Updated to use factory pattern
5. **TLS 1.2 Support** - Fixed HTTPS connectivity for modern servers
6. **Debug Logging** - `[OLW-DEBUG]` prefix for DebugView filtering

### TODO 📋

#### Phase 1a: Simple Browser Uses
- [ ] `OpenLiveWriter.CoreServices/HtmlScreenCaptureCore.cs`
- [ ] `OpenLiveWriter.CoreServices/HTML/WebPageDownloader.cs`
- [ ] `OpenLiveWriter.CoreServices/BrowserOperationInvoker.cs`
- [ ] `OpenLiveWriter.CoreServices/WebRequest/WebPageDownloader.cs`
- [ ] `OpenLiveWriter.InternalWriterPlugin/MapControl.cs`

#### Phase 1b: DOM Interop Layer
- [ ] Create `WebView2DomHelper.cs` with common DOM operations
- [ ] Map IHTMLElement operations to JavaScript equivalents

#### Phase 1c: HTML Editor (Major Work)
- [ ] Analyze `OpenLiveWriter.Mshtml/MshtmlEditor.cs` (106+ IHTMLDocument refs)
- [ ] Design WebView2-based editing with contenteditable
- [ ] Handle paste, drag-drop, undo/redo, spell checking

---

## Build & Test

```powershell
# Build
.\build.ps1 /p:PlatformToolset=v145

# Run with IE (default)
.\src\managed\bin\Debug\i386\Writer\OpenLiveWriter.exe

# Run with WebView2
$env:OLW_USE_WEBVIEW2 = "1"
.\src\managed\bin\Debug\i386\Writer\OpenLiveWriter.exe
```

---

## Key Files
| File | Purpose |
|------|---------|
| `OpenLiveWriter.BrowserControl/IBrowserControl.cs` | Browser abstraction interface |
| `OpenLiveWriter.BrowserControl/WebView2BrowserControl.cs` | WebView2 implementation |
| `OpenLiveWriter.BrowserControl/BrowserControlFactory.cs` | Factory for switching |
| `OpenLiveWriter.Mshtml/MshtmlEditor.cs` | Main HTML editing engine (needs rewrite) |
| `writer.build.settings` | Build settings |
