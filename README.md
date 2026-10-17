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
| E | Use door, altar or portal |
| C | Spirit bolt after finding the sanctuary reward; 1.35-second cooldown |
| M | Toggle music |
| R after death | Return to checkpoint |
| N after death | Restart the entire journey |

Start and outcome menus also support mouse buttons. The journey timer counts active gameplay, pauses at the start/death/finish screens, and resets on a new run.

## The journey

1. Learn combat in Leap Garden and climb Crystal Ascent.
2. Claim the upper sanctuary key in **Echo**, use it at the **Frost** doorway, enter the elevated hidden room, and claim Spirit Bolt at the altar. Use its return portal.
3. Descend through enemy encounters and the dimension barriers.
4. Shoot both seals in their matching world to open the relay gate.
5. Clear three waves of inhabitants across **both** worlds in the proving grounds.
6. Defeat the **Rift Warden**, then reach the final sanctuary.

The Warden has 64 HP, pursuit, a warned heavy charge/melee attack, ranged fans and alternating blue/violet recovery windows. Only the matching dimension can damage an exposed core. Below half health it pursues faster, warns for less time, and fires five bolts instead of three. Death resets the trial or boss encounter and opens its entrance for another attempt.

Landing on a mushroom from above deals one damage and rebounds without contact damage. Side contact still hurts. The standalone slime that used to fall into the opening scene is removed. Enemies now show an exclamation mark before attacking.

Checkpoints preserve discovered keys, the weapon, activated seals, score and defeated ordinary enemies **during the current run**. There is no disk-save/profile/inventory system yet. Restarting resets the run.

The first-play pacing target is approximately 7–8 minutes across traversal, discovery, mandatory fights and boss retries; it is a design estimate, not a measured or enforced minimum. Experienced play can finish faster. See `Docs/DemoRelease.md` for validation and limitations.

Editor builders and tests live under `Tools/`, outside runtime imports. Copy `Tools/Demo/DemoSetup.cs`, `Tools/Art/PaintedVariantBuilder.cs`, and desired `Tools/Validation/Editor/*.cs` into an isolated project's `Assets/Editor` to regenerate/test. Run `DemoSetup.Run` on the existing polished project; do not run older expansion builders afterward without rebuilding the demo.

Source artwork/audio attribution remains alongside the assets. Demo reference art is documented in `Assets/Art/Demo/PROVENANCE.md`.
