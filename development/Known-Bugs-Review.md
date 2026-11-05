# Bug review — 5 November 2026

Baseline: enhanced-development at 03b3f03; FreeSO d8a3c175. Public reports are leads, not proof a defect remains in this revision. Older FreeSO forum reports often describe a different platform, release or the online game.

## Verified in source; addressed by this branch

| Problem | Evidence | Change |
| --- | --- | --- |
| Cannot type a pet name | [Issue 26](https://github.com/alexjyong/Simitone/issues/26), [sithtoast's proposed fix 42](https://github.com/alexjyong/Simitone/pull/42); UIMobileAlert places input in button area and uses missing TSO background | Move input above all button rows, shared white background with dark text, initial focus, Enter follows first button; retain normal response path |
| Deleted family leaves stale memberships | [sithtoast's proposed fix 35](https://github.com/alexjyong/Simitone/pull/35); DeleteFamily removes chunks without clearing NBRS family references | Detach matching listed members, clear cached subsets, remove family/name chunks; reject stale, housed and special-family deletion |
| CAS can fail in unwritable executable folder | Same PR35; unconditional File.AppendAllText in SetFamilies | Remove obsolete debug file writes |

Implementation adapts the findings and layout from sithtoast's upstream PR42 and PR35, with additional guards and regression coverage. Those upstream PRs were open when reviewed. No upstream PR has been merged by this work.

## Remaining queue

| Candidate | Evidence / present status | Next useful verification |
| --- | --- | --- |
| Vacation save/resume | [Current known limitations](https://alexjyong.dev/Simitone/), [original roadmap](https://github.com/riperiperi/Simitone/issues/8); our separate draft PR2 | Play save/restart while away, multi-lot vacation, checkout and home return; review integration before combining with fame |
| Eviction leaves household on lot | [Issue 18](https://github.com/alexjyong/Simitone/issues/18), open | Inspect retained house FAMI and saved VM; fixture-based roundtrip before changing lot records. Family deletion here is not an eviction fix |
| Blank buy/build icons | [Proposed PR46](https://github.com/alexjyong/Simitone/pull/46), open | Review VM mutations from temporary object creation, exceptions and texture/cache lifetime before adopting renderer fallback |
| Cars cannot leave hillside lots | [Proposed PR41](https://github.com/alexjyong/Simitone/pull/41), open | Compare proposed FreeSO change with pinned routing and reproduce on a slope |
| Household not initially selected | [Issue 15](https://github.com/alexjyong/Simitone/issues/15), open; baseline already has selection/fallback logic | Verify household selection versus visitors/NPCs and arrival timing; do not assume still wholly unimplemented |
| Phone resets and becomes unusable | [2022 forum report](https://forum.freeso.org/threads/simitone-game-bugs-when-using-phones.6727/) | Report has no stack trace and used Linux/Proton. Reproduce current call/cab flows and record SimAntics log before selecting an engine fix |
| Clothing CC / missing objects crash startup | [2018 forum discussion](https://forum.freeso.org/threads/how-do-i-make-simitone-recognize-my-neighborhood-without-crashing.6330/), [Issue 29](https://github.com/alexjyong/Simitone/issues/29) | Test current custom-content fixtures; skin catalogue fix remains separate PR1. Old discussion is not a current reproduction |
| Frozen lots | [FreeSO proposed PR298](https://github.com/riperiperi/FreeSO/pull/298) | Its LastTick comparison change is already present in our pinned VMNetDriver; no duplicate patch needed |
| Missing controllers on unplayed lots / random dialog pictures | Upstream issues13/47 closed, fixes already in baseline | Retain as regression scenarios; do not reapply historical fixes |
| Custom object animations | Baseline03b3f03 explicitly bumps FreeSO for GameData/Skins animation loading | Reproduce with custom meshes/animations before further loader changes; original content-specific failures remain possible |

## Validation and in-game checklist

- `dotnet run --project Tests/Simitone.Dialog.Tests -c Release`: links the production alert source to minimal UI doubles. Checks layout for 640/1024/1920 widths, short/long text, icon allowance, zero/one/three buttons, repeated resize, focus and Enter response. This verifies layout/control logic, not rendered pixels, keyboard platform integration or pet adoption.
- `dotnet run --project Tests/Simitone.Family.Tests -c Release`: links production cleanup to the real pinned IFF/NBRS implementation. Checks matching/moved/unrelated/missing/short records, protected/housed/stale families, chunk-ID reuse and NBRS serialization/reload. Uses synthetic data only.
- Windows Release compilation and both suites run in `bugfix-checks.yml`.
- In-game: adopt a dog/cat, type a name, test both OK and Enter, return home and save/reload. Check baby naming too. Try a longer prompt and resize while the dialog is open.
- On a disposable profile: create/delete an unplaced family, create another, save/restart and verify no old members appear. Check Cancel does nothing and housed families stay outside this deletion path.
- Launch from a protected/read-only executable folder with a writable profile and verify CAS opens; the obsolete log must not be required.

This branch is independent of the Superstar and vacation previews. Nothing is merged; original installation and original game assets are untouched. No complete gameplay playthrough has been performed.
