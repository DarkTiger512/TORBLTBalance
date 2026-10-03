param(
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = "Stop"
$build = Join-Path $PSScriptRoot "build"
if (-not (Test-Path (Join-Path $build "SubModule.xml"))) {
    throw "No build found. Run .\build.ps1 first."
}

$out = Join-Path $PSScriptRoot "artifacts"
New-Item -ItemType Directory -Force -Path $out | Out-Null

$stageRoot = Join-Path $out "stage"
$moduleRoot = Join-Path $stageRoot "TORBLTBalance"
if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $moduleRoot | Out-Null
Copy-Item "$build\*" $moduleRoot -Recurse -Force

$zip = Join-Path $out "TORBLTBalance-v$Version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $moduleRoot -DestinationPath $zip

Remove-Item $stageRoot -Recurse -Force
Write-Host "Created $zip"
