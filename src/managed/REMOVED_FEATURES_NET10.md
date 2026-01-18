# .NET 10 Migration Status

This document tracks the status of the .NET 10 migration for Open Live Writer.

---

## ✅ Migration Complete

**Tests:** 77/77 passing (100%)  
**Application:** Builds and runs successfully  
**Ribbon UI:** Working (native C++ DLL)

---

## ✅ Successfully Restored Features

All major features were preserved during the migration:

### Blog Clients
- **Google Blogger v3** with Drive image upload
- **SharePoint** - Modernized with HttpClient-based SOAP
- **SixApart Atom** (TypePad/Vox) - Updated authentication
- **All other clients** - Working as expected

### Image Editing
- **Ribbon-based editing** via `PictureEditingManager` (active)
- All image decorators, effects, and commands working

### HTML Editor
- **MSHTML COM interop** working via `net10.0-windows`
- Custom marshaling for `EnumeratorToEnumVariantMarshaler`

### Core Services
- **Settings serialization** - Migrated from BinaryFormatter to JSON (backwards compatible)
- **Registry persistence** - Working with automatic format migration

---

## 🗑️ Deleted Legacy Code

These files were deleted as they were superseded by modern replacements:

| Deleted File | Replaced By | Reason |
|--------------|-------------|--------|
| `MenuBuilderEntry.cs` | `CommandMenuBuilderEntry.cs` | Broken code, missing dependencies |
| `ImageEditingPropertyForm.cs` | `PictureEditingManager` (Ribbon) | Old floating window, superseded by ribbon UI |
| `AppIconCache.cs` | `ShellHelper.GetXxxIcon()` | Marked `[Obsolete(..., error: true)]` |

---

## ⚠️ Excluded Legacy Files

These files remain excluded but represent no feature loss:

### CoreServices - Legacy .NET Framework APIs
| File | Issue |
|------|-------|
| `HTML/WebPageDownloader.cs` | Uses Project31 namespaces |
| `HTML/AsyncPageDownload.cs` | Uses Project31 namespaces |
| `WebRequest/MultiThreadedPageDownloader.cs` | Uses Project31 namespaces |
| `WebRequest/CloseTrackingHttpWebRequest.cs` | Uses `RealProxy` (not in .NET 10) |
| `DataObject/SearchReferrerChain.cs` | Uses Project31 namespaces |
| `ComponentRootDesigner.cs` | Design-time only |
| `Diagnostics/UnexpectedErrorMessageDesigner.cs` | Design-time only |

### Designer Files (Design-Time Support)
| File | Impact |
|------|--------|
| `DisplayMessageDesigner.cs` | VS Designer only |
| `WebPublishMessageDesigner.cs` | VS Designer only |

---

## ⏳ Future Improvements (Not Blocking)

### 1. Auto-Update System
**Status:** Disabled  
**Reason:** Squirrel.Windows doesn't support .NET 10  
**Recommendation:** Migrate to [Velopack](https://github.com/velopack/velopack)

### 2. WebRequest → HttpClient Migration
**Status:** ✅ Completed (localized suppressions only)  
**Details:** New `HttpClientService` class provides modern HttpClient-based API. Legacy WebRequest usages are suppressed with localized `#pragma` directives.  
**Files Added:** `OpenLiveWriter.CoreServices/WebRequest/HttpClientService.cs`  
**Files Updated:** `HttpRequestHelper.cs`, `WebRequestWithCache.cs`, `AsyncWebRequestWithCache.cs`, `TistoryBlogClient.cs`, `GenericAtomClient.cs`

---

## Global Warning Suppressions

### Directory.Build.props
| Warning | Reason |
|---------|--------|
| `SYSLIB0003` | Code Access Security (COM interop) |
| `SYSLIB0050` | Type.IsSerializable |
| `SYSLIB0051` | Formatter-based serialization (RegistryCodec fallback) |
| `CA1416` | Platform compatibility (Windows-only app) |
| `WFDEV004/006` | Deprecated WinForms controls |
| `NU1701/1603/1605` | NuGet compatibility |

### Localized Suppressions
| File | Warning | Reason |
|------|---------|--------|
| `RegistryCodec.cs` | `SYSLIB0011` | BinaryFormatter read-only fallback |
| `HttpRequestHelper.cs` | `SYSLIB0009` | AuthenticationManager for WSSE |
| `HttpRequestHelper.cs` | `SYSLIB0014` | WebRequest backward compatibility |
| `WebRequestWithCache.cs` | `SYSLIB0014` | WebRequest fallback for non-HTTP URLs |
| `AsyncWebRequestWithCache.cs` | `SYSLIB0014` | WebRequest fallback for non-HTTP URLs |
| `TistoryBlogClient.cs` | `SYSLIB0014` | WebRequest for Tistory API |
| `GenericAtomClient.cs` | `SYSLIB0009` | AuthenticationManager for Google login |

---

## Native Ribbon

**Project:** `src/unmanaged/OpenLiveWriter.Ribbon/OpenLiveWriter.Ribbon.vcxproj`  
**Status:** ✅ Working

```powershell
# Build command (VS2028)
msbuild OpenLiveWriter.Ribbon.vcxproj /p:Configuration=Debug /p:Platform=Win32 /p:PlatformToolset=v145
```

The ribbon loads optionally - application runs without it if the DLL is missing.

---

## Settings Serialization (BinaryFormatter Migration)

**Location:** `OpenLiveWriter.CoreServices/Settings/RegistryCodec.cs`

- **New data:** Serialized as JSON with `OLW_JSON:` prefix
- **Legacy data:** BinaryFormatter deserialization (read-only fallback)
- **Migration:** Automatic on next save

---

## Revision History

- **2026-01-18**: WebRequest migration - Added HttpClientService, localized SYSLIB0014/0009 suppressions
- **2026-01-18**: Cleanup - deleted superseded legacy code (MenuBuilderEntry, ImageEditingPropertyForm, AppIconCache)
- **2026-01-18**: BinaryFormatter migrated to JSON with backwards compatibility
- **2026-01-18**: All 77 tests passing, application running with ribbon UI
- **2026-01-18**: Phase 2 & 3 complete - full .NET 10 migration
- **2026-01-17**: Initial migration started
