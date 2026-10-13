<#
  Builds the plugin in Release, copies it into Stream Deck's plugin folder and restarts Stream Deck.

    .\Tools\install-local.ps1
#>
$ErrorActionPreference = 'Stop'

$uuid   = 'com.nisemonox.scstreamdeck'
$root   = Split-Path $PSScriptRoot -Parent
$plugin = Join-Path $root "PluginCore\bin\Release\$uuid.sdPlugin"
$target = Join-Path $env:APPDATA "Elgato\StreamDeck\Plugins\$uuid.sdPlugin"

dotnet build (Join-Path $root 'PluginCore\PluginCore.csproj') -c Release -nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$app = Get-Process StreamDeck -ErrorAction SilentlyContinue | Select-Object -First 1
$appPath = if ($app) { $app.Path }

# Stream Deck only loads new plugins at startup, and a running plugin's exe cannot be replaced.
if ($app) {
    Stop-Process -Id $app.Id
    $app.WaitForExit(10000) | Out-Null
}
Get-Process $uuid -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

# Mirror, so files removed from the plugin disappear from the installed copy too. Logs next to the exe survive,
# and so does a "cache" folder left by versions before 1.3.1, which the plugin moves to %LOCALAPPDATA% itself.
robocopy $plugin $target /MIR /XD cache /XF *.log /NJH /NJS /NFL /NDL /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Copy failed (robocopy exit code $LASTEXITCODE)." }
$global:LASTEXITCODE = 0  # robocopy reports success as 1-7
"Installed to $target"

if ($appPath) {
    Start-Process $appPath
    'Restarted Stream Deck'
}
else {
    'Stream Deck is not running; start it to load the plugin.'
}
