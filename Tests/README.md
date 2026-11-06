# Compatibility regression checks

Run `dotnet run --project Tests/Simitone.Compatibility.Tests -c Release` with .NET 9.
This executable links the production skin catalogue filter and exits nonzero on failure.
It needs no game assets, graphics device, test packages, or Sims installation.

The checks cover catalogue preservation across skin-tone changes, texture and hand
alignment, consecutive unavailable textures, and empty/all/none-available selections.
Replacing the filter's catalogue copy with the original list causes the preservation
assertion to fail, reproducing the old shared-list mutation.

## In-game verification still required

Use a disposable copy of game data and saves. Include valid custom head/clothing
meshes whose textures exist for only one skin tone, alongside stock content.

1. In Create-a-Sim, select light, dark, medium, then light again. Confirm custom heads
   reappear for their supported tones, in the same order, without restarting.
2. Create another Sim and reopen Create-a-Sim. Confirm the first selection did not
   remove heads from the second Sim's catalogue.
3. Open the clothing selector for Sims with different skin tones, close and reopen
   it, and verify supported custom clothes remain available. Check hands and body
   textures stay paired correctly.
4. Check stock heads/clothes and child/pet selection for regressions.

This fix preserves selection catalogues; it does not add missing textures or repair
malformed meshes, object scripts, or animation data. The compatibility-fixes edition also guards empty-selection indexing, randomization
and acceptance; the unavailable-content message still needs visual in-game verification.

## Upstream review

Baseline Simitone `03b3f03a1619df9614480761ae050bac4ef2933d` pins FreeSO
`d8a3c175f4456cb76a1eded77237cabb9c3645c0`. The pin is retained.

- FreeSO `31e5a3e4` already broadens skin IDs to letters/digits, registers unisex
  variants, and updates custom audio/object/skin loading.
- FreeSO `4ec98c57` includes custom-animation decode validation, failed-file
  reporting and null-animation handling.
- FreeSO `d8a3c175` adds loose `.cfp` discovery in `GameData/Skins` alongside
  `Downloads`, and drops unloaded normal/carry animation states when loading saves.

The remaining selection bug is in Simitone: Create-a-Sim's head list and the
clothing selector's body list alias `TS1ClothingCollection.ClothesByAvatarType`.
Filtering missing textures therefore permanently removed entries from shared
content for the running session. `GeneralAvatarType` already returns a new body
list; it now uses the same filter to keep all selection paths consistent.

## Compatibility-fixes edition

Run `dotnet run --project Tests/Simitone.ExpansionSafety.Tests -c Release` for
source-linked outfit/inventory regressions with VM/content doubles and real NGBH
serialization. `python Tests/VerifyCasEmpty.py` compiles the current CAS head setter
against small doubles to exercise null/empty lists and valid index wrapping.
The actual engine project tests discovery and failed CFP retries in EngineSafety.
Windows CI runs all nine suites, both probes and the collector check before packaging.
