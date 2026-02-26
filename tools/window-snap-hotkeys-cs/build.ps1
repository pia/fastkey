$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$source = Join-Path $scriptDir 'Program.cs'
$distDir = Join-Path $scriptDir 'dist'
$output = Join-Path $distDir 'WindowSnapHotkeys.exe'

if (!(Test-Path $distDir)) {
  New-Item -ItemType Directory -Path $distDir | Out-Null
}

$cscPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $cscPath)) {
  $cscPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}

if (!(Test-Path $cscPath)) {
  throw 'Cannot find csc.exe on this machine.'
}

& $cscPath /nologo /target:winexe /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:$output $source

if ($LASTEXITCODE -ne 0) {
  throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host "Build complete: $output"
