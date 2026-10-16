# Dimension Shifter

A chunk-based 2D action platformer built with Unity 6000.5.0f1 (URP 2D).

Open this folder in Unity Hub using the version listed in `ProjectSettings/ProjectVersion.txt`.

- `Assets/Scenes/SampleScene.unity`: original pixel environment with the current movement and background fixes.
- `Assets/Scenes/PaintedDimension.unity`: separate first pass of the painted stone environment, including luminous plants and eight painted chunk prefabs. Press Play to generate the level.

Controls: A/D or arrow keys to move, Space to jump/double jump, V for the normal attack animation, B for the dash attack. Acceleration can be toggled on PlayerMovement in the Inspector.

This checkpoint includes movement/slope and parallax fixes. Attack damage, enemy health/death, and dimension switching have not been implemented yet. Character and slime artwork are still the existing pixel assets.

Development notes are in `DEVELOPMENT_PLAN.md`. Editor validation sources and the painted environment builder are under `Tools/`; run them in an isolated validation project as described in their source headers. The last movement validation passed 25 checks; rendered background validation passed nine repeat boundaries. These do not replace a complete playthrough or a standalone build test.

Keep source asset attribution and license files with their assets. Generated painted artwork is documented in `Assets/Art/Painted/README.md`.
