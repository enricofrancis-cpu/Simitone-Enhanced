# Lot weather and blueprint export

Two bugs verified in baseline03b3f03:

- All ordinary lots loaded and saved `LocalHouse/cas.weather`, sharing the last saved weather across houses and neighbourhoods.
- Blueprint export created `Blueprints/cas.xml` as a directory, then attempted to write an XML file to that path. Real filesystem operations reproduce the failure.

## Changes

Each lot now reads/writes its own adjacent `.weather` file, e.g. a copied `UserData/Houses/House05.iff` uses `UserData/Houses/House05.weather`. Exported `Blueprints/cas.xml` uses `Blueprints/cas.weather`. Normal saves no longer write the global CAS weather file.

Weather uses an exact two-byte, little-endian short. Missing or damaged optional weather defaults clear; damaged files remain for inspection and produce a diagnostic. Reads are bounded and never load arbitrary large sidecars into memory. Writes stage, flush and replace the weather file, cleaning temporary files on failure. This atomic replacement applies only to the weather file, not to the whole multi-file game save.

No fallback to the old shared CAS weather file is attempted: that file cannot identify which lot it belonged to, so applying it to all houses would preserve the bug. Existing lots without a dedicated sidecar initially open clear. Normal saved lots gain their own weather file on the next save. In-session travel/home weather and vacation checkpoint integration remain separate; this fixes file persistence, not every weather/travel behavior.

Blueprint export creates the parent `Blueprints` directory. An empty `cas.xml` directory left by the old bug is removed before export; nonempty directories are preserved and the operation fails rather than deleting their contents. XML data and the existing VM export format are unchanged.

## Verification

`dotnet run --project Tests/Simitone.LotFiles.Tests -c Release` links the production file-state helper. It verifies distinct houses/neighbourhoods/profiles, no legacy weather leakage, signed short/binary roundtrips, corrupt/oversized optional sidecars, failed replacement cleanup, the original export-directory failure, successful export-path repair and nonempty-directory preservation. It uses temporary synthetic files only, no game assets.

The Windows workflow builds the complete Release client and runs the regression executable. In-game verification remains necessary: save different weather in two homes, restart and reopen each; test another neighbourhood; try blueprint export and confirm XML plus VM outputs. No graphical weather or exporter playthrough has been performed.

No original installation is modified, no game assets are included, and the FreeSO pin is unchanged. This overlaps TS1GameScreen in the separate fame/vacation branches and must be integrated and rechecked deliberately.
