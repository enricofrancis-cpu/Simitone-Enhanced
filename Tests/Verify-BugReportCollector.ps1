$ErrorActionPreference = "Stop"
$root = Join-Path ([IO.Path]::GetTempPath()) ("Simitone-Collector-Test-" + [Guid]::NewGuid().ToString("N"))
try {
    $profile = Join-Path $root "TestProfile"
    $session = Join-Path $profile "Diagnostics/session-20261106-synthetic"
    New-Item -ItemType Directory -Path $session -Force | Out-Null
    Set-Content "$session/.simitone-diagnostics" "1"
    Set-Content "$session/session.log" "synthetic log"
    Set-Content "$session/personal.iff" "must not collect"
    Set-Content "$profile/superstar-preview.log" "synthetic fame"
    New-Item -ItemType Directory -Path "$profile/UserData" | Out-Null
    Set-Content "$profile/UserData/Neighborhood.iff" "important synthetic save"
    & "$PSScriptRoot/../packaging/superstar/Collect-Bug-Report.ps1" -ProfileDirectory $profile -OutputDirectory $root
    $zip = Get-ChildItem $root -Filter "*.zip" | Select-Object -First 1
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($zip.FullName)
    try {
        $entries = @($archive.Entries.FullName | ForEach-Object { $_.Replace('\','/') })
        if (!($entries -match 'session.log$') -or !($entries -contains 'superstar-preview.log')) { throw "Expected logs missing" }
        if ($entries -match '\.iff$|UserData') { throw "Collector included a save or non-log file" }
    } finally { $archive.Dispose() }
    if ((Get-Content "$profile/UserData/Neighborhood.iff") -ne "important synthetic save") { throw "Save changed" }
    Write-Host "PASS: collector includes expected logs and excludes saves and unrelated files."
} finally { if (Test-Path $root) { Remove-Item $root -Recurse -Force } }
