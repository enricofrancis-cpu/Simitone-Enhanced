# Superstar playable preview

This development build connects the original Superstar object scripts to a new
native-call adapter and provides a Windows x64 test bundle. It retains the
pinned FreeSO revision and renderer. It has not completed a real in-game career
playthrough; it is intended for that verification now.

## Implemented loop

- Tabloid enrollment is still performed by the original script (ordinary job 0,
  positive fame score). The adapter reads the original `GameData/fame.iff` at
  runtime: ten score/skill/friend requirements and eleven displayed ranks.
- Generic call 34 checks promotion. Call 36 reconciles score decay/demotion and
  promotion, matching its use by the Studio Town "Promotion Check" script.
  Results are false when no transition occurs; original scripts can then choose
  their proper dialog/reward branches instead of receiving unconditional success.
- Existing audition, singing, acting, modelling, payout, award, fan and decay
  scripts remain responsible for their own interactions. The adapter supplies
  the missing rank changes they depend on; their full behaviour is not yet proven.
- The career panel shows fame titles/stars, score progress toward the next rank,
  celebrity-friend power, and the next six skill targets. Normal careers retain
  their own panel. Friend, family and famous-contact filters now apply.
- Fame and the six ordinary skills synchronize to neighbourhood records before
  saves/travel and to retained home avatars before returning. Native save-Sim
  call 25 also synchronizes these fields. Motives, inventory, personality and
  ordinary-career data are not arbitrarily copied by this adapter.
- Diagnostics go to `TestProfile/superstar-preview.log`, including loaded tuning,
  changed/blocked checks, and explicit test overrides.

## Explicit preview policies to compare in-game

These are implementation choices informed by the resource scripts, not claims
of exact original-executable behaviour:

1. Ranks use half-stars (0–10), with at most one half-star transition per check.
   Score/skill/friend gates are inclusive. The final zero tuning row is a sentinel.
2. Promotions require all score, six skill and friend-power gates. Demotion is
   score-based and does not erase rank solely because a friend relationship fell.
3. Friend eligibility uses the original global routine's mutual short-term
   relationship **greater than** the loaded global threshold (50 in the supplied
   resources). Famous-friend power counts each eligible non-household Sim once,
   using displayed stars (two raw rank units per star). Household exclusion and
   friend-power units still need comparison against the original game.
4. Call 36 can promote as well as demote because the original controller checks
   both result directions after it. Call 34 is promotion-only. This distinction,
   transition timing and demotion hysteresis need gameplay confirmation.
5. Field 82 is preserved as a score high-water mark, following the original
   `FameObjectGlobals` comparison, rather than interpreted as a star count.

Missing/incomplete fame tuning disables the adapter's rank changes and is logged;
it does not synthesize defaults or report successful promotions. Some original
fame-title text substitutions are still unsupported by the upstream dialog parser.

## Start the Windows bundle

Extract everything into a writable folder outside the original installation.
Run `Start-Superstar-Preview.cmd`, then choose the existing Complete Collection
game path. The bundle includes .NET and needs no separate runtime installation.
It includes no original game assets.

The marker beside `lib/Simitone.exe` makes direct launches use the adjacent
`TestProfile` too. Profile selection happens before configuration is loaded.
The original game data is read, and neighbourhoods are copied into this profile
by Simitone's existing importer. Keep the marker in place. Do not transplant
experimental saves into the original installation or normal Simitone profile.

## Quick access for testing gigs

The career panel's **Test** button appears in the marked bundle. It can:

- advance the selected Sim by half a star, bypassing normal gates;
- set all six skills to ten;
- reset to enrolled with zero stars.

These are clearly logged test overrides, and advancing/resetting ends any
ordinary career for that Sim. Use them to reach high-level audition objects
quickly. Do not use them as evidence that ordinary promotion or awards work.

## Gameplay test order

1. **Agency:** start with a fresh profile/family, enroll through the tabloid and
   verify nobody/zero stars. Confirm an ordinary salary job blocks fame checks.
2. **Entry gigs:** earn the first score at open mic or the photo set. Check the
   score meter, half-star increase and original feedback. Try failing a gig too.
3. **Every half-star:** test exact and just-below score, skill and friend gates.
   Confirm no repeated promotion feedback without a rank change. Work through
   singing, recording, music video, soap-opera/acting, runway and photo shoots.
4. **High-level scenes:** use Test tools in a separate test household to unlock
   ranks quickly. Check audition availability, animation sequences, choices,
   success/failure results and payouts. Record the object and star level on error.
5. **Friends:** make a celebrity friend, test one-way/mutual relationships, the
   threshold boundary and the famous-contact filter. Check the displayed total.
6. **Decay/demotion:** stop performing, trigger decay/breakdown and confirm score
   and stars decrease with the expected feedback. Check the cap at five stars.
7. **Persistence:** gain skills/fame in Studio Town, switch destinations, return
   home, save, restart and verify the exact values. Check absent household members
   and visitors were not changed. Quit/rejoin the track and check reset values.
8. **Rewards/NPCs:** verify rank-specific rewards, paparazzi, fans/obsessed fans,
   celebrity interactions and original promotion/demotion dialogs.

Studio Town save-and-resume while still away is not implemented by this branch;
return home before saving a persistent career test. The separate vacation PR is
not included. General same-session unsaved-neighbourhood rollback remains an
upstream limitation. UI layout and the full game loop require your in-game run.

## Automated verification

The regression executable covers every half-star score/skill/friend gate, a
zero-to-five-star progression and decay back to zero, native registration before
BHAV assembly, fallback delegation, false results for missing targets, friendship
eligibility, fame/skill transfer, NBRS serialization, and isolated profile defaults.

```sh
dotnet build Tests/Simitone.Fame.Tests -c Release -m:1
dotnet run --project Tests/Simitone.Fame.Tests -c Release --no-build
```

An optional `--fame-resource /path/to/fame.iff` checks the real local tuning file.
No game resources are committed to the test suite or bundled with the executable.
