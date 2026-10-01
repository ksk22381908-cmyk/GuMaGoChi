$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
$releaseRoot = Join-Path $PSScriptRoot 'release'
$packagePath = Join-Path $releaseRoot 'GuMaGoChi-0.1.9-windows'
New-Item -ItemType Directory -Path (Join-Path $packagePath 'assets/higgsfield') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin/GuMaGoChi.exe') -Destination $packagePath -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'GuMaGoChi.exe.config') -Destination $packagePath -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/characters') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/home') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/baby') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/adults-v2') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/adult-actions') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '사용안내.txt') -Destination $packagePath -Force
$archivePath = Join-Path $releaseRoot 'GuMaGoChi-0.1.9-windows.zip'
Compress-Archive -Path $packagePath -DestinationPath $archivePath -Force
Get-FileHash -LiteralPath $archivePath -Algorithm SHA256 | Format-List
