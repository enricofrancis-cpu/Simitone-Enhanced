# Reporting bugs from the diagnostics preview

Extract the entire diagnostics preview into a new writable folder outside the original installation and run `Start-Gameplay-Preview.cmd` in Windows/Parallels. The game uses the adjacent `TestProfile`. This candidate includes the earlier gameplay and safety preview; no additional inventory, CAS empty-list or discovery fixes are claimed here.

## What to send

After the problem occurs, close the game normally if possible, then double-click `Collect-Bug-Report.cmd`. It creates a uniquely named `Simitone-Bug-Report-*.zip` beside the launcher. Send that ZIP here with:

- What you clicked/did immediately before the problem, the household/lot and object or gig.
- What you expected and what actually happened; whether repeating the steps reproduces it.
- A screenshot of the full error dialog. Short video is useful for routing/animation/silent failures; no full-session recording is needed.
- Whether you used Superstar Test overrides. For persistence problems, describe the values before saving and after restarting.

You can put those notes in `TestProfile/Bug-Report.txt` before collecting; the collector includes it. Screenshots/video are attached separately.

The ZIP includes only allowlisted diagnostic files from the latest five marked sessions, the existing `superstar-preview.log`, and optional notes. It excludes neighbourhoods, saves, configuration files and original game assets. Nothing is uploaded automatically. Review it before sharing: common profile/user/installation paths in the new logger are redacted, but exception text and the existing Superstar log are not guaranteed anonymous.

## Files and meaning

- `TestProfile/Diagnostics/session-<UTC timestamp>-<id>/session.log`: source/build identifier, runtime/OS/architecture, content-load milestones and failed BCF files, lot-load/travel requests, save start/completion/failure and process exit.
- `errors.log` in the same session: captured SimAntics error dialogs, failed saves, unobserved task exceptions and managed fatal exceptions.
- `crash.txt` in that session: most recent managed fatal exception, when the handler runs.
- `TestProfile/superstar-preview.log`: existing fame rules/progression/overrides diagnostics, independent of the new logger.

No diagnostic file proves a whole-game save is consistent. A logged save completion means that the existing save method returned; it does not validate every game resource. No ordinary dialogue or every-click activity is recorded. SimAntics errors are captured when the engine emits its standard error dialog; suppressed errors without a dialog are not covered. Errors before profile logging starts, native/GPU crashes, hard termination and power loss may produce no crash report. The last event is still useful. Missing diagnostics must not prevent gameplay or replace an existing error.

Each new session log/error file is capped at 2 MiB and each message at 16,000 characters; only the latest five owned session folders are retained. Crash text is bounded. The existing Superstar log has its existing lifetime; the new logger does not rotate it.

Keep the affected disposable profile if a bug occurs. Send the log ZIP first; save files should only be provided later if needed to reproduce the issue. Do not send original installation files.

## Validation

`dotnet run --project Tests/Simitone.Diagnostics.Tests -c Release` checks production logger metadata/redaction, concurrent writes, error filtering, fatal output, size caps/retention, unavailable destinations and preservation of unrelated/save sentinel files, using synthetic data.

`pwsh -File Tests/Verify-BugReportCollector.ps1` executes the production collector against synthetic folders and inspects the ZIP to check logs are present and saves/unrelated IFF files are absent. Windows CI also builds the full client and runs all eight suites before publishing/validating this separate bundle. Actual in-game capture still needs testing.

**Return home before saving. Vacation checkpoints and eviction fixes remain outside this preview.**
