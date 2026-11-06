param(
    [string]$PublishDirectory = "publish/gameplay-win",
    [string]$OutputDirectory = "release/Gameplay-Safety-Preview-Windows"
)
$ErrorActionPreference = "Stop"
$lib = Join-Path $OutputDirectory "lib"
New-Item -ItemType Directory -Path $lib -Force | Out-Null
Copy-Item "$PublishDirectory/*" $lib -Recurse -Force

# The client assets must win over generic FreeSO assets with identical names.
Copy-Item "Client/Simitone/Simitone.Client/Content/*" "$lib/Content" -Recurse -Force
$build = "Client/Simitone/Simitone.Windows/bin/Release/net9.0-windows/win-x64"
if (!(Test-Path "$build/Monogame")) { throw "Built MonoGame backend directories are missing." }
Copy-Item "$build/Monogame" $lib -Recurse -Force
Remove-Item "$lib/MonoGame.Framework.dll" -ErrorAction SilentlyContinue
Set-Content "$lib/superstar-preview.marker" "Experimental isolated Superstar preview" -Encoding utf8

foreach ($unused in @("Content/Objects", "Content/Patch", "Content/Avatar", "Content/MeshReplace",
    "Content/iOS", "Content/Cities", "Content/Blueprints", "DatabaseScripts", "MailTemplates", "IDERes")) {
    $path = Join-Path $lib $unused
    if (Test-Path $path) { Remove-Item $path -Recurse -Force }
}
if (Test-Path "$lib/runtimes") {
    Get-ChildItem "$lib/runtimes" -Directory | Where-Object { $_.Name -notin @("win", "win-x64") } |
        ForEach-Object { Remove-Item $_.FullName -Recurse -Force }
}
Copy-Item "packaging/superstar/Start-Gameplay-Preview.cmd" $OutputDirectory
Copy-Item "packaging/superstar/README.txt" $OutputDirectory
Copy-Item "development/Superstar-Preview.md" $OutputDirectory
Copy-Item "development/Gameplay-Test-Preview.md" $OutputDirectory
Copy-Item "development/Safety-Preview.md" $OutputDirectory
Copy-Item "FreeSO/LICENSE.md" "$OutputDirectory/FreeSO-LICENSE.md"
Copy-Item "FreeSO/Other/libs/FSOMonoGame/LICENSE.txt" "$OutputDirectory/MonoGame-LICENSE.txt"
foreach ($required in @("Simitone.exe", "Simitone.dll", "Simitone.runtimeconfig.json", "coreclr.dll",
    "hostfxr.dll", "Monogame/Windows/MonoGame.Framework.dll", "Monogame/WindowsGL/MonoGame.Framework.dll",
    "Content/uigraphics/live/rel_fame.png", "superstar-preview.marker")) {
    if (!(Test-Path (Join-Path $lib $required))) { throw "Preview bundle is missing $required" }
}
if (Test-Path "$OutputDirectory/TestProfile") { throw "Do not package personal test saves." }
if (Test-Path "$lib/GameData") { throw "Do not package original game assets." }
Write-Host "Self-contained gameplay preview prepared at $OutputDirectory"
