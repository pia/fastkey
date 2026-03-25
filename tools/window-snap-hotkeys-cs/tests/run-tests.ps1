$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$distDir = Join-Path $scriptDir 'dist'
$output = Join-Path $distDir 'HotkeyStateMachineTests.exe'
$testsSource = Join-Path $scriptDir 'HotkeyStateMachineTests.cs'
$logicSource = Join-Path $projectDir 'HotkeyLogic.cs'

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

$sources = @($testsSource)
if (Test-Path $logicSource) {
  $sources += $logicSource
}

& $cscPath /nologo /target:exe /out:$output $sources

if ($LASTEXITCODE -ne 0) {
  throw "Test build failed with exit code $LASTEXITCODE"
}

& $output

if ($LASTEXITCODE -ne 0) {
  throw "Tests failed with exit code $LASTEXITCODE"
}

Write-Host "Tests passed: $output"
