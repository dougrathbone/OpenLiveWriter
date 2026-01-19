# Open Live Writer: .NET 10 Migration Plan (Comprehensive)

## Executive Summary

This document outlines a comprehensive plan to migrate Open Live Writer from **.NET Framework 4.7.2** to **.NET 10 LTS** (released November 2025, supported until November 2028).

**Estimated Effort**: 3-6 months for a small team
**Risk Level**: Medium-High (due to extensive COM interop and P/Invoke usage)
**Recommended Approach**: Phased migration with intermediate .NET 8 target

---

## Table of Contents

1. [Current State Analysis](#1-current-state-analysis)
2. [Migration Benefits](#2-migration-benefits)
3. [Technical Challenges](#3-technical-challenges)
4. [Migration Strategy](#4-migration-strategy)
5. [Phase 1: Preparation](#5-phase-1-preparation)
6. [Phase 2: Project Conversion](#6-phase-2-project-conversion)
7. [Phase 3: Code Migration](#7-phase-3-code-migration)
8. [Phase 4: Testing & Validation](#8-phase-4-testing--validation)
9. [Phase 5: Deployment](#9-phase-5-deployment)
10. [Risk Assessment](#10-risk-assessment)
11. [Timeline](#11-timeline)
12. [Resources](#12-resources)

---

## 1. Current State Analysis

### 1.1 Project Structure

| Metric | Count |
|--------|-------|
| Total Projects | 32 |
| Library Projects | 28 |
| Executable Projects | 4 |
| Test Projects | 3 |

### 1.2 Target Framework
- **Current**: .NET Framework 4.7.2
- **Platform**: x64 only (x86 deprecated)
- **Build System**: MSBuild with custom `.targets` files

### 1.3 NuGet Dependencies

| Package | Version | .NET 10 Compatible | Notes |
|---------|---------|-------------------|-------|
| Microsoft.Web.WebView2 | 1.0.2903.40 | ✅ Yes | Core dependency |
| Newtonsoft.Json | 13.0.3 | ✅ Yes | Can migrate to System.Text.Json |
| Google.Apis.* | 1.39.0 | ⚠️ Update Required | Need v1.60+ for .NET 10 |
| squirrel.windows | 1.4.4 | ❌ No | **Migrate to Velopack** (see Section 3.1) |
| DeltaCompressionDotNet | 1.1.0 | ❌ Unknown | Test required |
| NUnit | 3.4.1 | ✅ Yes | Update to 4.x recommended |
| YamlDotNet | 6.1.1 | ✅ Yes | Update to 13.x recommended |
| Microsoft.Bcl.* | 1.1.10 | ❌ Remove | Built into .NET 10 |
| PlatformSpellCheck | 1.0.0 | ⚠️ Unknown | Test required |

### 1.4 Native Interop Analysis

#### P/Invoke Declarations (~120+)

| DLL | Usage Count | Migration Risk |
|-----|-------------|----------------|
| User32.dll | 40+ | Low |
| Kernel32.dll | 25+ | Low |
| Gdi32.dll | 15+ | Low |
| Shell32.dll | 10+ | Low |
| Ole32.dll | 10+ | Low |
| ComCtl32.dll | 5+ | Low |
| WinInet.dll | 5+ | Low |
| Others | 10+ | Low |

**Assessment**: P/Invoke is fully supported in .NET 10. No changes required.

#### COM Interop Interfaces (~140+)

| Category | Interface Count | Migration Risk |
|----------|-----------------|----------------|
| OLE Documents | 15+ | Medium |
| Shell Integration | 10+ | Medium |
| Data Operations | 10+ | Medium |
| UI Ribbon | 8+ | High |
| Persistence | 6+ | Low |
| Network/Protocol | 10+ | Medium |

**Assessment**: COM interop is supported but some patterns may need adjustment.

### 1.5 Windows-Specific APIs

| API | Files Using | Migration Path |
|-----|-------------|----------------|
| Registry (Microsoft.Win32) | 10+ | Use Microsoft.Win32.Registry package |
| WindowsIdentity | 3+ | Use System.Security.Principal |
| GDI+ | 20+ | Use System.Drawing.Common package |

---

## 2. Migration Benefits

### 2.1 Performance Improvements
- **30-50% faster startup** (JIT improvements)
- **Lower memory usage** (GC improvements)
- **Faster JSON serialization** (System.Text.Json)

### 2.2 Modern Features
- **C# 14** language features (field-backed properties, primary constructors)
- **Native AOT** option for faster startup
- **Better container support** for deployment
- **Enhanced debugging** with Hot Reload improvements

### 2.3 Long-Term Support
- **.NET 10 LTS** supported until November 2028
- **Active development** vs. .NET Framework (maintenance only)
- **Security updates** delivered faster

### 2.4 Developer Experience
- **SDK-style projects** (simpler .csproj files)
- **Central package management**
- **Faster builds** with incremental compilation
- **Better IDE integration**

---

## 3. Technical Challenges

### 3.1 Squirrel.Windows → Velopack Migration

**Problem**: Squirrel.Windows 1.4.4 does not support .NET 10.

**Solution**: Migrate to **Velopack** - the actively maintained successor to Squirrel.

#### Why Velopack?

| Feature | Squirrel.Windows | Velopack |
|---------|-----------------|----------|
| .NET 10 Support | ❌ No | ✅ Yes |
| Active Maintenance | ❌ Abandoned | ✅ Active |
| Delta Updates | ✅ Yes | ✅ Yes (faster) |
| Cross-Platform | ❌ Windows only | ✅ Win/Mac/Linux |
| Auto-Migration | N/A | ✅ From Squirrel |
| API Similarity | - | ~90% compatible |

#### Current Squirrel Usage in OLW

**Files to modify:**
1. `OpenLiveWriter\ApplicationMain.cs` - Event handlers (install/uninstall/update)
2. `OpenLiveWriter.PostEditor\Updates\UpdateManager.cs` - Background update check
3. `OpenLiveWriter\packages.config` - Package reference

**Current implementation (~70 lines of Squirrel code):**

```csharp
// ApplicationMain.cs - Current Squirrel code
using Squirrel;

private static void RegisterSquirrelEventHandlers(string downloadUrl)
{
    using (var mgr = new Squirrel.UpdateManager(downloadUrl))
    {
        SquirrelAwareApp.HandleEvents(
            onInitialInstall: v => InitialInstall(mgr),
            onFirstRun: () => FirstRun(mgr),
            onAppUpdate: v => OnAppUpdate(mgr),
            onAppUninstall: v => OnAppUninstall(mgr));
    }
}

// UpdateManager.cs - Current update check
using (var manager = new Squirrel.UpdateManager(downloadUrl))
{
    var update = await manager.CheckForUpdate();
    await manager.UpdateApp();
}
```

#### Velopack Migration Code

**Step 1: Replace NuGet package**
```xml
<!-- Remove -->
<package id="squirrel.windows" version="1.4.4" />

<!-- Add -->
<PackageReference Include="Velopack" Version="0.0.1298" />
```

**Step 2: Update ApplicationMain.cs**
```csharp
// NEW: Velopack implementation
using Velopack;

public static void Main(string[] args)
{
    // Velopack MUST be first line in Main()
    VelopackApp.Build()
        .WithFirstRun(v => FirstRun())
        .WithAfterInstallFastCallback(v => InitialInstall())
        .WithAfterUpdateFastCallback(v => OnAppUpdate())
        .WithBeforeUninstallFastCallback(v => OnAppUninstall())
        .Run();
    
    // Rest of existing Main() code...
    Application.EnableVisualStyles();
    // ...
}

private static void InitialInstall()
{
    // Create shortcuts, file associations
    var locator = VelopackLocator.GetDefault(null);
    var mgr = new UpdateManager(locator);
    mgr.CreateShortcutForThisExe(ShortcutLocation.StartMenu | ShortcutLocation.Desktop);
    
    SetAssociation(".wpost", "OPEN_LIVE_WRITER", 
        locator.CurrentlyInstalledVersion?.TargetFullPath ?? Application.ExecutablePath, 
        "Open Live Writer post");
}

private static void OnAppUninstall()
{
    // Clean up registry and data
    string OLWRegKey = @"SOFTWARE\OpenLiveWriter";
    Registry.CurrentUser.DeleteSubKeyTree(OLWRegKey, false);
    
    try
    {
        Directory.Delete(ApplicationEnvironment.LocalApplicationDataDirectory, true);
        Directory.Delete(ApplicationEnvironment.ApplicationDataDirectory, true);
    }
    catch { /* Ignore cleanup errors */ }
}
```

**Step 3: Update UpdateManager.cs**
```csharp
// NEW: Velopack update check
using Velopack;
using Velopack.Sources;

public class UpdateManager
{
    public static async Task CheckForUpdatesAsync(bool forceCheck = false)
    {
        if (!forceCheck && !UpdateSettings.AutoUpdate)
            return;
            
        var downloadUrl = UpdateSettings.CheckForBetaUpdates ?
            UpdateSettings.BetaUpdateDownloadUrl : UpdateSettings.UpdateDownloadUrl;

        try
        {
            var source = new SimpleWebSource(downloadUrl);
            var mgr = new Velopack.UpdateManager(source);
            
            // Check for updates
            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null)
            {
                Trace.WriteLine("No updates available.");
                return;
            }
            
            // Verify update is newer (not older)
            var currentVersion = mgr.CurrentVersion;
            if (newVersion.TargetFullRelease.Version <= currentVersion)
            {
                Trace.WriteLine($"Update {newVersion.TargetFullRelease.Version} is not newer than {currentVersion}");
                return;
            }
            
            // Download and apply
            await mgr.DownloadUpdatesAsync(newVersion);
            Trace.WriteLine($"Update downloaded: {newVersion.TargetFullRelease.Version}");
            
            // Will apply on next restart
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Update check failed: {ex.Message}");
        }
    }
}
```

**Step 4: Update build/packaging**
```powershell
# Install Velopack CLI
dotnet tool install -g vpk

# Package the app (replaces Squirrel packaging)
vpk pack `
    --packId "OpenLiveWriter" `
    --packVersion "0.7.0" `
    --packDir "./src/managed/bin/Release/x64/Writer" `
    --mainExe "OpenLiveWriter.exe" `
    --outputDir "./Releases"

# Generate delta updates
vpk delta `
    --baseDir "./Releases/previous" `
    --targetDir "./Releases/current"
```

#### Automatic Migration from Squirrel

**Good news**: Velopack automatically migrates existing Squirrel installations!

When an existing Squirrel user updates to the new Velopack version:
1. Velopack detects Squirrel installation artifacts
2. Shortcuts are updated to point to new location
3. Registry entries are migrated
4. Old Squirrel files are cleaned up
5. User continues receiving updates via Velopack

**No user action required** - migration is seamless.

#### Testing the Migration

1. Create test VM with current Squirrel-based OLW installed
2. Build new Velopack version with same app ID
3. Publish update to test channel
4. Verify existing install updates correctly
5. Verify shortcuts, file associations still work
6. Verify clean install works
7. Verify uninstall cleans up properly

### 3.2 HIGH: UI Ribbon COM Interop

**Problem**: Custom Windows Ribbon Framework integration via COM.

**Files Affected**:
- `OpenLiveWriter.ApplicationFramework/CommandManager/`
- Native `OpenLiveWriter.Ribbon.dll` (C++)

**Options**:
1. Keep COM interop (works but complex)
2. Migrate to WPF Ribbon (requires UI rewrite)
3. Use third-party ribbon control

**Recommendation**: Keep existing COM interop, test thoroughly.

### 3.3 MEDIUM: MSHTML/SHDocVw Interop

**Current State**: Already migrated to WebView2! ✅

**Remaining**: `OpenLiveWriter.Interop.Mshtml` and `OpenLiveWriter.Mshtml` projects contain legacy code.

**Recommendation**: 
- Remove MSHTML interop assemblies if WebView2 migration is complete
- Keep as compatibility layer if needed for edge cases

### 3.4 MEDIUM: Project File Format

**Problem**: Old-style .csproj files with complex dependencies.

**Solution**: Convert to SDK-style projects using .NET Upgrade Assistant.

### 3.5 LOW: Registry Access

**Problem**: Direct Registry access not included by default in .NET 10.

**Solution**: Add `Microsoft.Win32.Registry` NuGet package.

---

## 4. Migration Strategy

### 4.1 Recommended Approach: Phased Migration

```
Phase 1: Preparation (2-3 weeks)
    │
    ▼
Phase 2: Project Conversion (2-4 weeks)
    │
    ▼
Phase 3: Code Migration (4-8 weeks)
    │
    ▼
Phase 4: Testing & Validation (2-4 weeks)
    │
    ▼
Phase 5: Deployment (1-2 weeks)
```

### 4.2 Migration Order (Bottom-Up)

```
Layer 1 (No UI dependencies):
├── OpenLiveWriter.HtmlParser
├── OpenLiveWriter.Localization
├── OpenLiveWriter.Api
└── OpenLiveWriter.Extensibility

Layer 2 (Core services):
├── OpenLiveWriter.Interop
├── OpenLiveWriter.CoreServices
└── OpenLiveWriter.BlogClient

Layer 3 (UI infrastructure):
├── OpenLiveWriter.Controls
├── OpenLiveWriter.ApplicationFramework
├── OpenLiveWriter.WebView2Shim
└── OpenLiveWriter.HtmlEditor

Layer 4 (Application):
├── OpenLiveWriter.PostEditor
├── OpenLiveWriter.BrowserControl
└── OpenLiveWriter (main exe)

Layer 5 (Tools - optional):
├── BlogRunner.*
├── LocUtil
└── MarketXmlGenerator
```

---

## 5. Phase 1: Preparation

### 5.1 Prerequisites

- [ ] Install .NET 10 SDK
- [ ] Install Visual Studio 2026 with .NET 10 workload
- [ ] Install .NET Upgrade Assistant (VS extension or CLI)
- [ ] Create migration branch: `feature/dotnet10-migration`

### 5.2 Dependency Audit

```powershell
# Run .NET Upgrade Assistant analyze mode
upgrade-assistant analyze writer.sln --source ./src/managed
```

### 5.3 NuGet Package Updates

**Before migration**, update to latest .NET Framework-compatible versions:

| Package | Current | Update To |
|---------|---------|-----------|
| Google.Apis.* | 1.39.0 | 1.68.0 |
| YamlDotNet | 6.1.1 | 15.x |
| NUnit | 3.4.1 | 4.x |

### 5.4 Remove Obsolete Dependencies

These are built into .NET 10:
- `Microsoft.Bcl`
- `Microsoft.Bcl.Async`
- `Microsoft.Bcl.Build`
- `Microsoft.Net.Http`

### 5.5 Evaluate Squirrel Replacement

Test these alternatives:
```powershell
# Install Velopack CLI
dotnet tool install -g vpk

# Evaluate MSIX
# Evaluate ClickOnce
```

---

## 6. Phase 2: Project Conversion

### 6.1 Convert to SDK-Style Projects

**Option A: .NET Upgrade Assistant (Recommended)**

```powershell
# Interactive mode
upgrade-assistant upgrade writer.sln --target-tfm net10.0-windows

# Or project-by-project
upgrade-assistant upgrade OpenLiveWriter.HtmlParser.csproj
```

**Option B: Manual Conversion**

For each project, replace old .csproj with SDK-style:

```xml
<!-- Old Format -->
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="14.0" ...>
  <Import Project="..\..\..\..\writer.build.settings" />
  <PropertyGroup>
    <ProjectGuid>{GUID}</ProjectGuid>
    ...
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="System" />
    <Reference Include="System.Core" />
    ...
  </ItemGroup>
  <ItemGroup>
    <Compile Include="Class1.cs" />
    ...
  </ItemGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>

<!-- New SDK-Style Format -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="..." Version="..." />
  </ItemGroup>
</Project>
```

### 6.2 Directory.Build.props

Create centralized build settings:

```xml
<!-- Directory.Build.props (repo root) -->
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <PlatformTarget>x64</PlatformTarget>
  </PropertyGroup>
  
  <PropertyGroup>
    <Company>Open Live Writer</Company>
    <Copyright>Copyright (c) Open Live Writer. All rights reserved.</Copyright>
  </PropertyGroup>
</Project>
```

### 6.3 Directory.Packages.props

Central package version management:

```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageVersion Include="Microsoft.Web.WebView2" Version="1.0.2903.40" />
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageVersion Include="Google.Apis.Blogger.v3" Version="1.68.0" />
    <!-- ... -->
  </ItemGroup>
</Project>
```

---

## 7. Phase 3: Code Migration

### 7.1 API Compatibility Changes

#### 7.1.1 System.Drawing

```csharp
// Add to .csproj
<PackageReference Include="System.Drawing.Common" Version="8.0.0" />

// Code change: None required for basic usage
```

#### 7.1.2 Registry Access

```csharp
// Add to .csproj
<PackageReference Include="Microsoft.Win32.Registry" Version="5.0.0" />

// Code: No changes required
```

#### 7.1.3 Encoding.Default

```csharp
// .NET Framework
Encoding.Default  // Returns system ANSI codepage

// .NET 10 - different behavior!
Encoding.Default  // Returns UTF-8

// Fix: Be explicit
Encoding.GetEncoding("windows-1252")  // Or specific codepage
```

#### 7.1.4 AppDomain

```csharp
// .NET Framework
AppDomain.CurrentDomain.BaseDirectory
AppDomain.CreateDomain(...)  // NOT SUPPORTED in .NET 10

// .NET 10
AppContext.BaseDirectory
// For isolation: Use AssemblyLoadContext
```

### 7.2 COM Interop Adjustments

#### 7.2.1 ComImport Interfaces

Most work unchanged. Watch for:

```csharp
// May need adjustment for nullable references
[ComImport]
[Guid("...")]
interface IMyInterface
{
    void Method([MarshalAs(UnmanagedType.BStr)] string? param);  // Note nullable
}
```

#### 7.2.2 COM Activation

```csharp
// .NET Framework
Type type = Type.GetTypeFromProgID("Shell.Application");
dynamic shell = Activator.CreateInstance(type);

// .NET 10 - Same, but add:
<PropertyGroup>
  <EnableComHosting>true</EnableComHosting>
</PropertyGroup>
```

### 7.3 Windows Forms Changes

#### 7.3.1 High DPI

```csharp
// .NET 10 has better High DPI support
// Update Application.SetHighDpiMode in Program.cs
Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
```

#### 7.3.2 Form Designer

- SDK-style projects use new `.designer.cs` format
- May need to regenerate some designer files
- Resource files (`.resx`) format is the same

### 7.4 Specific File Changes

#### 7.4.1 OpenLiveWriter.Interop Changes

```csharp
// File: Kernel32.cs
// Add platform check for cross-platform safety (even though Windows-only)
#if WINDOWS
[DllImport("kernel32.dll")]
public static extern bool SetDllDirectory(string path);
#endif
```

#### 7.4.2 Remove MSHTML Dependencies (if WebView2 complete)

```xml
<!-- Remove these projects from solution -->
OpenLiveWriter.Interop.Mshtml
OpenLiveWriter.Mshtml
```

---

## 8. Phase 4: Testing & Validation

### 8.1 Test Categories

| Category | Priority | Automated |
|----------|----------|-----------|
| Build verification | Critical | Yes |
| Unit tests (NUnit) | High | Yes |
| UI automation | Medium | Partial |
| Manual smoke test | High | No |
| Blog provider tests | High | Partial |
| Plugin compatibility | Medium | No |

### 8.2 Test Plan

#### 8.2.1 Build Verification
```powershell
dotnet build src/managed/writer.sln -c Release
dotnet test src/managed/writer.sln
```

#### 8.2.2 Smoke Test Checklist

- [ ] Application starts without errors
- [ ] Splash screen displays correctly
- [ ] Main window renders (Ribbon, panels)
- [ ] WebView2 editor loads and accepts input
- [ ] Source view (CodeMirror) works
- [ ] Can create new post
- [ ] Can open existing post
- [ ] Can save draft locally
- [ ] Blog configuration wizard works
- [ ] Can publish to test blog
- [ ] Images insert correctly
- [ ] Spell check works
- [ ] Update check works (with new system)
- [ ] All menus/commands functional

#### 8.2.3 Blog Provider Tests

- [ ] WordPress (self-hosted)
- [ ] WordPress.com
- [ ] Blogger
- [ ] TypePad
- [ ] MetaWeblog API generic

### 8.3 Performance Baseline

Capture before/after metrics:

```powershell
# Startup time
Measure-Command { Start-Process OpenLiveWriter.exe -Wait }

# Memory usage
Get-Process OpenLiveWriter | Select WorkingSet64

# Cold start vs warm start
```

---

## 9. Phase 5: Deployment

### 9.1 Velopack Deployment (Replaces Squirrel)

**Velopack is now the definitive choice** for OLW's update system. See Section 3.1 for full migration code.

#### 9.1.1 Build Pipeline Changes

```powershell
# build-release.ps1 - Updated for Velopack

# 1. Build the application
dotnet publish src/managed/OpenLiveWriter/OpenLiveWriter.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o ./publish

# 2. Install Velopack CLI (if not already)
dotnet tool install -g vpk

# 3. Package with Velopack
vpk pack `
    --packId "OpenLiveWriter" `
    --packVersion $env:OLW_VERSION `
    --packDir "./publish" `
    --mainExe "OpenLiveWriter.exe" `
    --icon "./src/managed/OpenLiveWriter.CoreServices/Images/ApplicationIcon.ico" `
    --packTitle "Open Live Writer" `
    --packAuthors "Open Live Writer Contributors" `
    --outputDir "./Releases"

# 4. Generate delta updates (for existing users)
vpk delta `
    --basePackage "./Releases/previous/OpenLiveWriter-$env:PREV_VERSION-full.nupkg" `
    --newPackage "./Releases/OpenLiveWriter-$env:OLW_VERSION-full.nupkg" `
    --outputDir "./Releases"
```

#### 9.1.2 Release Artifacts

Velopack generates these files in `./Releases/`:

| File | Purpose |
|------|---------|
| `OpenLiveWriter-0.7.0-full.nupkg` | Full install package |
| `OpenLiveWriter-0.7.0-delta.nupkg` | Delta update (smaller) |
| `OpenLiveWriter-0.7.0-win-Setup.exe` | Standalone installer |
| `RELEASES` | Version manifest |

#### 9.1.3 Update Server Requirements

Same as Squirrel - just static file hosting:
- Azure Blob Storage (current)
- GitHub Releases
- Amazon S3
- Any HTTP server

**Update URL format**: `https://olw.blob.core.windows.net/stable/Releases/`

#### 9.1.4 CI/CD Integration (AppVeyor)

```yaml
# appveyor.yml updates
build_script:
  - ps: .\build.ps1 Release x64

after_build:
  - ps: |
      dotnet tool install -g vpk
      vpk pack --packId OpenLiveWriter --packVersion $env:APPVEYOR_BUILD_VERSION `
        --packDir ./src/managed/bin/Release/x64/Writer `
        --mainExe OpenLiveWriter.exe --outputDir ./Releases

artifacts:
  - path: Releases\*.nupkg
  - path: Releases\*-Setup.exe
  - path: Releases\RELEASES

deploy:
  - provider: AzureBlob
    storage_account_name: olw
    storage_access_key:
      secure: <encrypted>
    container: stable
    folder: Releases
```

### 9.2 Installer Changes

```powershell
# Velopack packaging
vpk pack --packId OpenLiveWriter --packVersion 0.7.0 \
    --packDir ./publish --mainExe OpenLiveWriter.exe
```

### 9.3 Runtime Deployment Options

| Option | Size | Startup | Recommended |
|--------|------|---------|-------------|
| Framework-dependent | ~5 MB | Fast (if runtime installed) | Development |
| Self-contained | ~150 MB | Fast | Production |
| Single-file | ~70 MB | Slower first run | Optional |
| Native AOT | ~30 MB | Fastest | Future option |

**Recommended**: Self-contained for user installs

```xml
<PropertyGroup>
  <PublishSingleFile>false</PublishSingleFile>
  <SelfContained>true</SelfContained>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
</PropertyGroup>
```

---

## 10. Risk Assessment

### 10.1 Risk Matrix

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| ~~Squirrel incompatibility~~ | ~~High~~ | ~~Critical~~ | ✅ **RESOLVED**: Migrate to Velopack (Section 3.1) |
| Velopack migration issues | Low | Medium | Test on VM with existing Squirrel install |
| COM interop breaks | Medium | High | Extensive testing, keep fallbacks |
| Third-party plugin breaks | Medium | Medium | Document breaking changes |
| Performance regression | Low | Medium | Benchmark before/after |
| Build system complexity | Medium | Low | Incremental migration |
| Google API changes | Low | Medium | Update packages first |

### 10.2 Rollback Plan

1. Keep `main` branch on .NET Framework
2. Maintain parallel `feature/dotnet10-migration` branch
3. Can release .NET Framework hotfixes during migration
4. Feature flags for gradual rollout

---

## 11. Timeline

### 11.1 Conservative Estimate (Part-time, 1-2 developers)

| Phase | Duration | Milestone |
|-------|----------|-----------|
| Phase 1: Preparation | 2-3 weeks | Dependencies audited, branch created |
| Phase 2: Project Conversion | 3-4 weeks | All projects SDK-style, builds |
| Phase 3: Code Migration | 6-8 weeks | All code compiles, tests pass |
| Phase 4: Testing | 3-4 weeks | Full test pass, performance verified |
| Phase 5: Deployment | 2 weeks | Release candidate ready |
| **Total** | **16-21 weeks** | |

### 11.2 Aggressive Estimate (Full-time, 2-3 developers)

| Phase | Duration |
|-------|----------|
| Phase 1 | 1 week |
| Phase 2 | 2 weeks |
| Phase 3 | 4 weeks |
| Phase 4 | 2 weeks |
| Phase 5 | 1 week |
| **Total** | **10 weeks** |

---

## 12. Resources

### 12.1 Documentation

- [.NET 10 What's New](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
- [WinForms Migration Guide](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/migration/)
- [.NET Upgrade Assistant](https://learn.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-overview)
- [COM Interop in .NET](https://learn.microsoft.com/en-us/dotnet/core/native-interop/)
- [Velopack Documentation](https://velopack.io/)

### 12.2 Tools

- .NET Upgrade Assistant (VS extension or `dotnet tool install -g upgrade-assistant`)
- .NET Portability Analyzer
- API Analyzer for deprecated APIs

### 12.3 Community

- [Open Live Writer GitHub Discussions](https://github.com/OpenLiveWriter/OpenLiveWriter/discussions)
- [.NET Discord](https://aka.ms/dotnet-discord)

---

## Appendix A: Project Migration Checklist

For each project:

- [ ] Run Upgrade Assistant analyze
- [ ] Convert to SDK-style .csproj
- [ ] Update NuGet packages
- [ ] Remove obsolete packages (Microsoft.Bcl.*)
- [ ] Fix nullable reference warnings
- [ ] Fix deprecated API usage
- [ ] Update unit tests
- [ ] Verify builds
- [ ] Verify tests pass
- [ ] Code review

---

## Appendix B: Breaking Changes Reference

### B.1 Common .NET Framework → .NET 10 Breaks

| API | Change | Fix |
|-----|--------|-----|
| `Encoding.Default` | Returns UTF-8 | Use explicit encoding |
| `AppDomain.CreateDomain` | Not supported | Use AssemblyLoadContext |
| `Thread.Abort` | Throws PlatformNotSupportedException | Use CancellationToken |
| `Remoting` | Not supported | Use gRPC or similar |
| `Code Access Security` | Not supported | Use OS-level security |
| `BinaryFormatter` | Obsolete/dangerous | Use System.Text.Json |

### B.2 Windows Forms Specific

| API | Change | Fix |
|-----|--------|-----|
| `Form.Menu` | MainMenuStrip preferred | Already using MenuStrip |
| `DataGrid` | Use DataGridView | Already using DataGridView |
| High DPI | Better support | Use PerMonitorV2 |

---

## Appendix C: OLW-Specific Considerations

### C.1 WebView2 Migration Status (COMPLETE ✅)

The hardest part of .NET 10 migration - removing MSHTML - is already done:
- WebView2 WYSIWYG editor working
- CodeMirror 5 source editor working
- Local image support working
- Content synchronization working

### C.2 Remaining MSHTML Code

These projects can be removed or kept as stubs:
- `OpenLiveWriter.Interop.Mshtml` - COM interop definitions
- `OpenLiveWriter.Mshtml` - MSHTML wrapper classes

### C.3 Native C++ Ribbon DLL

The `OpenLiveWriter.Ribbon.dll` (C++) will continue to work:
- P/Invoke calls work the same in .NET 10
- May need to rebuild with updated VS toolset
- Consider migrating to managed ribbon in future

---

*Document Version: 2.0*
*Created: January 2026*
*Last Updated: January 2026*
*Previous Version: See NET10-MIGRATION-PLAN.md (original WebView2 focus)*
