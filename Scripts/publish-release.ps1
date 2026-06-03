param(
    [string]$Version,
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

function Get-InnoSetupCompiler {
    $iscc = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($null -ne $iscc) {
        return $iscc.Source
    }

    $candidatePaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 5\ISCC.exe"
    )

    foreach ($candidatePath in $candidatePaths) {
        if (![string]::IsNullOrWhiteSpace($candidatePath) -and (Test-Path -LiteralPath $candidatePath)) {
            return $candidatePath
        }
    }

    return $null
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

[xml]$projectXml = Get-Content -LiteralPath $projectPath
$propertyGroups = @($projectXml.Project.PropertyGroup)
$projectVersion = $propertyGroups |
    ForEach-Object { $_.Version } |
    Where-Object { ![string]::IsNullOrWhiteSpace($_) } |
    Select-Object -First 1
$projectAssemblyVersion = $propertyGroups |
    ForEach-Object { $_.AssemblyVersion } |
    Where-Object { ![string]::IsNullOrWhiteSpace($_) } |
    Select-Object -First 1
$projectFileVersion = $propertyGroups |
    ForEach-Object { $_.FileVersion } |
    Where-Object { ![string]::IsNullOrWhiteSpace($_) } |
    Select-Object -First 1
$projectInformationalVersion = $propertyGroups |
    ForEach-Object { $_.InformationalVersion } |
    Where-Object { ![string]::IsNullOrWhiteSpace($_) } |
    Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($projectVersion) -or
    [string]::IsNullOrWhiteSpace($projectAssemblyVersion) -or
    [string]::IsNullOrWhiteSpace($projectFileVersion) -or
    [string]::IsNullOrWhiteSpace($projectInformationalVersion)) {
    Stop-Publish "Version metadata is incomplete in GoblinFarmer.csproj."
}

if (![string]::IsNullOrWhiteSpace($Version) -and $Version -ne $projectVersion) {
    Stop-Publish "The project version is $projectVersion, but -Version $Version was supplied. Update GoblinFarmer.csproj instead of overriding release metadata."
}

$resolvedRepo = [System.IO.Path]::GetFullPath($repoRoot)
$resolvedPublishRoot = [System.IO.Path]::GetFullPath($publishRoot)
$resolvedPublishDir = [System.IO.Path]::GetFullPath($publishDir)
if (!$resolvedPublishDir.StartsWith($resolvedPublishRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    Stop-Publish "Publish directory resolved outside the artifacts publish root: $resolvedPublishDir"
}

Write-Host "GoblinFarmer release publish"
Write-Host "Repository: $resolvedRepo"
Write-Host "Version:    $projectVersion"
Write-Host "File ver:   $projectFileVersion"
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
        -p:IncludeNativeLibrariesForSelfExtract=true
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

$exeVersionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exePath)
if ($exeVersionInfo.FileVersion -ne $projectFileVersion) {
    Stop-Publish "Published executable FileVersion is $($exeVersionInfo.FileVersion), expected $projectFileVersion."
}

if ($exeVersionInfo.ProductVersion -ne $projectInformationalVersion) {
    Stop-Publish "Published executable ProductVersion is $($exeVersionInfo.ProductVersion), expected $projectInformationalVersion."
}

$isccPath = Get-InnoSetupCompiler
if (!$SkipInstaller -and $null -ne $isccPath -and (Test-Path $installerScript)) {
    Invoke-Step "compile Inno Setup installer" {
        $sourceDirDefine = "/DSourceDir=`"$publishDir`""
        & $isccPath $sourceDirDefine $installerScript
    }
}
elseif (!$SkipInstaller -and $null -eq $isccPath) {
    Write-Warning "Inno Setup compiler (ISCC.exe) was not found. The self-contained publish folder is ready; install Inno Setup to build the installer."
    Write-Host "After installing Inno Setup, compile with:"
    Write-Host "ISCC.exe `"/DSourceDir=$publishDir`" `"$installerScript`""
}
elseif (!$SkipInstaller -and !(Test-Path $installerScript)) {
    Write-Warning "Installer script not found: $installerScript"
}

Write-Host ""
Write-Host "========== Release Publish Summary =========="
Write-Host "Publish folder: $publishDir"
Write-Host "Executable:     $exePath"
Write-Host "EXE version:    $($exeVersionInfo.ProductVersion)"
Write-Host "EXE file ver:   $($exeVersionInfo.FileVersion)"
Write-Host "Config:         $configPath"
Write-Host "Images:         $imagesPath"
Write-Host "Installer out:  $installerOutput"
Write-Host "Install target: %LOCALAPPDATA%\Programs\GoblinFarmer"
Write-Host ""
Write-Host "Users can run GoblinFarmer.exe from the publish folder or install with the generated Inno Setup .exe."
