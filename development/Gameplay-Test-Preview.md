# Combined gameplay test preview — 5 November 2026

Extract to a new folder outside the original Sims installation. In Windows/Parallels, run `Start-Gameplay-Preview.cmd` and select Complete Collection if prompted. The self-contained runtime and separate `TestProfile` are included/configured; game assets are read from your installation. Start with a fresh test profile and do not overwrite the earlier preview folder.

This candidate combines the Superstar preview with the separately reviewed skin, naming/family, phone-book and lot-file fixes. It is built on the tested Superstar branch and includes the changes from draft PRs1,4,5,6. No PR has been merged into enhanced-development or upstream. FreeSO stays d8a3c175; the renderer, lighting, shadows, zoom and weather effects remain intact.

## Test checklist

1. Load a family and check normal movement, controls and ordinary object interactions.
2. Open the phone book for a Sim with no contacts; Cancel must work. With known contacts, choose a family then a Sim, change families, and call the intended Sim. Check ordinary answered/unanswered calls.
3. Adopt/name a pet: type a name and test both OK and Enter on separate attempts. Save at home and reload. Also check baby naming when available.
4. Cycle skin tones light/dark/medium/light in CAS with custom heads/outfits, then open clothing selection for another Sim. Compatible items should remain available; check hands/textures too.
5. In this disposable profile, create and delete an unplaced family, then create another. Restart and verify old Sims have not attached to the new family. This does not fix eviction from occupied lots.
6. Join the Superstar career through the tabloid and visit Studio Town. Check gigs, fame feedback and progression, skills and celebrity friends, then return home and save/reload. The career panel's Test controls deliberately override rank/skills for testing; use real gigs to check normal promotion requirements. See Superstar-Preview.md for exact policies and full checklist.
7. If testing weather, save two homes with different weather, restart and open each. Each home should retain its own saved weather. Existing lots with only the old shared weather file initially load clear; in-session return-home weather is still a separate limitation.
8. If using blueprint export, confirm the XML file and VM export are produced. An old empty cas.xml directory can be repaired; a nonempty directory is preserved and blocks export.

## Important limits

**Return home before saving.** Vacation checkpoints are not included in this build; saving/resuming while away remains outside this candidate. Eviction, hillside-car routing, missing catalogue pictures and other independent object/animation bugs remain open.

Automated tests cover code behavior and synthetic records/files. Naming/phone-book UI tests use headless doubles, so they do not verify rendered pixels, actual custom meshes, adoption scripts or completed phone conversations. Superstar gigs/rewards and original-executable parity remain experimental and need gameplay verification.

Weather sidecars are replaced atomically, but the game's whole multi-file save operation is not transactional. Keep this profile disposable. Do not copy its saves into your normal game. Report the action, lot, Sim/gig, expected versus actual behavior, any error text/screenshots and the Superstar diagnostics log when relevant.
