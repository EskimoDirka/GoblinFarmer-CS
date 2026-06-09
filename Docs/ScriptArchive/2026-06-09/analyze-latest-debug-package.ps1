param(
    [string]$PackagePath = "",
    [string]$DebugPackagesRoot = "",
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
. (Join-Path $scriptRoot "debug-analysis-tools.ps1")

if ([string]::IsNullOrWhiteSpace($DebugPackagesRoot)) {
    $DebugPackagesRoot = Join-Path $repoRoot "DebugPackages"
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "Debug\PackageAnalysis\Latest"
}

if ([string]::IsNullOrWhiteSpace($PackagePath)) {
    $latest = Get-DgaLatestDebugPackage $DebugPackagesRoot
    if ($null -eq $latest) {
        throw "No GoblinFarmer_Debug_*.zip package found under $DebugPackagesRoot"
    }

    $PackagePath = $latest.FullName
}

$expandedRoot = $null
try {
    $expandedRoot = Expand-DgaPackageForAnalysis $PackagePath
    $result = Write-DgaAnalysisFiles -Root $expandedRoot -OutputDirectory $OutputDirectory

    Write-Host "Analyzed package: $PackagePath"
    Write-Host "Analysis:         $($result.AnalysisPath)"
    Write-Host "Timeline:         $($result.TimelinePath)"
    Write-Host "Evidence health:  $($result.HealthPath)"
}
finally {
    if (-not [string]::IsNullOrWhiteSpace($expandedRoot) -and (Test-Path -LiteralPath $expandedRoot)) {
        Remove-Item -LiteralPath $expandedRoot -Recurse -Force
    }
}
