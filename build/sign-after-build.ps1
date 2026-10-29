param([string]$TargetPath, [string]$ProjectDir)

$certThumbprint = "C48F68359A1A33125EB0ED4DA91353C7F52C9C9D"
$signtool = "C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe"
if (-not (Test-Path $signtool)) {
    $signtool = "C:\Program Files\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\signtool.exe"
}
if (-not (Test-Path $signtool)) {
    $signtool = "signtool.exe"
}

try {
    $cert = Get-Item "Cert:\CurrentUser\My\$certThumbprint" -ErrorAction Stop
    & $signtool sign /fd SHA256 /sha1 $certThumbprint /q $TargetPath 2>&1 | Out-Null
    Write-Output "[sign] Signed: $TargetPath"
} catch {
    Write-Output "[sign] Skip (no cert): $TargetPath"
}
