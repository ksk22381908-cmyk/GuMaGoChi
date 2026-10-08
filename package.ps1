param([switch]$SkipBuild,[switch]$RequireSignature,[string]$ExecutablePath,[string]$PreviousPackagePath)
$ErrorActionPreference = 'Stop'
if(-not $SkipBuild){& (Join-Path $PSScriptRoot 'build.ps1')}
$releaseRoot = Join-Path $PSScriptRoot 'release'
$packagePath = Join-Path $releaseRoot 'GuMaGoChi-0.2.12-windows'
New-Item -ItemType Directory -Path (Join-Path $packagePath 'assets/higgsfield') -Force | Out-Null
if(-not $ExecutablePath){$ExecutablePath=Join-Path $PSScriptRoot 'GuMaGoChi.exe'}
if($RequireSignature){& (Join-Path $PSScriptRoot 'verify-signature.ps1') -ExecutablePath $ExecutablePath}
Copy-Item -LiteralPath $ExecutablePath -Destination $packagePath -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'GuMaGoChi.exe.config') -Destination $packagePath -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/characters') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/home') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/baby') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/adults-v2') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/adult-actions') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/sleep') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/gancheopma') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/berry') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/carrot') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/evolutions') -Destination (Join-Path $packagePath 'assets/higgsfield') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/higgsfield/defense/고구마버러지디펜스Intro.png') -Destination (New-Item -ItemType Directory -Path (Join-Path $packagePath 'assets/higgsfield/defense') -Force).FullName -Force
foreach($assetKind in @('basic-attacks','skills','enemies')) {
    $assetSource = Join-Path $PSScriptRoot "assets/higgsfield/defense/$assetKind"
    $assetTarget = Join-Path $packagePath "assets/higgsfield/defense/$assetKind"
    New-Item -ItemType Directory -Path $assetTarget -Force | Out-Null
    Get-ChildItem -LiteralPath $assetSource -Directory | Where-Object Name -Match '^\d{2}$' | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination $assetTarget -Recurse -Force}
    Copy-Item -LiteralPath (Join-Path $assetSource 'manifest.json') -Destination $assetTarget -Force
}
& (Join-Path $PSScriptRoot 'prune-retired-assets.ps1') -AssetRoot (Join-Path $packagePath 'assets')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '사용안내.txt') -Destination $packagePath -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs/디펜스-구현-사용안내.md') -Destination $packagePath -Force
$archivePath = Join-Path $releaseRoot 'GuMaGoChi-0.2.12-windows.zip'
Compress-Archive -Path $packagePath -DestinationPath $archivePath -Force
$hash = Get-FileHash -LiteralPath $archivePath -Algorithm SHA256
[IO.File]::WriteAllText($archivePath + '.sha256', $hash.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($archivePath) + "`n", [Text.Encoding]::ASCII)
$hash | Format-List

if(-not $PreviousPackagePath){$PreviousPackagePath=Join-Path $releaseRoot 'GuMaGoChi-0.2.11-windows'}
if(Test-Path -LiteralPath (Join-Path $PreviousPackagePath 'GuMaGoChi.exe')){
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $version=[Reflection.AssemblyName]::GetAssemblyName((Join-Path $packagePath 'GuMaGoChi.exe')).Version.ToString(3)
    $baseline=[Reflection.AssemblyName]::GetAssemblyName((Join-Path $PreviousPackagePath 'GuMaGoChi.exe')).Version.ToString(3)
    if([Version]$baseline -ge [Version]$version){throw 'Patch baseline must be an older version.'}
    $deltaPath=Join-Path $releaseRoot "GuMaGoChi-$version-delta.zip"
    $required=@();$changed=0
    $stream=[IO.File]::Open($deltaPath,[IO.FileMode]::Create)
    $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create)
    try{
        foreach($file in Get-ChildItem -LiteralPath $packagePath -Recurse -File){
            $relative=$file.FullName.Substring($packagePath.Length).TrimStart('\').Replace('\','/')
            if($relative -ne 'GuMaGoChi.exe' -and $relative -ne 'GuMaGoChi.exe.config' -and -not $relative.StartsWith('assets/')){continue}
            $newHash=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            $old=Join-Path $PreviousPackagePath $relative
            if($relative -ne 'GuMaGoChi.exe' -and (Test-Path -LiteralPath $old -PathType Leaf) -and (Get-Item -LiteralPath $old).Length -eq $file.Length -and (Get-FileHash -LiteralPath $old -Algorithm SHA256).Hash.ToLowerInvariant() -eq $newHash){
                $required+=@{path=$relative;size=$file.Length;sha256=$newHash}
            }else{
                [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$file.FullName,"GuMaGoChi-$version-windows/$relative",[IO.Compression.CompressionLevel]::Optimal)|Out-Null
                $changed++
            }
        }
    }finally{$zip.Dispose();$stream.Dispose()}
    $manifest=@{format=1;version=$version;baseVersion=$baseline;required=@($required)}|ConvertTo-Json -Depth 5 -Compress
    [IO.File]::WriteAllText((Join-Path $releaseRoot "GuMaGoChi-$version-delta.json"),$manifest,[Text.UTF8Encoding]::new($false))
    $deltaHash=(Get-FileHash -LiteralPath $deltaPath -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText($deltaPath+'.sha256',$deltaHash+'  '+[IO.Path]::GetFileName($deltaPath)+"`n",[Text.Encoding]::ASCII)
    Write-Output "Patch: $changed changed files, $((Get-Item $deltaPath).Length) bytes (baseline $baseline)"
}else{Write-Warning 'Previous package not found; only full update was generated.'}
