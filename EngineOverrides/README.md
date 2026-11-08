# Reviewed FreeSO engine overrides

These MPL-2.0 provider copies come from FreeSO commit `d8a3c175f4456cb76a1eded77237cabb9c3645c0`, pinned by the unchanged submodule. Original source and notices are retained. New helpers are MPL-2.0 too; see `FreeSO/LICENSE.md`.

Root `Directory.Build.targets` replaces six compile items across FSO.Content, FSO.SimAntics and FSO.Vitaboy with these reviewed copies and includes their safety helpers. It does not patch the submodule working tree. SHA-256 guards accept the original LF or CRLF files and fail when any underlying source changes. An upstream upgrade requires reviewing the diff, porting or removing each override, and deliberately updating the expected hashes. Never update only the hashes.

Changes relative to the pinned sources:

- `TS1NeighbourProvider.InitSpecific`: stage first neighbourhood import, validate required nonempty resources, and rename the complete directory into place. Preserve established profiles and refuse incomplete ones without automatic repair.
- `LoadCharacters`: skip unrelated files, accept case-insensitive IFF extensions, and calculate the next filename from all directory entries before loading characters.
- `SaveNewNeighbour`: serialize to a temporary file and publish under an unused five-digit name without replacement. Update the index and registration only after successful publication.
- `TS1BCFProvider.Init`: validate name length before clothing classification, retaining short names in the generic appearance catalogue.

- `TS1Provider`: case-insensitive, extension-anchored discovery in the existing allowed folders; retain archive precedence.
- `TS1BCFProvider.Get` and `CFP.EnrichAnim`: require both arrays and publish them only after decoding completes. Malformed animations still fail; incomplete results cannot become cache hits.
- `VMTS1PurchasableOutfitHelper`: filter a catalogue copy, look up tuple outfit IDs correctly, and reject invalid selections before writes.
- `VMTS1InventoryOperations`: reject count underflow/overflow and invalid negative counts; update follower records 10/11 instead of motive records 0/1. No migration of already-damaged data.

`Tests/Simitone.ExpansionSafety.Tests` links the overlaid primitive/helper sources and real NGBH serializer with minimal VM/content doubles. It checks bounded inventory changes, follower/motive separation and serialized roundtrips, plus native outfit queries. This is headless coverage, not an in-game engine session.

`Tests/Simitone.EngineSafety.Tests` references the actual FSO.Content project, exercises provider methods with synthetic IFF and BCF files, and verifies failures/collisions/import recovery. `Tests/VerifyOverlayGuard.py` checks the actual MSBuild target with unchanged LF/CRLF and changed source fixtures.

This is not a transaction across neighbourhood, family and character resources. Import validation checks required files exist and are nonempty, not that their game data is semantically valid. A process kill can leave an unpublished staging directory; it is ignored on retry, never promoted automatically. Do not claim power-loss durability or complete family-creation rollback.
