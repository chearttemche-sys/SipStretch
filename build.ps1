$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Install .NET Framework 4.8 to build Sip & Stretch.' }
$output = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$exe = Join-Path $output 'SipStretch.exe'
$icon = Join-Path $PSScriptRoot 'app\SipStretch.ico'
$manifest = Join-Path $PSScriptRoot 'app.manifest'
$sources = (Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'app') -Filter '*.cs').FullName
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 /r:System.Drawing.dll /r:System.Windows.Forms.dll "/win32icon:$icon" "/win32manifest:$manifest" "/out:$exe" $sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Output $exe
