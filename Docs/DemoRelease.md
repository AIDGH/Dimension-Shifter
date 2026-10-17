# The Rift Warden demo — 2026-10-17

## Implemented progression

Both terrain styles now use a deterministic ten-encounter route: Leap Garden → Crystal Ascent → Key Sanctuary → Hollow Descent → Spore Pass → Rift Crossing → Twin Seals → Rift Vault → Proving Grounds → Warden Arena → final sanctuary. Existing start/end pieces and the original random chunk library are retained. The camera limit derives from the authored route length.

The key only exists for collection in Echo. A nearby E interaction opens the blue doorway only in Frost and only with the key, teleporting into an enclosed room above the trail. Claiming the altar reward unlocks C, enables the return portal and opens the route gate. Spirit bolts carry the casting dimension, stop at solid terrain, deal two enemy damage, and activate each matching relay once. The two relays are on separate platform sequences; both are required for progress. These discoveries survive checkpoint returns.

The proving grounds seals behind the player and spawns three waves containing 3, 4 and 5 opponents split across both worlds. The HUD lists survivors per world. Defeating all waves opens its exit and heals 35 HP. Death clears the current attempt and releases the entrance for a fresh three-wave attempt.

The Rift Warden has 64 HP and persists in both worlds. It chases, telegraphs a heavy melee charge, fires a three-bolt fan, then exposes its core for 4.1 seconds. Exposed color alternates each cycle; the player must match it to damage the core. Hits have a 0.28-second duplicate-hit guard. Below half health, pursuit/charge speed rises, warning time falls from 0.95 to 0.70 seconds, the fan grows to five bolts, and recovery becomes 3.6 seconds. Death restores the boss and releases the arena entrance; victory opens the final gate. The in-run checkpoint preserves the weapon and puzzle state for retries.

The original opening-scene slime is removed, not merely repositioned. Existing enemy telegraphs use a native exclamation symbol. Falling onto a mushroom's head deals one damage, rebounds the player and grants 0.18 seconds of contact grace; side contact retains its normal damage. Boss health is shown in the dedicated HUD panel rather than a second floating bar.

## Presentation

Start menu with pointer/Enter controls; active-play timer and current objective; wave survivor counts; boss vitality/armor/core cues; outcome screen with time, falls, score and restart/checkpoint buttons. Start, death and completion do not advance the journey timer. Restart returns to the start menu and clears quest/reward state. No profile, disk save, inventory or settings suite is introduced in this milestone.

## Pacing

First-play target: approximately 7–8 minutes. Design allocation is about 1–1.5 minutes introductory travel/combat, 1–1.5 minutes key/sanctuary exploration, 1–1.5 minutes traversal and relay puzzles, about a minute in the three-wave trial, and 1.5–2.5 minutes for the boss and finale. There is no artificial timer gate; experienced players or shortcuts can finish faster. These are design estimates, not an observed minimum. A fresh-player playthrough is still required to tune the target against actual elapsed time.

## Validation

- DemoPlayChecks: 49 passing checks in real Unity Play Mode, including menu/pause behavior, the complete key/door/portal/weapon/relay dependency chain, a physical stomp, projectile damage and wall occlusion, preserved progression on respawn, three waves across both worlds, trial reset after death, boss pursuit/melee/fans, armor/wrong-world rejection, phase-two fan, boss reset after death, victory gating and fresh-run restart.
- DemoLayoutChecks: both scenes generate all ten encounters and the finale within camera coverage; all enemy warnings reference the new symbol; 1,136 floor samples across four new chunks in both art variants; mandatory gates exceed jump height; actual double-jump traversal on required steps with acceleration on/off. The complete report has 1,256 assertions including route/warning and traversal checks. Two initially over-wide gaps were shortened after these movement tests failed.
- MovementRegressionChecks: 25 passes. CombatRegressionChecks: 13 passes; scene wiring now verifies enemies in generated encounters because the standalone opening slime was intentionally removed.
- Rendered start menu, hidden sanctuary, exposed Warden and completion UI at 1600×900. Sprite sheets are sliced with the installed Unity sprite data provider and foot-aligned pivots; the first automatic center-pivot import was corrected after visual inspection.

Reports and previews are under the enclosing workspace's `outputs/dimension-shifter/demo-v1-2026-10-17`. Automated Play Mode tests relocate players and apply test damage to exercise systems quickly; their short timer screenshots are not playthrough duration measurements. No full manual keyboard playthrough or Windows runtime test has been performed. Unity's unrelated Editor.Search startup exception is excluded by editor-stack filtering; game exceptions fail the checks.

Standalone startup revealed two oversized boss textures; their runtime imports are capped at 8192 while retaining original-resolution sprite slices and foot pivots. Windows x64 and macOS Intel/Apple Silicon standalone builds succeeded. A 12-second headless macOS startup check is recorded separately; it does not verify interactive input, sound or rendering. Build results are recorded alongside the reports. `DemoBuildChecks.Windows` and `.Mac` build the painted scene first and the original-style scene second, with Mono and a 1600×900 window. Runtime source and generated asset references are checked during integration.

## Separate Hollow Edition

The demo is frozen as a source snapshot before creating the separate Hollow Edition Unity project. That copy includes all source assets, Packages, ProjectSettings, tools and documentation; generated Library/Temp caches are intentionally regenerated by Unity. The larger Java-map/character/environment migration belongs in that copy, preserving this demo and the older Original backup.
