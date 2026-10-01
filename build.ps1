$ErrorActionPreference = 'Stop'
$projectPath = $PSScriptRoot
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path $compilerPath)) { $compilerPath = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe' }
if (-not (Test-Path $compilerPath)) { throw '.NET Framework 4.x compiler is required.' }
$outputPath = Join-Path $projectPath 'bin'
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$sourceFiles = Get-ChildItem (Join-Path $projectPath 'src') -Filter '*.cs' | ForEach-Object FullName
& $compilerPath /nologo /target:winexe /platform:anycpu /optimize+ /utf8output "/out:$outputPath/GuMaGoChi.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll $sourceFiles
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Copy-Item -LiteralPath (Join-Path $projectPath 'assets') -Destination $outputPath -Recurse -Force
Copy-Item -LiteralPath (Join-Path $projectPath 'GuMaGoChi.exe.config') -Destination $outputPath -Force
Copy-Item -LiteralPath (Join-Path $outputPath 'GuMaGoChi.exe') -Destination (Join-Path $projectPath 'GuMaGoChi.exe') -Force
Write-Output "Built: $outputPath/GuMaGoChi.exe"
