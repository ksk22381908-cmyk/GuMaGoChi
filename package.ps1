param([switch]$SkipBuild,[string]$ExecutablePath)
$ErrorActionPreference = 'Stop'
if(-not $SkipBuild){& (Join-Path $PSScriptRoot 'build.ps1')}
$releaseRoot = Join-Path $PSScriptRoot 'release'
$packagePath = Join-Path $releaseRoot 'GuMaGoChi-0.2.5-windows'
New-Item -ItemType Directory -Path (Join-Path $packagePath 'assets/higgsfield') -Force | Out-Null
if(-not $ExecutablePath){$ExecutablePath=Join-Path $PSScriptRoot 'GuMaGoChi.exe'}
Copy-Item -LiteralPath $ExecutablePath -Destination $packagePath -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'GuMaGoChi.exe.config') -Destination $packagePath -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/characters') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/home') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/baby') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/adults-v2') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/adult-actions') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/sleep') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/gancheopma') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/evolutions') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/defense/고구마버러지디펜스Intro.png') -Destination (New-Item -ItemType Directory -Path (Join-Path $packagePath 'assets/higgsfield/defense') -Force).FullName -Force
foreach($assetKind in @('basic-attacks','skills','enemies')) {
    $assetSource = Join-Path $PSScriptRoot "assets/higgsfield/defense/$assetKind"
    $assetTarget = Join-Path $packagePath "assets/higgsfield/defense/$assetKind"
    New-Item -ItemType Directory -Path $assetTarget -Force | Out-Null
    Get-ChildItem -LiteralPath $assetSource -Directory | Where-Object Name -Match '^\d{2}$' | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination $assetTarget -Recurse -Force}
    Copy-Item -LiteralPath (Join-Path $assetSource 'manifest.json') -Destination $assetTarget -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '사용안내.txt') -Destination $packagePath -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs/디펜스-구현-사용안내.md') -Destination $packagePath -Force
$archivePath = Join-Path $releaseRoot 'GuMaGoChi-0.2.5-windows.zip'
Compress-Archive -Path $packagePath -DestinationPath $archivePath -Force
$hash = Get-FileHash -LiteralPath $archivePath -Algorithm SHA256
[IO.File]::WriteAllText($archivePath + '.sha256', $hash.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($archivePath) + "`n", [Text.Encoding]::ASCII)
$hash | Format-List
