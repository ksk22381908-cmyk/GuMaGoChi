param([Parameter(Mandatory=$true)][string]$ExecutablePath)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $ExecutablePath -PathType Leaf)) { throw 'Executable is missing.' }
$signature = Get-AuthenticodeSignature -LiteralPath $ExecutablePath
if ($signature.Status -ne 'Valid' -or -not $signature.SignerCertificate) {
    throw "A valid trusted Authenticode signature is required: $($signature.Status)."
}
if (-not $signature.TimeStamperCertificate) { throw 'A trusted signing timestamp is required.' }
Write-Output "Verified signed executable: $ExecutablePath"
Write-Output "Publisher: $($signature.SignerCertificate.Subject)"
Write-Output "Certificate thumbprint: $($signature.SignerCertificate.Thumbprint)"
