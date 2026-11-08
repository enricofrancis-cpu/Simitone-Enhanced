@echo off
setlocal
cd /d "%~dp0lib"
Simitone.exe -profile="%~dp0TestProfile" %*
if errorlevel 1 pause
