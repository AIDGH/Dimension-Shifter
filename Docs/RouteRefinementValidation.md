# Route refinement validation — 2026-10-17

Validated in an isolated Unity 6000.5.0f1 copy, then integrated into both playable scenes. Runtime C# files match the tested copy byte-for-byte. Generated assets differ only in trailing whitespace normalization; their references resolve against project and package metadata. Main project physics remains FixedUpdate. Original random chunk prefabs and their painted meshes are unchanged.

226 assertions passed:

- RouteRefinementChecks: 83. Actual opening-scene slime has an enabled solid collider, remains above generated terrain for eight seconds, and finishes supported by the ground. Checks also cover wall spacing/height, horizontal membrane support and shift-through, shrine recoloring, 48 underside samples across both art variants, death/victory screens, pointer-click checkpoint return, and pointer-click scene restart with reset score.
- VerticalTraversalChecks: 24. All three ascent and descent steps work with the shipped player settings, both art variants, and acceleration on/off.
- ExpansionLayoutChecks: 5. Both scenes generate all six authored encounters and reach the exit; sampled encounter floors are continuous.
- ExpansionPlayChecks: 20. Existing enemy, projectile, trap, dimension overlap, checkpoint and completion checks pass.
- PolishPlayChecks: 56. Dimension inhabitants, cooldown, rewards, audio, shrine/barrier rendering, vertical floors and background coverage pass.
- MovementRegressionChecks: 25; CombatRegressionChecks: 13.

Inspected 1600×900 Play Mode renders of blue/violet shrines, the three-sided relic chamber, continuous sloped underside, and death/victory menus. Raw reports and previews are retained at `outputs/dimension-shifter/route-refinement-2026-10-17` in the enclosing Uni workspace.

These are automated Unity physics/Play Mode checks and rendered inspections, not a manual keyboard playthrough or a standalone release build. Unity’s unrelated Editor.Search startup exception is filtered by its editor stack; other game errors fail the checks. The new menus offer in-session checkpoint return and complete scene restart; they do not add a disk save.
