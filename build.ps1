param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

Write-Host "==> Building StarCitizenLibrary ($Configuration)..." -ForegroundColor Cyan
dotnet build (Join-Path $root 'StarCitizenLibrary.csproj') -c $Configuration -m:4
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

$outDir = Join-Path $root "bin\\$Configuration\\net462"
$toolbox = "$env:LOCALAPPDATA\\Playnite\\Toolbox.exe"
if (Test-Path $toolbox) {
    Write-Host "==> Packaging extension (.pext)..." -ForegroundColor Cyan
    & $toolbox pack $outDir $root
    if ($LASTEXITCODE -ne 0) { throw "Packaging failed." }

    # Rename to clean versioned name (e.g. StarCitizenLibrary_v0.1.0.pext)
    $version = if ((Get-Content (Join-Path $outDir 'extension.yaml') -Raw) -match '(?m)^Version:\s*([^\r\n]+)') { $Matches[1].Trim() } else { "0.1.0" }
    $rawPext = Get-ChildItem -Path $root -Filter "*.pext" | Where-Object { $_.Name -like "*StarCitizenLibrary*" } | Select-Object -First 1
    if ($rawPext) {
        $cleanName = "StarCitizenLibrary_v${version}.pext"
        Rename-Item $rawPext.FullName -NewName $cleanName -Force
        Write-Host "==> Successfully packaged $cleanName in $root" -ForegroundColor Green
    }
} else {
    Write-Host "Toolbox.exe not found at $toolbox; skipping packaging." -ForegroundColor Yellow
}
