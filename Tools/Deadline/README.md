# Deadline release tooling — 2026-10-18

Run only in an isolated project copy. The shipped scenes/prefabs are already configured. Do not rerun DeadlineSetup/DeadlineFinish on the live project: these are migration tools, not idempotent rebuilders.

For validation, copy DeadlineChecks.cs to Assets/Editor in a scratch project and invoke DeadlineChecks.Run. For builds, copy ReleaseBuild.cs there and invoke MainMac/MainWindows/HollowMac. Preserve existing source/PrismChallenge files. Setup needs the existing PaintedVariantBuilder editor helper for the new canopy mesh only.
