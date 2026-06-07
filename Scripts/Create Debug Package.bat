@echo off
setlocal

rem Supported manual ZIP export path.
rem Normal runtime diagnostics stay in-app; VS Debug Goblin Tracker review files
rem are generated automatically under Debug\GoblinReplayReview\Latest.
powershell.exe ^
  -NoProfile ^
  -ExecutionPolicy Bypass ^
  -File "%~dp0create-debug-package.ps1"

pause
