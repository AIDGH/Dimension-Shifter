# Demo final polish — 2026-10-18

- Nearby door/portal prompt shows E above the object. Prominent cyan objective and gold-accented longer notices. Spirit Bolt is collected on proximity, without E.
- Player sorting order 50 keeps the character visibly in front of doors and checkpoint rings. Existing pause menu remains available with Esc.
- Mushroom bounce clears the movement controller's ground state and jump buffers; the same contact cannot inflict repeat damage. A separated, descending landing can stomp again.
- Warden health 48. Core world uses independent random choices (repeats allowed). Random charge, leap and energy-lance attacks. Lance tracks for 1.35 seconds, locks aim for 0.65 seconds, then fires a fast visible projectile. Real body contact replaces the former invisible forward melee box. Occasional helpers are limited to one pair, split across worlds; retry/victory clears them and beams.
- Trial waves are 2/3/3 enemies, spawned near the player with shorter gaps. Restored ice traps in Leap Garden and Spore Pass, away from checkpoints.
- Selected supplied Hollow Knight sound effects override matching cues with existing sounds as fallback. Shortened/faded clips, mono import and preload at title avoid full footstep-loop overlap and first-action loading.
- 120 FPS cap, one queued render frame, 100 Hz physics, and faster optional acceleration. These reduce configured latency; actual perceived Mac latency still needs device playtesting.
- Selected supplied moss-rock artwork and a generated solid stump replace selected rocks and vertical wood columns while preserving their source collision. The full Hollow Edition migration remains separate.

Validation and packaged builds are recorded in the workspace outputs folder for this release. No manual full-run timing claim is made.

## Verification scope

49 demo progression checks, 22 focused gameplay checks and 5 swept-lance collision checks passed. The lance was checked against terrain shielding, a near miss, a direct 22-damage hit, phase-dash immunity and encounter cleanup. Tests run in an isolated copy; Windows is compiled here, not manually played. Full human run length and perceived input latency are not measured.

The original pixel canopy/crate artwork remains in some areas; this is a partial scenery polish, not the separate full Hollow Edition conversion. AncientStump art and its built-in image generation prompt are documented in AncientStumpArt.md.

35 pause/casting/slime-alignment checks and 1,256 scene layout/traversal assertions also passed after the changes.

## Deliverables

Version `1.0.1-demo`: Windows x64 and macOS universal (Intel + Apple Silicon) builds succeeded with zero build errors. Packages, reports, screenshots and a source archive are stored outside the repository in `outputs/dimension-shifter/demo-v1.0.1-2026-10-18`. The previous release and independent Hollow Edition source copy remain available.
