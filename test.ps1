param([switch]$SkipRootCopy)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1') -SkipRootCopy:$SkipRootCopy
# Windows PowerShell hosts the same Framework assembly. This also works in
# environments that delay or restrict launching freshly compiled executables.
$assemblyPath = Join-Path $PSScriptRoot 'bin/GuMaGoChi.exe'
$frameworkTestScript = @'
param([string]$assemblyPath)
$ErrorActionPreference = 'Stop'
$assembly = [Reflection.Assembly]::LoadFrom($assemblyPath)
foreach ($typeName in @('GuMaGoChi.Tests', 'GuMaGoChi.UiCheck')) {
    $testType = $assembly.GetType($typeName)
    $result = $testType.GetMethod('Run', [Reflection.BindingFlags]'Public,Static').Invoke($null, @())
    Write-Output $result
}
$assembly.GetType('GuMaGoChi.RenderCheck').GetMethod('Run', [Reflection.BindingFlags]'Public,Static').Invoke($null, @())
'@
$runnerPath = Join-Path $PSScriptRoot 'bin/framework-test.ps1'
Set-Content -LiteralPath $runnerPath -Value $frameworkTestScript -Encoding utf8
& powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $runnerPath $assemblyPath
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
