@echo off
REM Run OpenLiveWriter with WebView2 editor enabled
set OLW_USE_WEBVIEW2_EDITOR=1
"%~dp0src\managed\bin\Debug\x64\Writer\OpenLiveWriter.exe" %*
