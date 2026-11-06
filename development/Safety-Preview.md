# Gameplay and safety preview — 6 November 2026

This is a separate unmerged candidate based on the combined gameplay preview (PR7), with three focused engine safety changes. Extract the entire archive into a **new writable folder outside the original installation**, keep `lib` intact, and run `Start-Gameplay-Preview.cmd` in Windows/Parallels. Use its fresh `TestProfile`; do not extract over the old preview. The earlier download remains available.

## Changes

1. Unrelated files in `Characters` no longer stop character discovery, and uppercase `.IFF` extensions load. New character files use an unused name and cannot replace an existing file. Failed serialization leaves no final partial character file.
2. Short BCF appearance names no longer crash clothing catalogue initialization. Generic appearances remain registered; existing valid clothing classification is retained. This does not repair malformed meshes or animations.
3. Initial neighbourhood copies are staged and published only after required resources have copied successfully. An existing profile is never merged or replaced. Incomplete existing profiles produce an error that preserves their data; use another fresh test folder rather than deleting important saves.

The pinned FreeSO commit and its checkout remain unchanged. Explicit reviewed provider copies are compiled through `Directory.Build.targets`; a source hash guard stops builds if the original providers change. No renderer changes were made.

## Verify in game

- Start with a fresh test folder and confirm initial neighbourhood import, family list and home loading work. Restart and confirm your test changes persist rather than being recopied from the installation.
- In the disposable `TestProfile` only, place a harmless text file alongside character IFFs, restart, and confirm all families remain available. Do not experiment on original saves.
- Create a family, save at home, restart and check the new family and pre-existing families. Test a second creation/deletion cycle. Automated checks exercise name collisions and failed writes; there is no need to force these failures manually.
- Load known custom outfits/heads, cycle skin tones and check both catalogue visibility and actual meshes/textures. Short-name tests use synthetic BCF data; real custom-content gameplay still needs verification.
- Follow `Gameplay-Test-Preview.md` and `Superstar-Preview.md` for naming, phone book, Superstar progression, weather and export checks.

## Validation and limits

The new suite invokes the actual compiled engine providers using synthetic character IFFs and decoded BCF files. It checks discovery, case handling, stale indices, collision retries, serializer failure, preserved original/existing files, failed import cleanup/retry and competing destination creation. Seven regression suites and Windows Release build/publish are required by CI; see the PR checks for results.

No gameplay testing has been performed for this candidate. Required-file validation is structural, not full corruption detection. Character publication does not make whole family creation or multi-file game saves transactional. Abrupt termination may leave an ignored staging folder. The preview still needs in-game testing on Parallels.

**Return home before saving. Vacation checkpoints and eviction fixes are not included.** Never copy experimental saves back into your normal game. Report the preview version, exact action, expected/actual behavior and full error text or screenshot.
