# Dimension Shifter

A chunk-based 2D action platformer built with Unity 6000.5.0f1 (URP 2D).

Open this folder in Unity Hub using the version listed in `ProjectSettings/ProjectVersion.txt`.

- `Assets/Scenes/SampleScene.unity`: original pixel environment with the current movement and background fixes.
- `Assets/Scenes/PaintedDimension.unity`: separate first pass of the painted stone environment, with eight painted chunk prefabs; original stones, crystals, trees, wood and bridge props are preserved. Press Play to generate the level.

Controls: A/D or arrow keys to move, Space to jump/double jump, V for a normal strike (1 damage), B for a dash strike (2 damage). Acceleration can be toggled on PlayerMovement in the Inspector.

This checkpoint includes movement/slope and parallax fixes. Slimes have 3 HP, a temporary health bar on hit, knockback/stun, a hit flash/sound/pause, and a death animation with collider cleanup. Enemy attacks and dimension switching remain for later. Character and slime artwork are still the existing pixel assets.

Development notes are in `DEVELOPMENT_PLAN.md`. Editor validation sources and the painted environment builder are under `Tools/`; run them in an isolated validation project as described in their source headers. Movement validation passed 25 checks; combat regression passed 13 checks and Play Mode combat passed 5 checks. Scenery validation checks all eight chunks, 100 slope ray samples and player slope traversal. Earlier rendered background validation passed nine repeat boundaries. These do not replace a complete playthrough or a standalone build test.

Keep source asset attribution and license files with their assets. Generated painted artwork is documented in `Assets/Art/Painted/README.md`.

## Audio, contact damage and chunk variety

Kenney CC0 SFX now cover footsteps (stone/snow/wood), sword swings, dash, jumps, landing, hurt and death. Licenses and source links: `Assets/Audio/README.md`. `GameAudio.masterVolume` adjusts the runtime SFX volume.

Slime contact costs 20 of 100 HP, with 2 seconds of damage invincibility and blinking. Normal melee reaches beyond physical contact. B phases through living enemies and deals 2 damage once; terrain still blocks the dash. The dash lasts 0.45 seconds with a 3-second cooldown and 0.18-second contact grace on exit. Traps remain dangerous during dash. At zero HP, controls stop and R reloads the current scene.

Both scenes have equal mid-chunk weights and weighted selection without replacement. Every fitting chunk gets a turn before reuse; near the end, only chunks fitting the remaining space are eligible. The original Chunk_02 weight of 100 (versus a total 17 for the other five) caused its ~85% selection rate.

Validation for this update: 11 focused audio/contact/chunk checks; actual Play Mode checks for safe melee reach, contact cooldown, passing through a surviving slime, damage resuming and the bounded SFX pool; existing 25 movement checks and 13 combat checks.
