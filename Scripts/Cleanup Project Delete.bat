@echo off
setlocal

echo This will delete generated build outputs and prune DebugPackages to the cleanup default retention.
echo It will NOT delete runtime artifacts or old installers unless those flags are passed directly to cleanup-project.ps1.
echo.
choice /C YN /N /M "Run cleanup-project.ps1 -Delete now? [Y/N] "
if errorlevel 2 (
  echo Cleanup delete cancelled.
  echo.
  pause
  exit /b 0
)

powershell.exe ^
  -NoProfile ^
  -ExecutionPolicy Bypass ^
  -File "%~dp0cleanup-project.ps1" ^
  -Delete

pause
