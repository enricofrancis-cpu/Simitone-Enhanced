# Second bug pass — phone-book safety

Baseline: enhanced-development at 03b3f03, FreeSO d8a3c175. No changes to the pinned submodule or original game installation.

## Confirmed and fixed

`UICallNeighborPanel` initially passed `-2` to `NeighbourList_OnSelectionChange` when no family had callable contacts. That callback only recognized `-1` as no selection and immediately indexed family `-1`. A new Sim who knows nobody could therefore crash the phone dialog before Cancel became available. A headless run of the original production source reproduces this ArgumentOutOfRangeException.

The constructor also dereferenced missing relationship targets and family strings, and indexed person data without length checks. Deleted neighbours, incomplete records and stale family links are now skipped. Relationship keys outside the callable positive-short ID range cannot wrap into another Sim's ID. Family labels have a missing-name fallback; contact labels use the saved neighbour name.

Both selection callbacks now reset invalid/stale selections safely. Call revalidates the neighbour and household membership immediately before returning its ID; Cancel still returns -1. Switching families clears the previous contact and portrait.

Portrait previews use the existing off-lot `UIIconCache.GetNeighbor` path rather than creating and deleting a Sim in the active VM. Missing or failed custom appearances leave the valid contact callable with no portrait; a diagnostic is written to stderr. Fatal allocation failures are not swallowed. The renderer itself is unchanged.

This is a reproducible phone-book fix, not a claim that every historic phone or cab error is resolved. The [2022 Linux/Proton phone report](https://forum.freeso.org/threads/simitone-game-bugs-when-using-phones.6727/) contains no stack trace, so its specific cause remains unconfirmed.

## Verification

Run with .NET 9 and pinned submodules:

```
dotnet build Tests/Simitone.Phonebook.Tests -c Release
dotnet run --project Tests/Simitone.Phonebook.Tests -c Release --no-build
```

The regression executable links the actual phone-book UI source and the pinned person-data enum, uses real Neighbour/FAMI/FAMs records, and substitutes small provider/UI doubles for graphics and game assets. It checks empty/null callers and relationships, stale/out-of-range relationship keys, pets, null/short person data, missing/reused families, family names, renamed contacts, invalid list indices, selection changes, portrait failures, no active-VM preview allocations, Call revalidation and outer-dialog cancellation. The original baseline source fails the empty-contact test; the fixed source passes.

Local Windows-targeted Release compilation passes with zero errors; existing dependency/compiler warnings remain. Native Windows Release and the phone-book suite also run in `phonebook-checks.yml`. No in-game playthrough or visual rendering has been performed.

In-game checks: use a new Sim with no contacts and cancel the phone book; meet several human Sims and call the intended contact; change selected families; try a renamed Sim and a contact with custom clothing; verify no duplicate Sims or SimAntics errors. Test a normal answered call and an unanswered call, then cab services separately. A successful UI response does not prove downstream phone scripts work.

## Other findings — deliberately separate

- Eviction currently changes the family budget and calls MoveOut, but does not persist those changes or clear the stored lot state in that action. A complete fix must reconcile lot snapshots, family data, finances and failure recovery. This pass does not claim to fix eviction.
- Normal lot loads and saves share `LocalHouse/cas.weather`, so weather state is not isolated per house. The existing blueprint export action also attempts to create a directory named `Blueprints/cas.xml` before writing that file. These are source findings awaiting separate reproductions and regression coverage.
- The earlier Superstar, vacation, skin and family/naming fixes remain in separate draft PRs. This branch is not included in the existing Superstar ZIP and nothing has been merged.
