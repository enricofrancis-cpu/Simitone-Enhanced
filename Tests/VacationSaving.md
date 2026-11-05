# Experimental vacation saving

This change adds Simitone vacation checkpoints for new trips to lots 40–49.
It does not convert existing original-game vacation saves or repair earlier
Simitone saves that already overwrote a destination house.

## Behaviour

- Saving while a household is on vacation captures the live destination VM and
  the home VM retained when the trip started. Travel context identifies the
  household, controlling Sim, destination, transit mode and each lot's weather.
- The checkpoint lives in Simitone's copied neighbourhood's
  `Simitone/Vacations/HouseXX.svc`, keyed by the household's home lot. The vacation
  destination's house IFF is not overwritten by this save path.
- Reopen the household's **home lot** from the neighbourhood to resume its trip.
  The destination VM loads without the ordinary house reset, preserving running
  interactions; the saved home VM remains available for the return journey.
- Returning home does not erase the last vacation save. Saving successfully at
  home removes the checkpoint. Exiting without saving keeps the last saved trip.
- A damaged, unsupported or mismatched checkpoint shows an error and is retained;
  the loader does not silently open an older home save instead.
- Save-and-quit and save-and-return stop on a save error, leaving the current lot
  open so the player can retry or explicitly choose not to save.

The checkpoint is checksummed and staged to a temporary file in the same
directory, flushed, then replaced. An invalid snapshot envelope or failed
neighbourhood-save callback cannot replace the previous checkpoint. The existing
neighbourhood/character file writes are **not** a multi-file transaction: a disk
failure after those writes may leave them newer than the retained checkpoint.
Backups must include both neighbourhood data and its checkpoint directory.
This does not fix the engine's broader in-memory neighbourhood discard semantics.
Restart before verifying a discarded trip so funds/inventory are reloaded from
the persisted neighbourhood as well as the VM checkpoint.

## Automated checks

With .NET 9 and the pinned submodules:

```sh
dotnet build Tests/Simitone.Vacation.Tests -c Release -m:1
dotnet run --project Tests/Simitone.Vacation.Tests -c Release --no-build
```

The executable links the production checkpoint and snapshot helpers. It checks:

- both snapshot payloads and travel/weather context survive round trips;
- household/home identity and controlling-Sim membership must match;
- every truncation of a valid checkpoint, checksum damage, unsupported versions,
  invalid snapshot lengths and non-vacation destinations are rejected;
- a simulated neighbourhood save failure keeps the previous checkpoint and
  removes temporary files; a successful save replaces it;
- neighbourhood and household paths stay separate;
- real FreeSO compressed `VMMarshal` serialization round-trips synthetic home and
  vacation lots with different clocks, RNG state and family data;
- the production snapshot reader rejects a swapped home/destination and non-VM data.

Synthetic lots contain no Sims or game assets. These checks do not exercise hotel
scripts, actual VM activation, vacation fees, inventory transfer or returning home.

## Required in-game verification

Use a separate disposable Windows account or VM, with copies of the game's data
and the Simitone profile. On Windows, Simitone writes its copied neighbourhoods
under `Documents/Simitone/UserData*`; setting `-path` alone does **not** isolate
these saves. Launch with the copied game's `-path` and verify the profile and
neighbourhood paths belong to the test account before running. Do not use the
original game data or the normal Simitone profile for these checks.

1. Record a household's members, funds, inventory, home objects and home clock.
   Make a backup and note the vacation destination IFF's hash.
2. Travel to a vacation lot. Check into a hotel, change motives, buy an item and
   start an interaction. Save. Confirm the destination IFF hash is unchanged and
   a checkpoint appears under the copied neighbourhood.
3. Quit, restart and select the household's home. Confirm it resumes at the saved
   destination, with the right family, funds, inventory, hotel booking, clocks,
   weather and interaction state. Check for duplicates and SimAntics errors.
4. Save again, restart again, travel between vacation destinations and return
   home normally. Check home layout, motives, purchases, funds and household
   membership; no duplicate Sims should appear.
5. After returning, exit without saving, restart Simitone, and confirm reopening
   restores the last saved trip. Then return and save at home; confirm the checkpoint is removed
   and a fresh restart opens the home rather than the vacation.
6. In disposable copies, corrupt a checkpoint and verify an error is shown without
   deleting it. Test a save failure and confirm Save-and-Quit/Return does not exit.
7. Check another household and another neighbourhood, a normal home save, and
   ordinary non-vacation travel. Keep their data independent.

This remains a draft until these gameplay checks pass. FreeSO's pin and the
lighting, shadows, zoom, weather effects and isometric renderer are retained.
