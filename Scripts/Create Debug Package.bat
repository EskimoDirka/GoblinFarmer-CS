@echo off
setlocal

rem Supported debug ZIP export path for both VS Debug and Release.
rem This is the single intentional review package workflow; app shutdown stays quiet.
powershell.exe ^
  -NoProfile ^
  -ExecutionPolicy Bypass ^
  -File "%~dp0create-debug-package.ps1" %*

pause
