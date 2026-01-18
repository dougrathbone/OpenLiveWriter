# Removed/Disabled Features for .NET 10 Migration

This document tracks all features, functionality, and code that was removed, disabled, or stubbed out during the .NET 10 migration. Each item needs to be reviewed and properly reimplemented or permanently removed with a documented decision.

---

## ✅ RESTORED - Previously Excluded Files

These files were successfully restored during the migration:

### HtmlEditor Files (Restored 2026-01-18)
- `OpenLiveWriter.HtmlEditor/HtmlEditorElementBehaviorManager.cs` - Fixed by making `_editorContext_SelectionChanged` a no-op (each behavior handles selection independently)
- `OpenLiveWriter.HtmlEditor/PropertyEditingMiniForm.cs` - Fixed ApplicationStyleManager reference
- `OpenLiveWriter.HtmlEditor/Commands/CommandSpellCheck.cs` - Removed `container.Add(this)` call

### WebPublishMessages (Restored 2026-01-18)
- `OpenLiveWriter.FileDestinations/WebPublish/WebPublishMessages/*.cs` (6 files)
- Updated `WebPublishMessage` base class to support both modern (MessageId) and legacy (designer) patterns
- Added protected parameterless constructor and settable Text/Title/TextFormatArgs properties

### Google Drive Support (Restored 2026-01-18)
- `OpenLiveWriter.BlogClient/Clients/GoogleBloggerv3Client.cs` - Full Google Drive implementation restored
- Re-enabled `Google.Apis.Drive.v3` package reference
- Implemented `GetDriveService()`, `GetAllFolders()`, `GetBlogImagesFolder()`, `PostNewImage()`
- Added `GetMimeTypeFromExtension()` helper for image MIME type detection
- Images are automatically made publicly readable for blog embedding
- Note: Mixed Google.Apis versions (Blogger 1.69.x, Drive 1.70.x) cause binding redirect warnings but work correctly

### SharePoint Client (Restored 2026-01-18)
- `OpenLiveWriter.BlogClient/Clients/SharePointClient.cs` - Modern HTTP-based SOAP client
- Replaced legacy `System.Web.Services.Protocols.SoapHttpClientProtocol` with `HttpClient`-based implementation
- `SharePointListsService` now constructs SOAP envelopes manually and sends via HTTP
- Implemented `AddAttachment()`, `DeleteAttachment()`, `GetAttachmentCollection()` methods
- Supports NTLM/Basic authentication via `HttpClientHandler`
- Uses `SecurityElement.Escape()` for XML safety
- Re-enabled in `BlogClientManager` and `BlogServiceDetector`

### Image Editing Property Controls (Restored 2026-01-18)
- `OpenLiveWriter.PostEditor/PostHtmlEditing/ImageEditing/ImageEditingPropertySidebar.cs` - Restored
- `OpenLiveWriter.PostEditor/PostHtmlEditing/ImageEditing/ImagePropertyEditorControl.cs` - Restored
- Created stub implementations for missing tab page controls:
  - `ImageEditingTabPageControl` - Base class for tab pages
  - `ImageTabPageImageControl`, `ImageTabPageLayoutControl`, `ImageTabPageEffectsControl`, `ImageTabPageUploadControl`
- Fixed namespace references (`Api.ImageEditing` -> `Extensibility.ImageEditing`)
- Fixed `IBlogPostImageDataContext` -> `IBlogPostImageEditingContext`
- Added backward-compatible overload for `ShowImageDecoratorEditorDialog`
- Note: `ImageEditingPropertyForm.cs` still excluded (uses `MainFrameSatelliteWindow`)

### SixApart Atom Client (Restored 2026-01-18)
- `OpenLiveWriter.BlogClient/Clients/SixApartAtomClient.cs` - TypePad/Vox blog support
- Updated `BlogClientAttribute` to include protocol name parameter
- Updated constructor to match current `AtomClient` signature
- Replaced `Capabilities` property with `ConfigureClientOptions()` pattern
- Updated WSSE authentication to use modern `Login()` method for credentials
- Replaced `SHA1Managed.Create()` with `SHA1.Create()`

### CoreServices Utilities (Restored 2026-01-18)
- `OpenLiveWriter.CoreServices/WebRequest/InternetShortCut.cs` - .url file generation
- Fixed namespace: `using OpenLiveWriter.Interop.SHDocVw` for `IWebBrowser2`

### Platform Compatibility Attributes (Added 2026-01-18 - Commit 2.1)
Added `[SupportedOSPlatform("windows")]` attributes to P/Invoke classes for documentation:
- `OpenLiveWriter.Interop\Windows\*.cs` (User32, Kernel32, Shell32, Gdi32, etc.)
- `OpenLiveWriter.Interop\Com\Ole32.cs`, `Ole32Storage.cs`, `ImageLoader.cs`
- `OpenLiveWriter.Interop\Windows\TaskDialog\*.cs`
- `OpenLiveWriter.CoreServices\*.cs` (PluginLoader, PathHelper, DisplayHelper, CabinetFileExtractor)
- `OpenLiveWriter.ApplicationFramework\*TrackingIndicator.cs`
- `OpenLiveWriter.Localization\CultureHelper.cs`, `BidiGraphics.cs`
- `OpenLiveWriter.PostEditor\ContentEditor\ISettingsProvider.cs`
- `OpenLiveWriter\ApplicationMain.cs`
- `Canvas\CanvasForm.cs`

Note: CA1416 warnings are still suppressed globally since the entire application targets `net10.0-windows`.
The attributes serve as documentation for code reviewers and potential future cross-platform considerations.

---

## ❌ STILL EXCLUDED - Requires Significant Work

### 1. Auto-Update System (Squirrel.Windows)

**Status:** Disabled  
**Reason:** Squirrel.Windows 1.4.4 doesn't support .NET 10  
**Impact:** Application auto-update functionality is completely disabled  

**Files Affected:**
- `OpenLiveWriter/ApplicationMain.cs` - `RegisterSquirrelEventHandlers()` stubbed out
- `OpenLiveWriter.PostEditor/Updates/UpdateManager.cs` - `CheckforUpdates()` stubbed out
- `OpenLiveWriter/OpenLiveWriter.csproj` - Splat and squirrel.windows packages removed
- `OpenLiveWriter.PostEditor/OpenLiveWriter.PostEditor.csproj` - Same

**Recommended Action:** Migrate to [Velopack](https://github.com/velopack/velopack) which is the modern successor to Squirrel.Windows and supports .NET 10.

### 2. Image Editing Property Form (Legacy)

**Status:** Excluded from compilation  
**Reason:** Uses `MainFrameSatelliteWindow` class which doesn't exist in the current codebase.  
**Impact:** `ImageEditingPropertyForm` unavailable - but `ImageEditingPropertySidebar` works as an alternative.

**Files Affected:**
- `OpenLiveWriter.PostEditor/PostHtmlEditing/ImageEditing/ImageEditingPropertyForm.cs` - Uses `MainFrameSatelliteWindow`

**Recommended Action:** This form appears to be dead code since the sidebar version is available. Consider permanent removal.

### 3. MenuBuilder Entry (Dead Code)

**Status:** Dead code - permanently excluded  
**Reason:** `MenuBuilderEntry` was replaced by `CommandMenuBuilderEntry`. The old class references non-existent `MenuBuilder.MenuType` property.  
**Impact:** None - functionality exists in `CommandMenuBuilderEntry`  

**Files Affected:**
- `OpenLiveWriter.ApplicationFramework/MenuBuilderEntry.cs` - Excluded (dead code)

**Recommended Action:** Safe to delete permanently.

### 4. Designer Files (Design-Time Support)

**Status:** Excluded from compilation  
**Reason:** Complex design-time component issues, not needed for runtime  
**Impact:** Cannot use Visual Studio designer for these components  

**Files Affected:**
- `OpenLiveWriter.Controls/DisplayMessageDesigner.cs`
- `OpenLiveWriter.CoreServices/Diagnostics/UnexpectedErrorMessageDesigner.cs`
- `OpenLiveWriter.FileDestinations/WebPublish/WebPublishMessageDesigner.cs`

**Recommended Action:** Design-time support is optional. Low priority to restore.

### 5. CoreServices Legacy Files

**Status:** Excluded from compilation  
**Reason:** Various issues - obsolete code, old namespaces, .NET Framework-only APIs  

**Files Affected:**
- `AppIconCache.cs` - Marked [Obsolete], replaced by ShellHelper methods
- `HTML/WebPageDownloader.cs` - Uses old Project31 namespaces, references ExplorerBrowserControl
- `HTML/AsyncPageDownload.cs` - Uses old Project31 namespaces
- `WebRequest/MultiThreadedPageDownloader.cs` - Uses Project31 namespaces
- `WebRequest/CloseTrackingHttpWebRequest.cs` - Uses `System.Runtime.Remoting.Proxies.RealProxy` (not in .NET 10)
- `DataObject/SearchReferrerChain.cs` - Uses Project31 namespaces, references missing `ExplorerUrlTracker`
- `ComponentRootDesigner.cs` - Design-time support only

**Restored:** `WebRequest/InternetShortCut.cs` (see above)

**Recommended Action:** Most are legacy/dead code using deprecated .NET Framework APIs.

### 6. Test Files

**Status:** Excluded from compilation  
**Reason:** Reference non-existent types  

**Files Affected:**
- `OpenLiveWriter.UnitTest/Interop/SpellApiEx.cs` - References removed NLG spell library
- `OpenLiveWriter.UnitTest/CoreServices/ResourceDownloading/LocalCabResourceCacheTest.cs` - References removed LocalCabResourceCache

**Recommended Action:** Remove if tested functionality no longer exists, or update to test current implementations.

---

## Warnings Suppressed (Technical Debt)

These are not removed features but warnings that were suppressed to allow compilation.

### ✅ Fixed (2026-01-18)
- `SYSLIB0021` - SHA1Managed → Now using `SHA1.HashData()`
- `SYSLIB0023` - RNGCryptoServiceProvider → Now using `RandomNumberGenerator.Fill()`
- `CA2200` - Re-throwing exceptions → Fixed with `throw;` or `ExceptionDispatchInfo`

### Global Suppressions (Directory.Build.props) - Still Active
- `SYSLIB0003` - SecurityPermission, IPermission (COM interop security attributes)
- `SYSLIB0009` - AuthenticationManager (legacy web authentication)
- `SYSLIB0011` - BinaryFormatter (requires serialization rewrite)
- `SYSLIB0014` - WebRequest/HttpWebRequest (would require full HttpClient migration)
- `SYSLIB0050` - Type.IsSerializable (serialization related)
- `SYSLIB0051` - Formatter-based serialization (requires rewrite)
- `CA1416` - Platform compatibility (app is Windows-only by design)
- `CS9191` - ref vs in parameter (minor code style)
- `WFO1000`, `WFO1001` - WinForms serialization (internal detail)
- `WFDEV004`, `WFDEV006` - WinForms deprecated controls (ContextMenu, MenuItem, Form.Closed)
- `NU1701`, `NU1603`, `NU1605` - NuGet package compatibility

### Project-Specific Suppressions
- `CS0672` (OpenLiveWriter.PostEditor) - Form.OnClosing/OnClosed obsolete
- `CS0672` (OpenLiveWriter.Controls) - Same
- `CS0414` (OpenLiveWriter.HtmlEditor) - Unused fields
- `CS0618` (OpenLiveWriter.BlogClient) - Obsolete API usage

---

## Revision History

- **2026-01-18**: Commit 2.1 complete - Added SupportedOSPlatform attributes to P/Invoke classes
- **2026-01-18**: Updated document - marked restored files, removed outdated build errors section (build now succeeds)
- **2026-01-17**: Initial document created during .NET 10 migration
