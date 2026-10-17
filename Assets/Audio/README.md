# Game audio

GameAudio loads named cues from Resources/SFX, varies pitch slightly, and uses a bounded eight-voice pool. Its Master Volume controls effects. WorldMusic controls the two music stems; M toggles music during play.

## Original procedural audio

Tools/Polish/ComposeAudio.py creates the WAV cues dash, jump, air_jump, hurt, player_death, shift, pickup and checkpoint, plus the harmonically aligned 32-second Frost/Echo music loops. These are original synthesized sounds composed for this project, with no sampled recording or third-party music. Dash combines airy noise with a separate existing knife-swish cue. Music crossfades when dimensions change; the tracks share their playback clock. The music importer uses streaming Vorbis.

Regenerate with Python 3 and NumPy by running the script from the project root. Keep Unity .meta files when replacing a cue. Runtime cue names must match the resource filenames, for example dash_0.wav.

## Licensed sound effects

The remaining swing, enemy impact/death, landing and snow/concrete/wood footstep cues are selected from Kenney RPG Audio and Impact Sounds (CC0). Original licenses are in Licenses/. Sources: https://kenney.nl/assets/rpg-audio and https://kenney.nl/assets/impact-sounds . These packs also remain the source of the knife-swish layered onto dash.
