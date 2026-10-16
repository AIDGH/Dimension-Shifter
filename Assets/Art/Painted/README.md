# Painted environment — first pass

Open Assets/Scenes/PaintedDimension.unity and press Play.
The original scene is Assets/Scenes/SampleScene.unity. Its original chunk prefabs remain under Assets/Prefabs/Chunks.

ColdCavernStone.png is an original bitmap generated with the built-in OpenAI image tool. Prompt summary: seamless edge-to-edge hand-painted cold basalt, slate blue-gray/violet shadows, charcoal and subtle teal accents; no characters, grass, text or border.
The terrain shader maps the texture over meshes derived from the existing tile collision contours. LuminousFern mesh and PaintedFlora shader are original procedural art.
The forest, player and slime remain the project's pre-existing assets; their original attribution and licenses remain with their source folders. No official Hollow Knight art was imported.

The reproducible editor builder is Tools/Art/PaintedVariantBuilder.cs. It runs in an isolated project and generates separate art, prefabs and scene.


Scenery restoration: only explicitly identified terrain tiles are resurfaced. Original prop artwork is rendered in a separate child Tilemap where needed, with no duplicate colliders. Painted-only terrain tile copies retain sprite references and disable old terrain collision; serialized polygon paths match the repaired mesh outlines. Frost outlines are emitted only at exposed boundaries across all map layers. Luminous fern assets remain available but are no longer automatically substituted for authored decorations.
