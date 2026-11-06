param(
    [string]$ProfileDirectory = (Join-Path $PSScriptRoot "TestProfile"),
    [string]$OutputDirectory = $PSScriptRoot
)
$ErrorActionPreference = "Stop"
$staging = Join-Path ([IO.Path]::GetTempPath()) ("Simitone-Report-" + [Guid]::NewGuid().ToString("N"))
try {
    New-Item -ItemType Directory -Path $staging | Out-Null
    $diagnostics = Join-Path $ProfileDirectory "Diagnostics"
    if (Test-Path $diagnostics) {
        $sessions = Get-ChildItem $diagnostics -Directory -Filter "session-*" |
            Where-Object { !($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -and (Test-Path (Join-Path $_.FullName ".simitone-diagnostics")) } |
            Sort-Object Name -Descending | Select-Object -First 5
        foreach ($session in $sessions) {
            $target = Join-Path $staging $session.Name
            New-Item -ItemType Directory -Path $target | Out-Null
            foreach ($name in @("session.log", "errors.log", "crash.txt")) {
                $source = Join-Path $session.FullName $name
                if ((Test-Path $source -PathType Leaf) -and !((Get-Item $source).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                    Copy-Item $source $target
                }
            }
        }
    }
    foreach ($name in @("superstar-preview.log", "Bug-Report.txt")) {
        $source = Join-Path $ProfileDirectory $name
        if ((Test-Path $source -PathType Leaf) -and !((Get-Item $source).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            Copy-Item $source $staging
        }
    }
    Set-Content (Join-Path $staging "READ-ME.txt") @"
Simitone Enhanced bug report. Logs only; saves and original game assets are excluded.
Please include: exact steps, expected/actual result, lot/household/gig, whether it repeats,
and whether Superstar Test overrides were used. Attach screenshots/video separately.
Review these logs before sharing: exception text and the existing Superstar log can contain
names or paths. Common paths in the new diagnostic logs are redacted, not guaranteed anonymous.
Absence of a crash report does not prove a clean exit (native crashes/power loss may bypass logging).
"@
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    $archive = Join-Path $OutputDirectory ("Simitone-Bug-Report-" + (Get-Date -Format "yyyyMMdd-HHmmss") + "-" + [Guid]::NewGuid().ToString("N").Substring(0,8) + ".zip")
    Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $archive
    Write-Host "Created $archive"
    Write-Host "Send this ZIP with your screenshots and steps. Your saves were not included."
} finally {
    if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
}
