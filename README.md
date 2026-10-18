# Dimension Shifter — The Rift Warden demo

A Unity 6000.5.0f1 / URP 2D action platformer built around Frost/Echo dimension shifting.

Open `Assets/Scenes/PaintedDimension.unity` and press Play, then choose **Begin Journey** (Enter). `SampleScene.unity` preserves the original terrain style with the same demo progression. The original random chunk library remains available; the demo follows ten authored encounters so its puzzles and boss cannot be skipped by random generation.

## Controls

| Key | Action |
| --- | --- |
| A/D or arrows | Move |
| Space | Jump / one air jump |
| V | Sword strike |
| B | Phase dash through enemies; 3-second cooldown |
| Q | Switch world; 2-second cooldown |
| E | Use door or return portal; Spirit Bolt collects automatically |
| C | Spirit bolt after finding the sanctuary reward; 1.35-second cooldown |
| M | Toggle music |
| Esc | Pause / resume; pause menu also returns to title (resets the run) |
| R after death | Return to checkpoint |
| N after death | Restart the entire journey |

Start and outcome menus also support mouse buttons. The journey timer counts active gameplay, pauses at the start/death/finish screens and with Esc, and resets on a new run.

## The journey

1. Learn combat in Leap Garden and climb Crystal Ascent.
2. Claim the upper sanctuary key in **Echo**, use it at the **Frost** doorway, enter the elevated hidden room, and walk into Spirit Bolt at the altar to collect it automatically. Use its return portal.
3. Descend through enemy encounters and the dimension barriers.
4. Shoot both seals in their matching world to open the relay gate.
5. Clear three shorter waves of inhabitants across **both** worlds in the proving grounds.
6. Defeat the **Rift Warden**, then reach the final sanctuary.

The Warden has 48 HP, pursuit, a warned heavy charge/melee attack, ranged fans and random blue/violet recovery windows. Only the matching dimension can damage an exposed core. Below half health it pursues faster, warns for less time, and fires five bolts instead of three. The Warden can also jump, aim an energy lance for two seconds before firing, and occasionally summon a limited pair of helpers, one per world. Death resets the trial or boss encounter and opens its entrance for another attempt.

Landing on a mushroom from above deals one damage and rebounds without contact damage. Side contact still hurts. The standalone slime that used to fall into the opening scene is removed. Enemies now show an exclamation mark before attacking.

Checkpoints preserve discovered keys, the weapon, activated seals, score and defeated ordinary enemies **during the current run**. There is no disk-save/profile/inventory system yet. Restarting resets the run.

The first-play pacing target is approximately 7–8 minutes across traversal, discovery, mandatory fights and boss retries; it is a design estimate, not a measured or enforced minimum. Experienced play can finish faster. See `Docs/FinalDemoPolish.md` for the current polish and validation, and `Docs/DemoRelease.md` for the original release.

Editor builders and tests live under `Tools/`, outside runtime imports. Copy `Tools/Demo/DemoSetup.cs`, `Tools/Art/PaintedVariantBuilder.cs`, and desired `Tools/Validation/Editor/*.cs` into an isolated project's `Assets/Editor` to regenerate/test. Do not rerun whole-project builders on current edited scenes or prefabs. Test transformations in an isolated copy and integrate selected files.

Source artwork/audio attribution remains alongside the assets. Demo reference art is documented in `Assets/Art/Demo/PROVENANCE.md`.

## Current collaboration and polish (2026-10-18)

`main` is the chunk-based demo; `hollow-edition` is a separate Unity project/branch. Keep teammate work in `Assets/PrismChallenge` independent; see `Docs/PrismChallenge.md`. Authoring instructions: `Docs/ChunkDesignGuide.fa.md`.

Levels 3/5/7 grant up to +3 blade/dash/cast damage. Dash cooldown decreases by 0.15 seconds per level, capped at 0.6 seconds; movement speed is unchanged. Latest work also fixes slime animation/sensing and adds the painted end tree and dedicated boss lightning material. See `Docs/WorkLog.md` for actual tests and build limitations.
