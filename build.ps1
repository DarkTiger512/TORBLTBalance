param(
    [ValidateSet("Debug","Release")]
    [string]$Configuration = "Release",
    [string]$BannerlordGameDir = $env:BANNERLORD_GAME_DIR
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($BannerlordGameDir)) {
    $BannerlordGameDir = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord"
}

if (-not (Test-Path $BannerlordGameDir)) {
    throw "Bannerlord directory not found: $BannerlordGameDir. Pass -BannerlordGameDir or set BANNERLORD_GAME_DIR."
}

$required = @(
    "$BannerlordGameDir\Modules\Bannerlord.Harmony\bin\Win64_Shipping_Client\0Harmony.dll",
    "$BannerlordGameDir\Modules\BannerlordTwitch\bin\Win64_Shipping_Client\BannerlordTwitch.dll",
    "$BannerlordGameDir\Modules\BLTAdoptAHero\bin\Win64_Shipping_Client\BLTAdoptAHero.dll",
    "$BannerlordGameDir\Modules\TOR_Core\bin\Win64_Shipping_Client\TOR_Core.dll"
)

foreach ($path in $required) {
    if (-not (Test-Path $path)) { throw "Missing dependency: $path" }
}

& msbuild .\TORBLTBalance.csproj /m /p:Configuration=$Configuration /p:BANNERLORD_GAME_DIR="$BannerlordGameDir"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Built module at .\build"
