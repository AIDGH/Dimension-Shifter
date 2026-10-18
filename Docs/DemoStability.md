# Demo 1.0.2 stability pass — 2026-10-18

- Uses a 60 FPS / 60 Hz budget with at most 0.05 seconds of physics catch-up. Previous 120 FPS / 100 Hz configuration increased workload on battery. Immediate movement mode remains immediate; acceleration remains optional. Mac standalone MetalDisplayLink is enabled. These are latency mitigations, not a measured end-to-end keyboard latency fix. Verify the packaged app on battery.
- macOS system power preferences were read, not changed. Battery mode did not report enabled low-power mode during inspection.
- Trial spawn selection reserves separated floor positions in both worlds and searches the opposite side near arena boundaries; clamped candidates cannot merge. Waves remain 2/3/3.
- Ice trap roots use floor +0.64. The opaque idle base is around -0.60, so it clears the surface by about 0.04 units. Six additional traps per route, across four encounter types; keep clear of nearby checkpoints, interactive objects and dimension obstacles.
- Boss lance uses the existing LivingRift shader with a thin cyan crackling core. The warning and 2-second aim, projectile speed, dash immunity and wall shielding remain.
- Twenty supplied Hollow Knight SFX cues now override their legacy equivalents. Quiet jump clips gain about 11 dB; footstep extraction selects an audible impact, including wood/snow surfaces. Music briefly ducks around foreground effects. Runtime tests inspect the clip attached to the actual playing AudioSource.
- The start menu prints the build version so a previous app is distinguishable from the updated build.

## Engine references

[Unity maximumDeltaTime](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Time-maximumDeltaTime.html) controls the number of fixed updates before another frame can run.
[Apple Metal display link](https://developer.apple.com/documentation/metal/achieving-smooth-frame-rates-with-a-metal-display-link) describes pacing for sustainable workloads and input latency. This does not establish the cause of this particular user's delay.
