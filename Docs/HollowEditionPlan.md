# Hollow Edition — independent V2 starting point

Frozen from Dimension Shifter demo commit d663bec (2026-10-17). This folder is a complete source project, not a shortcut to the original. Open it through Unity Hub with Unity 6000.5.0f1. Unity regenerates Library and Temp on first opening. The original demo and its backups remain separate.

## Next implementation phase

1. Build one representative room using the provided art before replacing the full route: coherent character scale, readable collisions, camera bounds, foreground/background depth and animated enemy silhouettes.
2. Convert the Tiled reference maps in `Game/HollowKnight-mini/assets/map` into Unity room scenes: `Greenpath-room1.tmx`, `Greenpath-room2.tmx`, `CrystalPeak.tmx`, and `BossRoom.tmx`. Preserve object-layer spawn and transition coordinates; define explicit entrances, exits and camera boundaries. Translate behavior into C# for this project rather than trying to run Java/libGDX inside Unity.
3. Map the first Greenpath room to the introduction, the second to cross-world key exploration, CrystalPeak to vertical movement and paired seals, and BossRoom to the Warden encounter. Maintain Frost/Echo-specific enemies and gates so dimension switching remains central.
4. Replace the player and environment in a consistent pass using selected supplied sprites and animations; preserve the working movement, combat, stomp, checkpoint and puzzle systems. Import only selected assets and keep their provenance records.
5. Test every transition in both directions and both dimensions, death/respawn, room revisits and progress persistence. Time a fresh-player run before tuning pacing.

This milestone creates the separate baseline only. The Tiled room conversion and complete Hollow Knight visual migration have not been implemented yet. No Java source has been copied.
