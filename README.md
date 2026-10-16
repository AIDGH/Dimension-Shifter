# Dimension Shifter

A chunk-based 2D action platformer built with Unity 6000.5.0f1 (URP 2D).

Open this folder in Unity Hub using the version listed in `ProjectSettings/ProjectVersion.txt`.

- `Assets/Scenes/SampleScene.unity`: original pixel environment with the current movement and background fixes.
- `Assets/Scenes/PaintedDimension.unity`: separate first pass of the painted stone environment, with eight painted chunk prefabs; original stones, crystals, trees, wood and bridge props are preserved. Press Play to generate the level.

Controls: A/D or arrow keys to move, Space to jump/double jump, V for a normal strike (1 damage), B for a dash strike (2 damage). Acceleration can be toggled on PlayerMovement in the Inspector.

This checkpoint includes movement/slope and parallax fixes. Slimes have 3 HP, a temporary health bar on hit, knockback/stun, a hit flash/sound/pause, and a death animation with collider cleanup. Enemy attacks and dimension switching remain for later. Character and slime artwork are still the existing pixel assets.

Development notes are in `DEVELOPMENT_PLAN.md`. Editor validation sources and the painted environment builder are under `Tools/`; run them in an isolated validation project as described in their source headers. Movement validation passed 25 checks; combat regression passed 13 checks and Play Mode combat passed 5 checks. Scenery validation checks all eight chunks, 100 slope ray samples and player slope traversal. Earlier rendered background validation passed nine repeat boundaries. These do not replace a complete playthrough or a standalone build test.

Keep source asset attribution and license files with their assets. Generated painted artwork is documented in `Assets/Art/Painted/README.md`.
