@echo off
setlocal

rem Maintenance-only generated artifact cleanup. Default mode is DRY RUN.
powershell.exe ^
  -NoProfile ^
  -ExecutionPolicy Bypass ^
  -File "%~dp0cleanup-project.ps1"

pause
