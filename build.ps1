param(
    [ValidateSet("Debug","Release")]
    [string]$Configuration = "Release",
    [string]$BannerlordGameDir = $env:BANNERLORD_GAME_DIR,
    [string]$TorCoreDll = $env:TOR_CORE_DLL
)
$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($BannerlordGameDir)) {
    $BannerlordGameDir = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord"
}
if ([string]::IsNullOrWhiteSpace($TorCoreDll)) {
    $TorCoreDll = Join-Path $BannerlordGameDir 'Modules\TOR_Core\bin\Win64_Shipping_Client\TOR_Core.dll'
}
$required = @(
    "$BannerlordGameDir\Modules\Bannerlord.Harmony\bin\Win64_Shipping_Client\0Harmony.dll",
    "$BannerlordGameDir\Modules\BannerlordTwitch\bin\Win64_Shipping_Client\BannerlordTwitch.dll",
    "$BannerlordGameDir\Modules\BLTAdoptAHero\bin\Win64_Shipping_Client\BLTAdoptAHero.dll",
    $TorCoreDll
)
foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing dependency: $path" }
}
# Pin game APIs independently of the locally installed game version.
$referenceRoot = Join-Path $PSScriptRoot 'artifacts\references\Bannerlord-1.3.15'
$packagePath = Join-Path $referenceRoot 'core.zip'
$referenceDir = Join-Path $referenceRoot 'core\ref\net472'
New-Item -ItemType Directory -Force -Path $referenceRoot | Out-Null
if (-not (Test-Path -LiteralPath $packagePath)) {
    Invoke-WebRequest 'https://api.nuget.org/v3-flatcontainer/bannerlord.referenceassemblies.core/1.3.15.110062/bannerlord.referenceassemblies.core.1.3.15.110062.nupkg' -OutFile $packagePath
}
$expectedHash = '585A9A4A18F21D6B01E8E8977FE4270F647D531DE3CAF8BDF7232C8D93485E53'
if ((Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash -ne $expectedHash) {
    throw 'Bannerlord 1.3.15 reference package checksum mismatch.'
}
Expand-Archive -LiteralPath $packagePath -DestinationPath (Join-Path $referenceRoot 'core') -Force
$msbuildCommand = Get-Command msbuild -ErrorAction SilentlyContinue
if ($msbuildCommand) { $msbuildPath = $msbuildCommand.Source }
else {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path -LiteralPath $vswhere)) { throw 'MSBuild not found. Install Visual Studio Build Tools.' }
    $msbuildPath = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if (-not $msbuildPath) { throw 'MSBuild not found.' }
}
& $msbuildPath (Join-Path $PSScriptRoot 'TORBLTBalance.csproj') /t:Rebuild /m "/p:Configuration=$Configuration" "/p:BANNERLORD_GAME_DIR=$BannerlordGameDir" "/p:TOR_CORE_DLL=$TorCoreDll" "/p:BANNERLORD_REFERENCE_DIR=$referenceDir"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host 'Built module for Bannerlord 1.3.15 at .\build'
