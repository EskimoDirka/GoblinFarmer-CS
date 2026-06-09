@echo off
setlocal

rem Creates a lightweight docs-only Project Brain ZIP for AI-assisted development.
powershell.exe ^
  -NoProfile ^
  -ExecutionPolicy Bypass ^
  -File "%~dp0create-project-brain.ps1"

pause
