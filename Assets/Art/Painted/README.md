# Painted environment — first pass

Open Assets/Scenes/PaintedDimension.unity and press Play.
The original scene is Assets/Scenes/SampleScene.unity. Its original chunk prefabs remain under Assets/Prefabs/Chunks.

ColdCavernStone.png is an original bitmap generated with the built-in OpenAI image tool. Prompt summary: seamless edge-to-edge hand-painted cold basalt, slate blue-gray/violet shadows, charcoal and subtle teal accents; no characters, grass, text or border.
The terrain shader maps the texture over meshes derived from the existing tile collision contours. LuminousFern mesh and PaintedFlora shader are original procedural art.
The forest, player and slime remain the project's pre-existing assets; their original attribution and licenses remain with their source folders. No official Hollow Knight art was imported.

The reproducible editor builder is Tools/Art/PaintedVariantBuilder.cs. It runs in an isolated project and generates separate art, prefabs and scene.
