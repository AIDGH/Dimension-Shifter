# Dimension Shifter — Hollow Edition

Independent Unity 6000.5.0f1 preview, branch `hollow-edition`. The original chunk-based demo is maintained on `main`; do not merge entire editions over one another.

Open `Assets/Scenes/HollowGreenpath.unity`, press Play, then Enter. Preview 0.2 contains Greenpath-room1, Greenpath-room2 and BossRoom, Knight animation, Frost Mossfly, Echo Hornhead, key/door/Spirit progression and the Rift Warden. Full traversal balancing remains in progress.

Controls: A/D move, Space jump/air jump, V strike, B phase dash, Q world shift, E door, C Spirit Bolt after collection, M music, Esc pause. Ctrl+Shift+B opens boss practice; Ctrl+F6 advances rooms for testing; F3 shows frame diagnostics.

Power rises at levels 3/5/7 (up to +3 blade/dash/cast damage). Dash cooldown improves by 0.15 seconds per level, capped at 0.6 seconds reduction. Movement and jump speeds do not scale. Progress is saved only within the current run.

Latest work and actual validation: `Docs/WorkLog.md`. Recovery: `Docs/ProjectRecovery-2026-10-18.md`. Persian authoring guide: `Docs/ChunkDesignGuide.fa.md`.

`Assets/PrismChallenge` is the teammate's optional demo challenge. Its current bootstrap requires the demo LevelGenerator; it is retained here for shared history but is not connected to Hollow rooms yet.

Never rerun whole-project legacy builders on this edited scene. Test any transformations in an isolated copy and integrate selected files. Ship asset files with their .meta files; omit Library, Temp, Logs, builds and private PDFs.
