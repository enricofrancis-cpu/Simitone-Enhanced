# Superstar fame workflow audit

## Scope and evidence

Reviewed Simitone baseline `03b3f03` and pinned FreeSO `d8a3c175`, plus the user's
uploaded Complete Collection resources in `GameData.zip`, `ExpansionPack6.zip`
and `ExpansionShared.zip`. The original resources were inspected in scratch
copies only. No original IFFs, animations, art or other game assets are committed.

This is a source/script audit, not a completed in-game Superstar playthrough.
Script presence does not establish that the interactions execute successfully.

## Workflow findings

| Stage | Original resource evidence | Current finding |
| --- | --- | --- |
| Join the fame track | `Tabloid.iff`, BHAV 4109 (`offer job`), sets caller fame score to 1 | Enrollment script exists. Global BHAV 485 checks ordinary JobType is 0 and fame score is positive. Live fame fields were not automatically synchronized to NBRS before travel; this PR adds that boundary transfer. Test agency sign-up and quitting an ordinary job in-game. |
| Leave the track | `Newspaper.iff`, BHAV 4123 (`Clear Fame`), clears score and star power | Existing script exists; transfer must preserve zeroes rather than keeping only higher values. |
| Travel to Studio Town | `PhonePluginStudioLots.iff`, `CarStudio.iff`, `PedMarkersStudio.iff`, Studio Town dialog handling | Travel path exists. Returning to the retained home VM can restore stale fame. This PR addresses that data transfer. |
| Entry gigs | `OpenMicSuperstar.iff`, BHAV 4099, and global BHAVs 483/484 | Script awards fame through the global adjustment path. Global 484 changes score and clamps it to 1–1000, then calls native promotion/demotion. Native calls are missing. |
| Singing, acting and modelling | `MusicRecordingStudio.iff`, `MusicVideoSet.iff`, `SetSoapOpera.iff`, `Stage.iff`, `SetPhotoShoot.iff`, `FashionRunway.iff`; `FameObjectGlobals.iff` | Gig-specific routines and fame-level selectors exist. They still need live audition, performance, result, payout and animation tests; source inspection alone cannot certify them. |
| Promotion | `Global.iff`, BHAV 484, node 16; phone plugin BHAV 4125, node 1, call generic 34 | `PromoteFameIfNeeded` is an enum/comment only in `VMGenericTS1Call.Execute`; there is no case implementing it. |
| Demotion | `Global.iff`, BHAV 484, node 29; `ControllerStudioLot.iff`, BHAV 4128, node 7, call generic 36 | `DemoteFameIfNeeded` is likewise missing. Decay checks cannot complete the intended rank transition. |
| Result notifications | Global BHAV 484 and the phone/controller checks branch on the native result, then compare old/new star power | The missing calls currently fall through to `GOTO_TRUE`. **Inference:** this can route scripts into promotion feedback without an actual rank change. Verify in-game; do not count dialogs as proof of progression. |
| Skills and famous friends | `GameData/fame.iff`, STR# 4, 5, 6 | Original tuning exists. Native promotion needs to consume the game's score, skills and friend-power requirements; summation units and exact boundary behaviour still require verification. |
| Fame decay and breakdowns | `ControllerStudioLot.iff`, BHAVs 4108, 4139 and 4140 | Existing scripts reduce score, apply breakdown penalties and check rank. Missing native demotion blocks the rank part. |
| Awards, fans, paparazzi, agents | Studio controller, `StarAwards.iff`, Superstar NPC resources | Behaviour assets exist and depend on working fame/rank state. Awards, obsessed-fan behaviour, celebrity networking and agent interactions remain unverified. |
| Career display | `UIJobSubpanel.Update` | Displays ordinary CARR jobs; no fame-specific panel, star meter or famous-friend requirement display is implemented here. Fame is a separate track, not an ordinary salary career row. |
| Fame relationship view | `UIRelationshipSubpanel.UpdateRelView` | Fame button changes `RelSort`, but that value is not used when building the relationship list. Friend/family/all filters are also not applied there. |
| Fame text substitutions | `VMDialogHandler` token list and parser | No `FameTitleLocal` implementation found. Original fame-related text substitutions need their own audit/fix. |
| Preserve fame during travel | `SavedLot = vm.Save()` at departure; `InitializeLot(SavedLot)` on return | The snapshot contains old person data. This PR copies live family fame to NBRS before every switch/save, and to the retained home snapshot when leaving an away lot. |
| Persist/reload fame | `VMAvatarMarshal.PersonData`, `Neighbour.PersonData`, `NBRS.Write/Read` | Raw fields are supported by existing serializers. New regression tests exercise the actual NBRS writer/reader. A full saved-game reload still needs live testing. |

## Original tuning, without inventing progression rules

`fame.iff` stores eleven entries per requirement table. The final all-zero entry
is retained as data and must not be treated as an extra promotion. Entries below
are in resource order. Skill values are points in the table, not raw person-data
units; regular skill fields use hundredths of a point.

| Entry | Fame score | Charisma | Body | Creativity | Friend-power value |
| --- | ---: | ---: | ---: | ---: | ---: |
| 0 | 2 | 0 | 0 | 0 | 0 |
| 1 | 9 | 1 | 0 | 1 | 0 |
| 2 | 21 | 2 | 1 | 2 | 0 |
| 3 | 35 | 3 | 2 | 3 | 0 |
| 4 | 52 | 4 | 3 | 4 | 2 |
| 5 | 78 | 6 | 4 | 4 | 4 |
| 6 | 156 | 6 | 5 | 6 | 7 |
| 7 | 300 | 7 | 6 | 7 | 11 |
| 8 | 550 | 8 | 7 | 8 | 14 |
| 9 | 900 | 10 | 8 | 9 | 18 |

Cooking, Mechanical and Logic requirements are zero in these rows. The title
table contains eleven ranks spanning zero to five stars in half-star steps.
This change does not guess strict/inclusive thresholds, friend-power units,
multi-rank promotion behaviour or demotion hysteresis.

Person data indexes are 80 (score), 81 (star power) and 82 (the engine's
`TS1FameStarHighWatermark`). `FameObjectGlobals` BHAV 8197 compares score with field
82 and updates it from score, so its name alone must not be taken as proof that
it holds a displayed star count.

## Implemented in this PR

Before a lot switch or save, capture the live active household members'
raw fame fields. Match by persistent object GUID, not transient lot object ID.
Update neighbourhood records and, while away, matching retained home avatars, so subsequent
lot loads do not reinstate older fame data. NPCs, unrelated visitors, family
members absent from the live lot and all other person-data fields are untouched.

Do not recompute ranks, maximize score, synthesize promotions or prevent real
demotions/resets. The native promotion/demotion gaps remain explicitly open.
No renderer, lighting, shadow, weather, zoom or interface redesign changes.

Automated checks link the production helper and pinned FreeSO project. They
cover family-only capture, stale-home repair, successive destinations,
demotion/reset, GUID identity, unrelated-data isolation, missing/short legacy
arrays, and a real NBRS save/read round-trip with synthetic neighbour data.

```sh
dotnet build Tests/Simitone.Fame.Tests -c Release -m:1
dotnet run --project Tests/Simitone.Fame.Tests -c Release --no-build
```

## Next implementation order

1. Implement and independently test native generic calls 34/36 using original
   tuning and verified target-Sim, boundary, friendship and return-value semantics.
   Include false-result behaviour when no rank changes. This is the main career blocker.
2. Exercise each performance object's interaction/result flow on copied Studio
   Town lots; diagnose native failures separately from missing assets/animations.
3. Verify decay, breakdowns, promotion rewards, awards and celebrity networking.
4. Fix fame-aware UI/text and relationship filtering after the gameplay rules work.
5. Complete save/reload and repeated home/Studio Town transition tests.

## Required gameplay matrix

Use a disposable test account/VM with copied game data and a copied Simitone
profile. `-path` alone does not isolate `Documents/Simitone/UserData*`.

- Enroll through the tabloid and confirm initial fame state and ordinary-career gating.
- Earn score at an entry gig, record fields 80–82, switch Studio Town lots, then
  return home. Confirm those exact fields persist, with no changes to unrelated
  skills, motives, job, inventory or family members who stayed home.
- Save at home, restart, reopen the household and verify fame and neighbourhood
  values. Test both score gains and script-driven decreases/reset values.
- Exercise singing, acting and modelling at every half-star level, checking job
  availability, skill gates, minigame choices, success/failure, payouts and animations.
- Test famous-friend requirements, decay, demotion, quitting fame, rewards,
  paparazzi/obsessed-fan events and repeated travel with more than one household.
- Verify promotion feedback corresponds to a real rank change after calls 34/36
  are implemented; current feedback is not reliable evidence.

This PR is independent of the skin-selection and vacation-checkpoint PRs and
targets `enhanced-development`. It also edits `TS1GameScreen`; when combining
with vacation saving, keep the transfer before both travel restoration and the
save/checkpoint serialization path. Review/retest that integration before merging.
