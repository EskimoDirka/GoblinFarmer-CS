param(
    [switch]$SkipBuild,
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot

function Invoke-DgaVerifyStep {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    Write-Host ""
    Write-Host "==> $Name"
    & $Action
}

Invoke-DgaVerifyStep "PowerShell parser validation" {
    $parseErrors = @()
    foreach ($script in Get-ChildItem -LiteralPath $scriptRoot -Filter "*.ps1" -File -ErrorAction SilentlyContinue) {
        [System.Management.Automation.Language.Token[]]$tokens = @()
        [System.Management.Automation.Language.ParseError[]]$errors = @()
        [System.Management.Automation.Language.Parser]::ParseFile($script.FullName, [ref]$tokens, [ref]$errors) | Out-Null
        if ($errors.Count -gt 0) {
            $parseErrors += $errors | ForEach-Object { "$($script.Name): $($_.Message)" }
        }
    }

    if ($parseErrors.Count -gt 0) {
        throw ($parseErrors -join [Environment]::NewLine)
    }

    Write-Host "PowerShell scripts parsed successfully."
}

Invoke-DgaVerifyStep "Debug workflow source sweep" {
    $retiredToken = "Goblin" + "Replay"
    $activeFiles = @(
        "Program.cs",
        "DebugManager.cs",
        "frmMain.GoblinEvidence.cs",
        "Scripts\create-debug-package.ps1"
    )

    foreach ($relative in $activeFiles) {
        $path = Join-Path $repoRoot $relative
        if ((Test-Path -LiteralPath $path -PathType Leaf) -and
            (Get-Content -LiteralPath $path -Raw).IndexOf($retiredToken, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "Retired replay token found in active workflow file: $relative"
        }
    }

    Write-Host "Active workflow files do not reference the retired replay engine."
}

Invoke-DgaVerifyStep "git diff whitespace check" {
    Push-Location $repoRoot
    try {
        & git diff --check
        if ($LASTEXITCODE -ne 0) {
            throw "git diff --check failed"
        }
    }
    finally {
        Pop-Location
    }
}

if (-not $SkipBuild) {
    Invoke-DgaVerifyStep "dotnet build" {
        Push-Location $repoRoot
        try {
            & dotnet build GoblinFarmer.csproj -p:UseSharedCompilation=false
            if ($LASTEXITCODE -ne 0) {
                throw "dotnet build failed"
            }
        }
        finally {
            Pop-Location
        }
    }
}

if (-not $SkipTests) {
    Invoke-DgaVerifyStep "test project" {
        Push-Location $repoRoot
        try {
            & dotnet run --project Tests\GoblinFarmer.Tests\GoblinFarmer.Tests.csproj -p:UseSharedCompilation=false
            if ($LASTEXITCODE -ne 0) {
                throw "test project failed"
            }
        }
        finally {
            Pop-Location
        }
    }
}

Write-Host ""
Write-Host "Development verification completed successfully."
