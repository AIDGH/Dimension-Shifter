# Playable encounter expansion

Open `Assets/Scenes/PaintedDimension.unity` for the painted terrain, or `SampleScene.unity` for the original tiles. The original six middle chunks and original chunk prefabs remain available.

## Controls

- A/D or arrows: move. Space: jump / air jump.
- V: strike. B: phase dash (3-second cooldown); passes through enemies and spores, but terrain stops it and traps can hurt.
- Q: switch Frost / Echo (0.6-second cooldown). Cyan belongs to Frost, violet to Echo. Ghost geometry has no solid collision. Switching is refused if solid geometry would appear inside the player.
- R after death: return to the latest checkpoint with full health and brief protection. R on the finish screen restarts the scene.

## New encounters

The generator inserts these in order, alternating with random existing chunks. They are not dependent on a lucky random selection. Camera max X is 170; the old end chunk remains, with an exit beacon near its far end.

1. **Leap Garden**: two slimes commit to a jump after an amber warning, with recovery time. Low stone ledges split the space.
2. **Spore Pass**: two mushroom keepers, cover pillars, and a timed thorn bed. Spores travel along the direction chosen at wind-up, deal 15 damage, expire after four seconds, and stop at terrain. A hit interrupts the keeper's preparation.
3. **Rift Crossing**: alternating dimension seals, dimension platforms, and phased thorn beds. The background shifts toward violet in Echo. No teleport or movement-physics changes are used for a shift.

Each encounter begins with a checkpoint. A checkpoint records position and dimension and heals once; death returns there without rebuilding the route or reviving already defeated enemies. Falling far below the checkpoint also returns the player. This is an in-session checkpoint, not a disk save.

Thorns rest 2 seconds, warn for 0.9 seconds, and activate for 0.8 seconds; damage is 20 and uses the existing 2-second player immunity. Enemies use the existing shared health/strike/death system. Mushroom visuals come from the asset pack already in the project. Existing Kenney audio cues are reused.

## Editing and validation

New assets live in `Assets/Prefabs/Encounters`. Parameters are editable on the enemy, trap, dimension-member, and marker components. The six original random chunks remain in `midChunks`; the three authored encounters are in `encounterChunks`.

`Tools/Expansion/ExpansionSetup.cs` builds the authored prefabs and wires both scenes. Run it in a validation copy by copying it and `Tools/Art/PaintedVariantBuilder.cs` into that copy's `Assets/Editor`, then using `-executeMethod ExpansionSetup.Run`. It rebuilds its own generated encounter assets, so hand edits to those generated assets should be saved elsewhere first.

`ExpansionPlayChecks` exercises real Play Mode collisions, enemy wind-up/interruption, spore damage/cover/dash avoidance, timed damage, dimension overlap rejection, checkpoint respawn and completion. `ExpansionLayoutChecks` checks both scene routes, continuous encounter floors and renders previews. Existing combat and movement regressions cover the shared systems.
