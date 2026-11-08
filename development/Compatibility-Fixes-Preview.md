# Compatibility fixes experimental preview

This separate edition builds on the diagnostics preview (PR #9). It remains unmerged and retains the isolated TestProfile, launcher and bug report collector. Extract into a new writable folder outside the original Sims installation. Do not overlay an older preview or import important saves. Run Start-Gameplay-Preview.cmd in Windows/Parallels. The bundled runtime requires no separate .NET installation; original Complete Collection assets are still required.

## Changes and targeted verification

| Fix | Automated coverage | In-game check using a disposable household |
| --- | --- | --- |
| Native outfit queries no longer remove shared entries; existing suit IDs are found correctly | Tone switching, breed catalogue preservation, valid/invalid indices, case-insensitive lookup | Switch light/medium/dark tones repeatedly; buy/change everyday and expansion outfits; reopen the outfit selector |
| Existing allowed loose-content folders accept mixed-case filenames and exclude backup extensions | Actual engine provider with synthetic CMX/CFP/IFF paths | Load known custom skins and animated objects with uppercase extensions; compare with the diagnostics preview |
| Failed CFP decode cannot publish partial cached arrays | Actual provider retries a truncated CFP twice, then decodes repaired synthetic data | Exercise custom-object and Sim animations repeatedly after travelling/reloading; report asset names and errors |
| Invalid inventory removals and additions cannot wrap ushort counts | Production primitive source with VM doubles, real NGBH serialization | Buy/use/remove small stacks, pet treats and Magic ingredients; save at home, restart and compare counts |
| Follower updates use token 10/11 rather than saved motive 0/1 | Both follower modes with/without existing motives, repeated updates, real NGBH roundtrip | Invite/change companions for travel and return home; verify companions and bladder/comfort remain sensible |
| Empty compatible CAS lists cannot be accepted/randomized/indexed | Availability gate regression, extracted production head setter with null/empty/valid lists; Windows compilation | If a custom-content selection has no compatible textures, check the message and choose another selection; verify recovery |

These checks do not prove the full fame journey, pets, Magic or all custom assets work. No gameplay session was performed by the developer. Missing native expansion implementations remain outside this patch. Existing duplicate follower records or previously corrupted counts are not automatically repaired. Malformed animations still produce errors; this change prevents a misleading partial cache hit. This is not a performance optimization claim.

## Test tonight

Start with a new household, test CAS and custom assets, then ordinary home saving/reloading. Exercise Superstar, pets and Magic separately so a failure has clear reproduction steps. Use the diagnostics preview in its own folder for comparison if needed. **Return home before saving: vacation checkpoints and eviction fixes are not included.**

After a problem, close the game if possible and run Collect-Bug-Report.cmd. Send its ZIP plus the complete error screenshot, steps, expected/actual result and affected custom-content filename. The collector excludes saves and game resources. See Reporting-Bugs.md. Keep the entire test folder until reports are resolved.

No changes are intended to rendering, classic isometric appearance, lighting, shadows, zoom or weather.
