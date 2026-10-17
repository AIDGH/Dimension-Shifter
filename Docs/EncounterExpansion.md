# Playable encounter expansion

Open `Assets/Scenes/PaintedDimension.unity` for the painted terrain, or `SampleScene.unity` for the original tiles. The original six middle chunks and original chunk prefabs remain available.

## Controls

- A/D or arrows: move. Space: jump / air jump.
- V: strike. B: phase dash (3-second cooldown); passes through enemies and spores, but terrain stops it and traps can hurt.
- Q: switch Frost / Echo (2-second cooldown). Cyan belongs to Frost, violet to Echo. Ghost geometry has no solid collision. Switching is refused if solid geometry would appear inside the player.
- R after death: return to the latest checkpoint with full health and brief protection. R on the finish screen restarts the scene.

## New encounters

The generator inserts these in order, alternating with random existing chunks. They are not dependent on a lucky random selection. Camera max X is 260; the old end chunk remains, with an exit beacon near its far end.

1. **Leap Garden**: two slimes commit to a jump after an amber warning, with recovery time. Low stone ledges split the space.
2. **Crystal Ascent**: rises six units through three two-unit steps; an optional upper shelf rewards an extra jump and a dimension shift.
3. **Spore Pass**: two mushroom keepers, cover pillars, and a timed thorn bed. Spores travel along the direction chosen at wind-up, deal 15 damage, expire after four seconds, and stop at terrain. A hit interrupts the keeper's preparation.
4. **Rift Crossing**: alternating dimension seals, dimension platforms, and phased thorn beds. The background shifts toward violet in Echo. No teleport or movement-physics changes are used for a shift.

5. **Hollow Descent**: descends six units, returning the route to its original height, with a high optional relic shelf.

Each encounter begins with a checkpoint. A checkpoint records position and dimension and heals once; death returns there without rebuilding the route or reviving already defeated enemies. Falling far below the checkpoint also returns the player. This is an in-session checkpoint, not a disk save.

Thorns rest 2 seconds, warn for 0.9 seconds, and activate for 0.8 seconds; damage is 20 and uses the existing 2-second player immunity. Enemies use the existing shared health/strike/death system. Mushroom visuals come from the asset pack already in the project. Kenney footsteps, strikes and enemy hits remain; new original jump, dash, hurt and dimension cues and music are documented in `Assets/Audio/README.md`.

## Editing and validation

New assets live in `Assets/Prefabs/Encounters`. Parameters are editable on the enemy, trap, dimension-member, and marker components. The six original random chunks remain in `midChunks`; the five authored encounters are in `encounterChunks`.

`Tools/Expansion/ExpansionSetup.cs` builds the authored prefabs and wires both scenes. Run it in a validation copy by copying it and `Tools/Art/PaintedVariantBuilder.cs` into that copy's `Assets/Editor`, then using `-executeMethod ExpansionSetup.Run`. It rebuilds its own generated encounter assets, so hand edits to those generated assets should be saved elsewhere first.

`ExpansionPlayChecks` exercises real Play Mode collisions, enemy wind-up/interruption, spore damage/cover/dash avoidance, timed damage, dimension overlap rejection, checkpoint respawn and completion. `ExpansionLayoutChecks` checks both scene routes, continuous encounter floors and renders previews. Existing combat and movement regressions cover the shared systems.

## Dimension and presentation polish

Slimes inhabit Frost and mushroom keepers inhabit Echo. A persistent wrapper hides the other dimension's enemies without resetting health; shifting also clears old projectiles. Existing hazards retain their dimension rules. Blue circular shrines replace plant checkpoint markers, and flowing energy seams replace plant decorations on the barriers. Transparent ghost barriers remain traversable.

Kills grant score and XP (slime: 100/25; keeper: 150/35). Floating relics grant their displayed-in-Inspector score/XP once per run. Each 100 XP raises Resonance by one and heals 20 HP, up to maximum; it does not change attack damage. Death/checkpoint return preserves progress, while restarting the scene resets the run.

The new scalable HUD shows five vitality shards, numerical HP, score, XP, dimension, and Q/B cooldowns. M toggles the ambient music. Background layers follow vertical camera movement, and their feathered edges blend into a dark camera background that always covers the viewport.

After the initial expansion, run `Tools/Polish/PolishSetup.cs` in an isolated Unity copy alongside PaintedVariantBuilder to generate the polish assets, vertical chunks and scene wiring. Do not rerun ExpansionSetup after it without rerunning PolishSetup. `PolishPlayChecks` covers dimension populations, cooldown, rewards, checkpoint art, audio references, barrier rendering, vertical floor continuity and rendered previews.
