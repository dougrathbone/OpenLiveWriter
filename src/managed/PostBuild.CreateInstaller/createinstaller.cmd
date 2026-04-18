@ECHO OFF

PUSHD "%~dp0..\..\..\"

CALL getversion.cmd

IF "%OLW_CONFIG%" == "" (
  echo %%OLW_CONFIG%% not set, will default to 'Debug'
  set OLW_CONFIG=Debug
)

:: Restore dotnet tools (includes vpk - Velopack CLI)
dotnet tool restore
IF %ERRORLEVEL% NEQ 0 (
   echo Failed to restore dotnet tools. Ensure .config\dotnet-tools.json is present.
   GOTO end
)

:: Create Velopack installer package
:: --packId: Application identifier (used for install directory name)
:: --packVersion: Version from version.txt
:: --packDir: Build output directory containing the application binaries
:: --mainExe: The main executable to launch
:: --icon: Application icon for the installer
:: --outputDir: Where to place the generated installer and update files
dotnet vpk pack ^
  --packId OpenLiveWriter ^
  --packVersion %dottedVersion% ^
  --packDir src\managed\bin\%OLW_CONFIG%\x64\Writer ^
  --mainExe OpenLiveWriter.exe ^
  --icon src\managed\OpenLiveWriter.PostEditor\Images\Writer.ico ^
  --outputDir Releases

IF %ERRORLEVEL% NEQ 0 (
   echo Velopack packaging failed.
   GOTO end
)

MOVE .\Releases\OpenLiveWriter-Setup.exe .\Releases\OpenLiveWriterSetup.exe
IF %ERRORLEVEL% NEQ 0 (
   echo Failed to rename OpenLiveWriter-Setup.exe. The file may not have been created by Velopack.
   GOTO end
)
ECHO Created Open Live Writer setup file.

:: Build Chocolatey package. Suppress package analysis since Chocolatey powershell generates verbose warnings.
IF EXIST "%LocalAppData%\Nuget\Nuget.exe" (
  "%LocalAppData%\Nuget\Nuget.exe" pack .\OpenLiveWriter.Install.nuspec -version %dottedVersion% -basepath Releases -nopackageanalysis
  ECHO Created Writer Chocolatey Package
) ELSE (
  echo Nuget.exe missing from %LocalAppData%\Nuget\Nuget.exe - skipping Chocolatey package
)

:end

POPD
