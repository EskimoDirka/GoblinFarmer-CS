param(
    [string]$Version = "1.0.0",
    [string]$Runtime = "win-x64",
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

function Stop-Publish {
    param([string]$Text)

    Write-Error $Text
    exit 1
}

function Invoke-Step {
    param(
        [string]$Description,
        [scriptblock]$Command
    )

    Write-Host ""
    Write-Host "==> $Description"
    & $Command
    if ($LASTEXITCODE -ne 0) {
        Stop-Publish "$Description failed."
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "GoblinFarmer.csproj"
$artifactsRoot = Join-Path $repoRoot "artifacts"
$publishRoot = Join-Path $artifactsRoot "publish"
$publishDir = Join-Path $publishRoot "GoblinFarmer"
$installerDir = Join-Path $repoRoot "Installer"
$installerScript = Join-Path $installerDir "GoblinFarmer.iss"
$installerOutput = Join-Path $artifactsRoot "installer"

if (!(Test-Path $projectPath)) {
    Stop-Publish "Project file not found: $projectPath"
}

$resolvedRepo = [System.IO.Path]::GetFullPath($repoRoot)
$resolvedPublishRoot = [System.IO.Path]::GetFullPath($publishRoot)
$resolvedPublishDir = [System.IO.Path]::GetFullPath($publishDir)
if (!$resolvedPublishDir.StartsWith($resolvedPublishRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    Stop-Publish "Publish directory resolved outside the artifacts publish root: $resolvedPublishDir"
}

Write-Host "GoblinFarmer release publish"
Write-Host "Repository: $resolvedRepo"
Write-Host "Version:    $Version"
Write-Host "Runtime:    $Runtime"

New-Item -ItemType Directory -Force -Path $publishRoot, $installerOutput | Out-Null
if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

Invoke-Step "dotnet publish self-contained Release" {
    dotnet publish $projectPath `
        --configuration Release `
        --runtime $Runtime `
        --self-contained true `
        --output $publishDir `
        -p:PublishSingleFile=false `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:AssemblyVersion=$Version `
        -p:FileVersion=$Version `
        -p:InformationalVersion=$Version
}

$exePath = Join-Path $publishDir "GoblinFarmer.exe"
$imagesPath = Join-Path $publishDir "Images"
$configPath = Join-Path $publishDir "Config\AppSettings.json"
$iconPath = Join-Path $publishDir "GoblinFarmerIcon.ico"

if (!(Test-Path $exePath)) {
    Stop-Publish "Published executable missing: $exePath"
}

if (!(Test-Path $imagesPath)) {
    Stop-Publish "Published Images folder missing: $imagesPath"
}

if (!(Test-Path $configPath)) {
    Stop-Publish "Published AppSettings.json missing: $configPath"
}

if (!(Test-Path $iconPath)) {
    Stop-Publish "Published icon missing: $iconPath"
}

$iscc = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
if (!$SkipInstaller -and $null -ne $iscc -and (Test-Path $installerScript)) {
    Invoke-Step "compile Inno Setup installer" {
        & $iscc.Source "/DMyAppVersion=$Version" "/DSourceDir=$publishDir" $installerScript
    }
}
elseif (!$SkipInstaller -and $null -eq $iscc) {
    Write-Warning "Inno Setup compiler (ISCC.exe) was not found. The self-contained publish folder is ready; install Inno Setup to build the installer."
}
elseif (!$SkipInstaller -and !(Test-Path $installerScript)) {
    Write-Warning "Installer script not found: $installerScript"
}

Write-Host ""
Write-Host "========== Release Publish Summary =========="
Write-Host "Publish folder: $publishDir"
Write-Host "Executable:     $exePath"
Write-Host "Config:         $configPath"
Write-Host "Images:         $imagesPath"
Write-Host "Installer out:  $installerOutput"
Write-Host "Install target: %LOCALAPPDATA%\Programs\GoblinFarmer"
Write-Host ""
Write-Host "Users can run GoblinFarmer.exe from the publish folder or install with the generated Inno Setup .exe."
