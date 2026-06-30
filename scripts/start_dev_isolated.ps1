param(
    [string]$LaunchProfile = "https",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifactsPath = Join-Path $repoRoot ".artifacts\run"

Set-Location $repoRoot

if (-not $SkipBuild) {
    dotnet build --artifacts-path $artifactsPath
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

dotnet run --no-build --launch-profile $LaunchProfile --artifacts-path $artifactsPath
exit $LASTEXITCODE
