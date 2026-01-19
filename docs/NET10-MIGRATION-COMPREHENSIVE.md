# Open Live Writer: Modernization Plan

## Overview

This plan is divided into **two independent phases** that can be executed separately:

| Phase | Goal | Framework | Risk | Duration |
|-------|------|-----------|------|----------|
| **Phase A** | Squirrel → Velopack | .NET Framework 4.7.2 | Low | 2-3 weeks |
| **Phase B** | .NET Framework → .NET 10 | .NET 10 | Medium-High | 10-18 weeks |

**Why separate phases?**
- Velopack supports both .NET Framework AND .NET 10
- Can ship Velopack update to all users on current .NET Framework
- Reduces risk - one major change at a time
- Phase A can be done immediately; Phase B can wait

---

# PHASE A: Velopack Migration (Installer & Auto-Update)

**Target**: Stay on .NET Framework 4.7.2, replace Squirrel with Velopack

**Estimated Duration**: 2-3 weeks

## A.1 Why Velopack?

| Feature | Squirrel.Windows | Velopack |
|---------|-----------------|----------|
| .NET Framework Support | ✅ Yes | ✅ Yes |
| .NET 10 Support | ❌ No | ✅ Yes |
| Active Maintenance | ❌ Abandoned | ✅ Active |
| Delta Updates | ✅ Yes | ✅ Yes (faster) |
| Cross-Platform | ❌ Windows only | ✅ Win/Mac/Linux |
| Auto-Migration from Squirrel | N/A | ✅ Built-in |

## A.2 Current Squirrel Code (~70 lines)

**Files affected:**
- `OpenLiveWriter\ApplicationMain.cs` - Install/uninstall/update hooks
- `OpenLiveWriter.PostEditor\Updates\UpdateManager.cs` - Background update check
- `OpenLiveWriter\packages.config` - NuGet reference

## A.3 Migration Steps

### Step 1: Update NuGet Package

```xml
<!-- packages.config: Remove -->
<package id="squirrel.windows" version="1.4.4" />
<package id="Splat" version="2.0.0" />
<package id="DeltaCompressionDotNet" version="1.1.0" />
<package id="Mono.Cecil" version="0.9.6.4" />

<!-- packages.config: Add -->
<package id="Velopack" version="0.0.1298" targetFramework="net472" />
```

### Step 2: Update ApplicationMain.cs

```csharp
// OLD: Squirrel
using Squirrel;

public static void Main(string[] args)
{
    Kernel32.SetDllDirectory("");
    Application.EnableVisualStyles();
    
    // Squirrel event registration (later in code)
    RegisterSquirrelEventHandlers(downloadUrl);
    // ...
}

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
```

```csharp
// NEW: Velopack
using Velopack;

public static void Main(string[] args)
{
    // IMPORTANT: Velopack MUST be first thing in Main()
    VelopackApp.Build()
        .WithFirstRun((v) => OnFirstRun())
        .WithAfterInstallFastCallback((v) => OnInstall())
        .WithAfterUpdateFastCallback((v) => OnUpdate())
        .WithBeforeUninstallFastCallback((v) => OnUninstall())
        .Run();

    // Then existing code...
    Kernel32.SetDllDirectory("");
    Application.EnableVisualStyles();
    // ...
}

private static void OnInstall()
{
    try
    {
        // Create shortcuts
        var locator = VelopackLocator.GetDefault(null);
        
        // File association for .wpost files
        var exePath = locator?.CurrentlyInstalledVersion?.TargetFullPath 
            ?? Application.ExecutablePath;
        SetAssociation(".wpost", "OPEN_LIVE_WRITER", exePath, "Open Live Writer post");
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Install callback error: {ex.Message}");
    }
}

private static void OnFirstRun()
{
    // Any first-run logic (currently just creates shortcuts, handled by Velopack)
    Trace.WriteLine("First run after install");
}

private static void OnUpdate()
{
    Trace.WriteLine("App updated successfully");
}

private static void OnUninstall()
{
    try
    {
        // Clean up registry
        string OLWRegKey = @"SOFTWARE\OpenLiveWriter";
        Registry.CurrentUser.DeleteSubKeyTree(OLWRegKey, false);
        
        // Clean up app data (optional - may want to keep user data)
        // Directory.Delete(ApplicationEnvironment.LocalApplicationDataDirectory, true);
        // Directory.Delete(ApplicationEnvironment.ApplicationDataDirectory, true);
    }
    catch (Exception ex)
    {
        Trace.WriteLine($"Uninstall cleanup error: {ex.Message}");
    }
}
```

### Step 3: Update UpdateManager.cs

```csharp
// OLD: Squirrel
using Squirrel;

private static ThreadStart UpdateOpenLiveWriter(string downloadUrl, bool checkNow)
{
    return async () =>
    {
        if (checkNow)
        {
            using (var manager = new Squirrel.UpdateManager(downloadUrl))
            {
                var update = await manager.CheckForUpdate();
                await manager.UpdateApp();
            }
        }
    };
}
```

```csharp
// NEW: Velopack
using Velopack;
using Velopack.Sources;

public static void CheckforUpdates(bool forceCheck = false)
{
    var checkNow = forceCheck || UpdateSettings.AutoUpdate;
    if (!checkNow) return;

    var downloadUrl = UpdateSettings.CheckForBetaUpdates ?
        UpdateSettings.BetaUpdateDownloadUrl : UpdateSettings.UpdateDownloadUrl;

    // Schedule update check 10 seconds after launch
    var delayUpdate = new DelayUpdateHelper(
        CheckForUpdatesAsync(downloadUrl), 
        UPDATELAUNCHDELAY);
    delayUpdate.StartBackgroundUpdate("Background OpenLiveWriter application update");
}

private static ThreadStart CheckForUpdatesAsync(string downloadUrl)
{
    return async () =>
    {
        try
        {
            var source = new SimpleWebSource(downloadUrl);
            var mgr = new Velopack.UpdateManager(source);
            
            // Check if we're in a Velopack-installed app
            if (!mgr.IsInstalled)
            {
                Trace.WriteLine("Not a Velopack install, skipping update check");
                return;
            }
            
            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null)
            {
                Trace.WriteLine("No updates available");
                return;
            }
            
            // Verify it's actually newer
            if (newVersion.TargetFullRelease.Version <= mgr.CurrentVersion)
            {
                Trace.WriteLine($"Available version {newVersion.TargetFullRelease.Version} " +
                    $"is not newer than current {mgr.CurrentVersion}");
                return;
            }
            
            Trace.WriteLine($"Downloading update: {newVersion.TargetFullRelease.Version}");
            await mgr.DownloadUpdatesAsync(newVersion);
            
            Trace.WriteLine("Update downloaded, will apply on next restart");
            // Update applies automatically on next app start
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Update check failed: {ex.Message}");
        }
    };
}
```

### Step 4: Update Build Script

```powershell
# build-release.ps1 - Add after existing build

# Install Velopack CLI (one-time)
if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    dotnet tool install -g vpk
}

# Package with Velopack
$version = Get-Content .\version.txt
$writerDir = ".\src\managed\bin\Release\x64\Writer"

vpk pack `
    --packId "OpenLiveWriter" `
    --packVersion $version `
    --packDir $writerDir `
    --mainExe "OpenLiveWriter.exe" `
    --icon ".\src\managed\OpenLiveWriter.CoreServices\Images\ApplicationIcon.ico" `
    --outputDir ".\Releases"

Write-Host "Velopack package created in .\Releases\"
```

### Step 5: Update AppVeyor CI

```yaml
# appveyor.yml additions
after_build:
  - ps: |
      dotnet tool install -g vpk
      $version = Get-Content version.txt
      vpk pack --packId OpenLiveWriter --packVersion $version `
        --packDir ./src/managed/bin/Release/x64/Writer `
        --mainExe OpenLiveWriter.exe `
        --outputDir ./Releases

artifacts:
  - path: Releases\*.nupkg
  - path: Releases\*Setup.exe
  - path: Releases\RELEASES
```

## A.4 Testing Checklist

- [ ] Clean install works (new user)
- [ ] Existing Squirrel install migrates automatically
- [ ] Shortcuts created correctly
- [ ] .wpost file association works
- [ ] Auto-update downloads new version
- [ ] Update applies on restart
- [ ] Uninstall removes registry keys
- [ ] Beta channel updates work

## A.5 Rollout Plan

1. **Week 1**: Implement Velopack, test internally
2. **Week 2**: Release to beta channel (`CheckForBetaUpdates = true`)
3. **Week 3**: Release to stable channel if no issues

**Key benefit**: Existing users on Squirrel will automatically migrate to Velopack when they receive this update.

---

# PHASE B: .NET 10 Migration

**Target**: Migrate from .NET Framework 4.7.2 to .NET 10 LTS

**Prerequisite**: Phase A complete (Velopack working)

**Estimated Duration**: 10-18 weeks

---

## B.1 Current State Analysis

### B.1.1 Project Structure

| Metric | Count |
|--------|-------|
| Total Projects | 32 |
| Library Projects | 28 |
| Executable Projects | 4 |
| Test Projects | 3 |

### B.1.2 Target Framework
- **Current**: .NET Framework 4.7.2
- **Target**: .NET 10.0-windows (LTS until Nov 2028)
- **Platform**: x64 only (x86 deprecated)
- **Build System**: MSBuild with custom `.targets` files → SDK-style projects

### B.1.3 NuGet Dependencies

| Package | Version | .NET 10 Compatible | Notes |
|---------|---------|-------------------|-------|
| Microsoft.Web.WebView2 | 1.0.2903.40 | ✅ Yes | Core dependency |
| Newtonsoft.Json | 13.0.3 | ✅ Yes | Can migrate to System.Text.Json |
| **Velopack** | 0.0.1298 | ✅ Yes | **From Phase A** |
| Google.Apis.* | 1.39.0 | ⚠️ Update | Need v1.60+ for .NET 10 |
| NUnit | 3.4.1 | ✅ Yes | Update to 4.x recommended |
| YamlDotNet | 6.1.1 | ✅ Yes | Update to 15.x recommended |
| Microsoft.Bcl.* | 1.1.10 | ❌ Remove | Built into .NET 10 |
| PlatformSpellCheck | 1.0.0 | ⚠️ Unknown | Test required |

### B.1.4 Native Interop Analysis

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

### B.1.5 Windows-Specific APIs

| API | Files Using | Migration Path |
|-----|-------------|----------------|
| Registry (Microsoft.Win32) | 10+ | Use Microsoft.Win32.Registry package |
| WindowsIdentity | 3+ | Use System.Security.Principal |
| GDI+ | 20+ | Use System.Drawing.Common package |

---

## B.2 Migration Benefits

### B.2.1 Performance Improvements
- **30-50% faster startup** (JIT improvements)
- **Lower memory usage** (GC improvements)
- **Faster JSON serialization** (System.Text.Json)

### B.2.2 Modern Features
- **C# 14** language features (field-backed properties, primary constructors)
- **Native AOT** option for faster startup
- **Better container support** for deployment
- **Enhanced debugging** with Hot Reload improvements

### B.2.3 Long-Term Support
- **.NET 10 LTS** supported until November 2028
- **Active development** vs. .NET Framework (maintenance only)
- **Security updates** delivered faster

### B.2.4 Developer Experience
- **SDK-style projects** (simpler .csproj files)
- **Central package management**
- **Faster builds** with incremental compilation
- **Better IDE integration**

---

## B.3 Technical Challenges

### B.3.1 UI Ribbon COM Interop (High Risk)

**Problem**: Custom Windows Ribbon Framework integration via COM.

**Files Affected**:
- `OpenLiveWriter.ApplicationFramework/CommandManager/`
- Native `OpenLiveWriter.Ribbon.dll` (C++)

**Options**:
1. Keep COM interop (works but complex)
2. Migrate to WPF Ribbon (requires UI rewrite)
3. Use third-party ribbon control

**Recommendation**: Keep existing COM interop, test thoroughly.

### B.3.2 MSHTML/SHDocVw Interop (Resolved)

**Current State**: Already migrated to WebView2! ✅

**Remaining**: `OpenLiveWriter.Interop.Mshtml` and `OpenLiveWriter.Mshtml` projects contain legacy code.

**Recommendation**: 
- Remove MSHTML interop assemblies if WebView2 migration is complete
- Keep as compatibility layer if needed for edge cases

### B.3.3 Project File Format (Medium Risk)

**Problem**: Old-style .csproj files with complex dependencies.

**Solution**: Convert to SDK-style projects using .NET Upgrade Assistant.

### B.3.4 Registry Access (Low Risk)

**Problem**: Direct Registry access not included by default in .NET 10.

**Solution**: Add `Microsoft.Win32.Registry` NuGet package.

---

## B.4 Migration Strategy

### B.4.1 Recommended Approach: Phased Migration

```
Sub-Phase B1: Preparation (2-3 weeks)
    │
    ▼
Sub-Phase B2: Project Conversion (2-4 weeks)
    │
    ▼
Sub-Phase B3: Code Migration (4-8 weeks)
    │
    ▼
Sub-Phase B4: Testing & Validation (2-4 weeks)
    │
    ▼
Sub-Phase B5: Deployment (1-2 weeks)
```

### B.4.2 Migration Order (Bottom-Up)

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

## B.5 Sub-Phase B1: Preparation

### B.5.1 Prerequisites

- [ ] Install .NET 10 SDK
- [ ] Install Visual Studio 2026 with .NET 10 workload
- [ ] Install .NET Upgrade Assistant (VS extension or CLI)
- [ ] Create migration branch: `feature/dotnet10-migration`

### B.5.2 Dependency Audit

```powershell
# Run .NET Upgrade Assistant analyze mode
upgrade-assistant analyze writer.sln --source ./src/managed
```

### B.5.3 NuGet Package Updates

**Before migration**, update to latest .NET Framework-compatible versions:

| Package | Current | Update To |
|---------|---------|-----------|
| Google.Apis.* | 1.39.0 | 1.68.0 |
| YamlDotNet | 6.1.1 | 15.x |
| NUnit | 3.4.1 | 4.x |

### B.5.4 Remove Obsolete Dependencies

These are built into .NET 10:
- `Microsoft.Bcl`
- `Microsoft.Bcl.Async`
- `Microsoft.Bcl.Build`
- `Microsoft.Net.Http`

### B.5.5 Verify Velopack Migration Complete

**Prerequisite**: Phase A must be complete before starting Phase B.

Verify:
- [ ] Velopack is working in current .NET Framework 4.7.2 build
- [ ] Updates are being distributed via Velopack
- [ ] No Squirrel dependencies remain

---

## B.6 Sub-Phase B2: Project Conversion

### B.6.1 Convert to SDK-Style Projects

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

### B.6.2 Directory.Build.props

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

### B.6.3 Directory.Packages.props

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

## B.7 Sub-Phase B3: Code Migration

### B.7.1 API Compatibility Changes

#### B.7.1.1 System.Drawing

```csharp
// Add to .csproj
<PackageReference Include="System.Drawing.Common" Version="8.0.0" />

// Code change: None required for basic usage
```

#### B.7.1.2 Registry Access

```csharp
// Add to .csproj
<PackageReference Include="Microsoft.Win32.Registry" Version="5.0.0" />

// Code: No changes required
```

#### B.7.1.3 Encoding.Default

```csharp
// .NET Framework
Encoding.Default  // Returns system ANSI codepage

// .NET 10 - different behavior!
Encoding.Default  // Returns UTF-8

// Fix: Be explicit
Encoding.GetEncoding("windows-1252")  // Or specific codepage
```

#### B.7.1.4 AppDomain

```csharp
// .NET Framework
AppDomain.CurrentDomain.BaseDirectory
AppDomain.CreateDomain(...)  // NOT SUPPORTED in .NET 10

// .NET 10
AppContext.BaseDirectory
// For isolation: Use AssemblyLoadContext
```

### B.7.2 COM Interop Adjustments

#### B.7.2.1 ComImport Interfaces

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

#### B.7.2.2 COM Activation

```csharp
// .NET Framework
Type type = Type.GetTypeFromProgID("Shell.Application");
dynamic shell = Activator.CreateInstance(type);

// .NET 10 - Same, but add:
<PropertyGroup>
  <EnableComHosting>true</EnableComHosting>
</PropertyGroup>
```

### B.7.3 Windows Forms Changes

#### B.7.3.1 High DPI

```csharp
// .NET 10 has better High DPI support
// Update Application.SetHighDpiMode in Program.cs
Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
```

#### B.7.3.2 Form Designer

- SDK-style projects use new `.designer.cs` format
- May need to regenerate some designer files
- Resource files (`.resx`) format is the same

### B.7.4 Specific File Changes

#### B.7.4.1 OpenLiveWriter.Interop Changes

```csharp
// File: Kernel32.cs
// Add platform check for cross-platform safety (even though Windows-only)
#if WINDOWS
[DllImport("kernel32.dll")]
public static extern bool SetDllDirectory(string path);
#endif
```

#### B.7.4.2 Remove MSHTML Dependencies (if WebView2 complete)

```xml
<!-- Remove these projects from solution -->
OpenLiveWriter.Interop.Mshtml
OpenLiveWriter.Mshtml
```

---

## B.8 Sub-Phase B4: Testing & Validation

### B.8.1 Test Categories

| Category | Priority | Automated |
|----------|----------|-----------|
| Build verification | Critical | Yes |
| Unit tests (NUnit) | High | Yes |
| UI automation | Medium | Partial |
| Manual smoke test | High | No |
| Blog provider tests | High | Partial |
| Plugin compatibility | Medium | No |

### B.8.2 Test Plan

#### B.8.2.1 Build Verification
```powershell
dotnet build src/managed/writer.sln -c Release
dotnet test src/managed/writer.sln
```

#### B.8.2.2 Smoke Test Checklist

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

#### B.8.2.3 Blog Provider Tests

- [ ] WordPress (self-hosted)
- [ ] WordPress.com
- [ ] Blogger
- [ ] TypePad
- [ ] MetaWeblog API generic

### B.8.3 Performance Baseline

Capture before/after metrics:

```powershell
# Startup time
Measure-Command { Start-Process OpenLiveWriter.exe -Wait }

# Memory usage
Get-Process OpenLiveWriter | Select WorkingSet64

# Cold start vs warm start
```

---

## B.9 Sub-Phase B5: Deployment

### B.9.1 Velopack .NET 10 Packaging

**Note**: Velopack was already migrated in Phase A. This section covers .NET 10-specific changes.

#### B.9.1.1 Build Pipeline for .NET 10

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

#### B.9.1.2 Release Artifacts

Velopack generates these files in `./Releases/`:

| File | Purpose |
|------|---------|
| `OpenLiveWriter-0.7.0-full.nupkg` | Full install package |
| `OpenLiveWriter-0.7.0-delta.nupkg` | Delta update (smaller) |
| `OpenLiveWriter-0.7.0-win-Setup.exe` | Standalone installer |
| `RELEASES` | Version manifest |

#### B.9.1.3 Update Server Requirements

Same as Squirrel - just static file hosting:
- Azure Blob Storage (current)
- GitHub Releases
- Amazon S3
- Any HTTP server

**Update URL format**: `https://olw.blob.core.windows.net/stable/Releases/`

#### B.9.1.4 CI/CD Integration (AppVeyor)

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

### B.9.2 Runtime Deployment Options

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

## B.10 Risk Assessment

### B.10.1 Risk Matrix

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| ~~Squirrel incompatibility~~ | ~~High~~ | ~~Critical~~ | ✅ **RESOLVED**: Phase A handles Velopack migration |
| COM interop breaks | Medium | High | Extensive testing, keep fallbacks |
| Third-party plugin breaks | Medium | Medium | Document breaking changes |
| Performance regression | Low | Medium | Benchmark before/after |
| Build system complexity | Medium | Low | Incremental migration |
| Google API changes | Low | Medium | Update packages first |

### B.10.2 Rollback Plan

1. Keep `main` branch on .NET Framework
2. Maintain parallel `feature/dotnet10-migration` branch
3. Can release .NET Framework hotfixes during migration
4. Feature flags for gradual rollout

---

## B.11 Timeline

### B.11.1 Conservative Estimate (Part-time, 1-2 developers)

| Phase | Duration | Milestone |
|-------|----------|-----------|
| B1: Preparation | 2-3 weeks | Dependencies audited, branch created |
| B2: Project Conversion | 3-4 weeks | All projects SDK-style, builds |
| B3: Code Migration | 6-8 weeks | All code compiles, tests pass |
| B4: Testing | 3-4 weeks | Full test pass, performance verified |
| B5: Deployment | 2 weeks | Release candidate ready |
| **Total** | **16-21 weeks** | |

### B.11.2 Aggressive Estimate (Full-time, 2-3 developers)

| Phase | Duration |
|-------|----------|
| B1 | 1 week |
| B2 | 2 weeks |
| B3 | 4 weeks |
| B4 | 2 weeks |
| B5 | 1 week |
| **Total** | **10 weeks** |

---

## B.12 Resources

### B.12.1 Documentation

- [.NET 10 What's New](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
- [WinForms Migration Guide](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/migration/)
- [.NET Upgrade Assistant](https://learn.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-overview)
- [COM Interop in .NET](https://learn.microsoft.com/en-us/dotnet/core/native-interop/)
- [Velopack Documentation](https://velopack.io/)

### B.12.2 Tools

- .NET Upgrade Assistant (VS extension or `dotnet tool install -g upgrade-assistant`)
- .NET Portability Analyzer
- API Analyzer for deprecated APIs

### B.12.3 Community

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

## Appendix: Breaking Changes Reference

### Common .NET Framework → .NET 10 Breaks

| API | Change | Fix |
|-----|--------|-----|
| `Encoding.Default` | Returns UTF-8 | Use explicit encoding |
| `AppDomain.CreateDomain` | Not supported | Use AssemblyLoadContext |
| `Thread.Abort` | Throws PlatformNotSupportedException | Use CancellationToken |
| `Remoting` | Not supported | Use gRPC or similar |
| `Code Access Security` | Not supported | Use OS-level security |
| `BinaryFormatter` | Obsolete/dangerous | Use System.Text.Json |

### Windows Forms Specific

| API | Change | Fix |
|-----|--------|-----|
| `Form.Menu` | MainMenuStrip preferred | Already using MenuStrip |
| `DataGrid` | Use DataGridView | Already using DataGridView |
| High DPI | Better support | Use PerMonitorV2 |

---

## Appendix: OLW-Specific Considerations

### WebView2 Migration Status (COMPLETE ✅)

The hardest part of .NET 10 migration - removing MSHTML - is already done:
- WebView2 WYSIWYG editor working
- CodeMirror 5 source editor working
- Local image support working
- Content synchronization working

### Remaining MSHTML Code

These projects can be removed or kept as stubs:
- `OpenLiveWriter.Interop.Mshtml` - COM interop definitions
- `OpenLiveWriter.Mshtml` - MSHTML wrapper classes

### Native C++ Ribbon DLL

The `OpenLiveWriter.Ribbon.dll` (C++) will continue to work:
- P/Invoke calls work the same in .NET 10
- May need to rebuild with updated VS toolset
- Consider migrating to managed ribbon in future

---

*Document Version: 2.0*
*Created: January 2026*
*Last Updated: January 2026*
*Previous Version: See NET10-MIGRATION-PLAN.md (original WebView2 focus)*
