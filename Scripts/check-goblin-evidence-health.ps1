param(
    [string]$PackagePath = "",
    [string]$RootPath = "",
    [string]$OutputPath = "",
    [switch]$FailOnWarnings
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
. (Join-Path $scriptRoot "debug-analysis-tools.ps1")

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRoot "Debug\PackageAnalysis\Latest\goblin-evidence-health.txt"
}

$expandedRoot = $null
try {
    $analysisRoot = $RootPath
    if ([string]::IsNullOrWhiteSpace($analysisRoot)) {
        if ([string]::IsNullOrWhiteSpace($PackagePath)) {
            $latest = Get-DgaLatestDebugPackage (Join-Path $repoRoot "DebugPackages")
            if ($null -eq $latest) {
                throw "No package path supplied and no GoblinFarmer_Debug_*.zip package found under DebugPackages."
            }

            $PackagePath = $latest.FullName
        }

        $expandedRoot = Expand-DgaPackageForAnalysis $PackagePath
        $analysisRoot = $expandedRoot
    }

    $outputDirectory = Split-Path -Parent $OutputPath
    if (-not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    }

    $content = @(New-DgaGoblinEvidenceHealthContent -Root $analysisRoot)
    $content | Out-File -FilePath $OutputPath -Encoding utf8
    $warnings = @($content | Where-Object { $_.StartsWith("WARN:", [System.StringComparison]::OrdinalIgnoreCase) })

    Write-Host "Goblin Evidence health written: $OutputPath"
    Write-Host "Warnings: $($warnings.Count)"
    if ($FailOnWarnings -and $warnings.Count -gt 0) {
        exit 1
    }
}
finally {
    if (-not [string]::IsNullOrWhiteSpace($expandedRoot) -and (Test-Path -LiteralPath $expandedRoot)) {
        Remove-Item -LiteralPath $expandedRoot -Recurse -Force
    }
}
