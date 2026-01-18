# Removed Features (.NET 10 Migration)

This document lists features, files, and functionality that were removed or disabled during the .NET 10 migration.

---

## Disabled Features

### Auto-Update System
**Status:** Disabled  
**Reason:** Squirrel.Windows doesn't support .NET 10  
**Recommendation:** Migrate to [Velopack](https://github.com/velopack/velopack)

---

## Deleted Files

These files were deleted as they were superseded by modern replacements:

| Deleted File | Replaced By |
|--------------|-------------|
| `ApplicationFramework/MenuBuilderEntry.cs` | `CommandMenuBuilderEntry.cs` |
| `PostEditor/ImageEditing/ImageEditingPropertyForm.cs` | `PictureEditingManager` (Ribbon UI) |
| `CoreServices/AppIconCache.cs` | `ShellHelper.GetXxxIcon()` |

---

## Excluded Files

These files are excluded from compilation (`<Compile Remove="..."/>`) due to incompatible APIs:

### CoreServices

| File | Reason |
|------|--------|
| `HTML/WebPageDownloader.cs` | Uses unavailable Project31 namespaces |
| `HTML/AsyncPageDownload.cs` | Uses unavailable Project31 namespaces |
| `WebRequest/MultiThreadedPageDownloader.cs` | Uses unavailable Project31 namespaces |
| `WebRequest/CloseTrackingHttpWebRequest.cs` | Uses `RealProxy` (not available in .NET 10) |
| `DataObject/SearchReferrerChain.cs` | Uses unavailable Project31 namespaces |

### Designer Files (Design-Time Only)

| File | Reason |
|------|--------|
| `CoreServices/ComponentRootDesigner.cs` | VS Designer support only |
| `CoreServices/Diagnostics/UnexpectedErrorMessageDesigner.cs` | VS Designer support only |
| `FileDestinations/DisplayMessageDesigner.cs` | VS Designer support only |
| `FileDestinations/WebPublishMessageDesigner.cs` | VS Designer support only |

---

## Suppressed Warnings

### Global Suppressions (Directory.Build.props)

| Warning | Reason |
|---------|--------|
| `SYSLIB0003` | Code Access Security (required for COM interop) |
| `SYSLIB0050` | Type.IsSerializable (legacy compatibility) |
| `SYSLIB0051` | Formatter-based serialization (RegistryCodec fallback) |
| `CA1416` | Platform compatibility (Windows-only application) |
| `WFDEV004/006` | Deprecated WinForms controls |

### Localized Suppressions

| File | Warning | Reason |
|------|---------|--------|
| `RegistryCodec.cs` | `SYSLIB0011` | BinaryFormatter read-only fallback |
| `HttpRequestHelper.cs` | `SYSLIB0009`, `SYSLIB0014` | Legacy WebRequest/AuthenticationManager |
| `WebRequestWithCache.cs` | `SYSLIB0014` | WebRequest fallback |
| `AsyncWebRequestWithCache.cs` | `SYSLIB0014` | WebRequest fallback |
| `TistoryBlogClient.cs` | `SYSLIB0014` | Tistory API |
| `GenericAtomClient.cs` | `SYSLIB0009` | Google login auth |
