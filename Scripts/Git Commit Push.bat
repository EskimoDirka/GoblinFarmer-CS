@echo off
setlocal EnableExtensions DisableDelayedExpansion

pushd "%~dp0.." >nul 2>nul
if errorlevel 1 (
    echo Failed to open repository root from script location:
    echo %~dp0..
    echo.
    pause
    exit /b 1
)

where git >nul 2>nul
if errorlevel 1 (
    echo Git is not installed or is not available on PATH.
    echo.
    pause
    popd
    exit /b 1
)

git rev-parse --is-inside-work-tree >nul 2>nul
if errorlevel 1 (
    echo This folder is not a git repository:
    cd
    echo.
    pause
    popd
    exit /b 1
)

for /f "delims=" %%B in ('git branch --show-current 2^>nul') do set "CURRENT_BRANCH=%%B"
if not defined CURRENT_BRANCH (
    for /f "delims=" %%B in ('git rev-parse --short HEAD 2^>nul') do set "CURRENT_BRANCH=detached at %%B"
)

echo Repository:
cd
echo.
echo Current branch: %CURRENT_BRANCH%
echo.
echo Git status:
git status
if errorlevel 1 (
    echo.
    echo Failed to read git status.
    echo.
    pause
    popd
    exit /b 1
)

set "HAS_CHANGES="
for /f "delims=" %%S in ('git status --porcelain 2^>nul') do set "HAS_CHANGES=1"

if not defined HAS_CHANGES (
    echo.
    echo No changes to commit.
    echo.
    pause
    popd
    exit /b 0
)

echo.
echo Review the status above before staging all changes.
pause

echo.
echo Staging all changes with: git add -A
git add -A
if errorlevel 1 (
    echo.
    echo Failed to stage changes.
    echo.
    pause
    popd
    exit /b 1
)

echo.
set /p "COMMIT_MESSAGE=Enter commit message: "
set "COMMIT_MESSAGE_CHECK=%COMMIT_MESSAGE: =%"
if not defined COMMIT_MESSAGE_CHECK (
    echo.
    echo Commit message cannot be blank. No commit was created.
    echo.
    pause
    popd
    exit /b 1
)

echo.
echo Creating commit...
git commit -m "%COMMIT_MESSAGE%"
if errorlevel 1 (
    echo.
    echo Commit failed. Push will not be attempted.
    echo.
    pause
    popd
    exit /b 1
)

echo.
set /p "PUSH_CHANGES=Push changes? (Y/N): "
if /i not "%PUSH_CHANGES%"=="Y" (
    echo.
    echo Commit created. Push skipped by user.
    echo.
    pause
    popd
    exit /b 0
)

echo.
echo Pushing to the current tracked branch...
git push
if errorlevel 1 (
    echo.
    echo Push failed. See the git error above.
    echo.
    pause
    popd
    exit /b 1
)

echo.
echo Commit and push completed successfully.
echo.
pause
popd
exit /b 0
