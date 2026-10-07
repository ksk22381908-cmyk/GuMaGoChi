param([string]$AssetRoot=(Join-Path $PSScriptRoot 'assets'))
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')+'\'
$assetPath=[IO.Path]::GetFullPath($AssetRoot).TrimEnd('\')
if(-not ($assetPath+'\').StartsWith($projectRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Asset path must stay within the project.'}
$removedFiles=0;$removedBytes=0L
foreach($id in @(0,2,4,7,19,20,21,27,28)){
    $number=$id.ToString('00')
    $relativePaths=@("higgsfield/characters/$number.png","higgsfield/adults-v2/$number.png","higgsfield/adult-actions/$number-sheet.png","higgsfield/sleep/$number-sheet.png","higgsfield/defense/basic-attacks/$number","higgsfield/defense/skills/$number")
    foreach($relativePath in $relativePaths){
        $target=[IO.Path]::GetFullPath((Join-Path $assetPath $relativePath))
        if(-not $target.StartsWith($assetPath+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Asset deletion must stay within the asset directory.'}
        if(Test-Path -LiteralPath $target){
            $item=Get-Item -LiteralPath $target
            if($item.PSIsContainer){$files=@(Get-ChildItem -LiteralPath $target -Recurse -File)}else{$files=@($item)}
            $removedFiles+=$files.Count;foreach($file in $files){$removedBytes+=$file.Length}
            Remove-Item -LiteralPath $target -Recurse -Force
        }
    }
}
foreach($relativePath in @('higgsfield/baby/baby-reference.png','higgsfield/baby/walk-sheet.png','higgsfield/baby/eat-sheet.png','higgsfield/baby/throw-sheet.png','higgsfield/baby/burrow-sheet.png','higgsfield/sleep/baby-sheet.png')){
    $target=[IO.Path]::GetFullPath((Join-Path $assetPath $relativePath))
    if(-not $target.StartsWith($assetPath+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Asset deletion must stay within the asset directory.'}
    if(Test-Path -LiteralPath $target){$file=Get-Item -LiteralPath $target;$removedFiles++;$removedBytes+=$file.Length;Remove-Item -LiteralPath $target -Force}
}
Write-Output "Removed retired runtime assets: $removedFiles files, $removedBytes bytes"
