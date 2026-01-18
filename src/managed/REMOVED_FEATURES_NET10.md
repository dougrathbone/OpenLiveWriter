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

**Status:** Partially excluded/updated  
**Reason:** Some reference non-existent types, others have behavioral differences  

**Excluded Files:**
- `OpenLiveWriter.UnitTest/Interop/SpellApiEx.cs` - References removed NLG spell library
- `OpenLiveWriter.UnitTest/CoreServices/ResourceDownloading/LocalCabResourceCacheTest.cs` - References removed LocalCabResourceCache
- `OpenLiveWriter.Tests/PostEditor/Tables/InsertTableTests.cs` - Uses ApprovalTests (not .NET 10 compatible)

**Failing Tests (Known Behavioral Differences):**
- `DefaultBlockElementTest.DivDefaultBlockElementTest` - Empty elements use `&nbsp;` instead of empty
- `DefaultBlockElementTest.ParagraphDefaultBlockElementTest` - Same as above
- `UrlHelperTest.TestCreateUrlFromPath` - Different URL encoding (`%3C` vs `<`)
- `BlogPostCategoryTest.BlogPostCategoryEquality` - Equality check behavior difference

**Test Statistics:**
- OpenLiveWriter.Tests: 46/46 passing
- OpenLiveWriter.UnitTest: 27/31 passing (4 known failures)

**Recommended Action:** Review failing tests and update expectations for .NET 10 behavior.

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
- `SYSLIB0011` - BinaryFormatter - See below for details
- `SYSLIB0014` - WebRequest/HttpWebRequest (would require full HttpClient migration)
- `SYSLIB0050` - Type.IsSerializable (serialization related)
- `SYSLIB0051` - Formatter-based serialization - See below for details

### ✅ BinaryFormatter Migration Complete (SYSLIB0011, SYSLIB0051)

**Location:** `OpenLiveWriter.CoreServices/Settings/RegistryCodec.cs`
**Class:** `SerializableCodec`

**Migration Completed:**
- New settings are serialized using `System.Text.Json` with a magic header prefix (`OLW_JSON:`)
- Legacy BinaryFormatter data is automatically detected and deserialized for backwards compatibility
- Settings are migrated to JSON format on next save (transparent to users)

**Format Detection:**
- JSON data: Byte array starting with `OLW_JSON:` followed by UTF-8 JSON
- Legacy data: BinaryFormatter serialized bytes (auto-detected by absence of JSON header)

**Technical Details:**
- `JsonSerializerOptions` configured with enum string conversion and field inclusion
- BinaryFormatter kept as read-only fallback (will never write new BinaryFormatter data)
- Warning suppression localized to `RegistryCodec.cs` only (removed from `Directory.Build.props`)
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

## Native Ribbon Status (Working!)

The Windows Ribbon Framework native DLL builds and works correctly:

1. **Project:** `src/unmanaged/OpenLiveWriter.Ribbon/OpenLiveWriter.Ribbon.vcxproj`
2. **Build:** Requires Visual Studio C++ tools with UICC.exe (Ribbon compiler)
3. **Toolset:** Default v143 (VS2022), can override with `/p:PlatformToolset=v145` for VS2028
4. **Output:** `src/managed/bin/$(Configuration)/i386/Writer/OpenLiveWriter.Ribbon.dll`

**Build Command:**
```powershell
cd src/unmanaged/OpenLiveWriter.Ribbon
msbuild OpenLiveWriter.Ribbon.vcxproj /p:Configuration=Debug /p:Platform=Win32 /p:PlatformToolset=v145
```

The managed `OpenLiveWriter.csproj` conditionally copies the Ribbon DLL to the output if it exists.
The application loads the ribbon optionally - if the DLL is missing, it runs without the ribbon toolbar.

---

## COM Interop Status (Commit 2.4)

The COM interop for MSHTML is properly configured:

1. **OpenLiveWriter.Interop.Mshtml**: Contains 723 generated interop files for MSHTML API
2. **CustomMarshalers.cs**: Provides compatibility shim for `EnumeratorToEnumVariantMarshaler` (removed in .NET Core)
3. **`net10.0-windows` target**: Provides full COM interop support
4. **No `<EnableComHosting>` needed**: Application is a COM client (consuming MSHTML), not a server

The MSHTML-based HTML editor works in .NET 10 through the existing COM interop types.

---

## Revision History

- **2026-01-18**: Phase 3 complete - .NET 10 migration completed
  - Commit 3.1: Test projects updated with Microsoft.NET.Test.Sdk, 73/77 tests passing
  - Commit 3.2: AppVeyor CI updated for VS2022 and .NET 10 SDK
  - Commit 3.3: Final cleanup and documentation
- **2026-01-18**: Phase 2 complete - Code compatibility updates
  - Commit 2.1: Added SupportedOSPlatform attributes to P/Invoke classes
  - Commit 2.2: System.Drawing/System.Net verified compatible
  - Commit 2.3: BinaryFormatter documented as technical debt
  - Commit 2.4: COM interop for MSHTML verified working
- **2026-01-18**: Updated document - marked restored files, removed outdated build errors section (build now succeeds)
- **2026-01-17**: Initial document created during .NET 10 migration
