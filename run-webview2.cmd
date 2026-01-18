@echo off
REM Run OpenLiveWriter with WebView2 editor enabled
set OLW_USE_WEBVIEW2_EDITOR=1
"%~dp0src\managed\bin\Debug\i386\Writer\OpenLiveWriter.exe" %*
