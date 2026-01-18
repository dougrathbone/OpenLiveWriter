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
| `HttpRequestHelper.cs` | `SYSLIB0009` | WSSE auth for SixApart/TypePad blogs |
| `HttpRequestHelper.cs` | `SYSLIB0014` | WebRequest factory for legacy blog clients |
| `GenericAtomClient.cs` | `SYSLIB0009` | Google login auth |

**Migration Progress:**
- ✅ `WebRequestWithCache.cs` - Fully migrated to HttpClient
- ✅ `AsyncWebRequestWithCache.cs` - Fully migrated to HttpClient
- ✅ `ContentTypeHelper.cs` - Updated to use HttpResponseMessage
- ✅ `TistoryBlogClient.cs` - Fully migrated to HttpClientXmlRestRequestHelper
- ✅ `YouTubeVideoService.cs` - Fully migrated to HttpClientService
- ✅ `YoutubeVideoPublisher.cs` - Partially migrated (DELETE, IsVideoCompleted use HttpClient)
- ✅ `LiveJournalClient.cs` - FotobilderRequestManager fully migrated to HttpClient
- ✅ `ResourceFileDownloader.cs` - Fully migrated to HttpClientService
- ✅ `PluginHttpRequest.cs` - Fully migrated to HttpClientService
- ✅ `HockeyAppProxy.cs` - Fully migrated to HttpClientService
- ✅ `GDataCaptchaForm.cs` - Fully migrated to HttpClientService
- ✅ `WebImageSource.cs` - Fully migrated to HttpClientService
- ✅ `DestinationValidator.cs` - Migrated to use HttpRequestHelper.CheckUrlReachable
- ✅ `HttpRequestHelper.cs` - Added HttpClient-based methods
- ✅ `HttpClientRedirectHelper.cs` - New HttpClient-based redirect helper (replaces RedirectHelper)
- ✅ `HttpClientXmlRestRequestHelper.cs` - New HttpClient-based XML REST helper (replaces XmlRestRequestHelper)
- ✅ `PostEditorMainControl.ValidateHtml` - Migrated to use HttpClient
- ✅ `MultiThreadedPageDownloader.cs` - Re-enabled (namespace updated from Project31 to OpenLiveWriter)
- ✅ `CloseTrackingHttpWebRequest.cs` - Rewritten without `RealProxy` using wrapper pattern for .NET 10 compatibility

**Remaining Legacy HttpWebRequest Usages (deferred):**
- `AtomClient.cs` / `AtomMediaUploader.cs` / `BloggerAtomClient.cs` - Complex Picasa file upload with multipart MIME
- `YouTubeUploadRequestHelper` - Multipart video upload
- `XmlRpcClient.cs` - XML-RPC infrastructure with auth filters
- `XmlRestRequestHelper.cs` / `RedirectHelper.cs` - Core infrastructure (used by above)
- Blog detection infrastructure (`WriterEditingManifest.cs`, `BlogEditingTemplateDetector.cs`, etc.)

These legacy usages are suppressed via `SYSLIB0014` in `HttpRequestHelper.cs` and work correctly. Full migration would require significant refactoring of the multipart upload patterns and authentication infrastructure.

**For New Code:**
- Use `HttpClientRedirectHelper` instead of `RedirectHelper`
- Use `HttpClientXmlRestRequestHelper` instead of `XmlRestRequestHelper`
- Use `HttpRequestHelper.HttpClient` or the new HttpClient-based methods instead of `CreateHttpWebRequest`
