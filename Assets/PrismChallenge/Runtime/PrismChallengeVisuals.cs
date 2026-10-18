using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DimensionShifter.PrismChallenge
{
    public sealed class PrismChallengeVisuals : MonoBehaviour
    {
        public static readonly Color Cyan = new Color(.32f, .94f, 1f);
        public static readonly Color Violet = new Color(.78f, .48f, 1f);
        public static readonly Color Gold = new Color(1f, .79f, .36f);
        public Material Material { get; private set; }
        public Material SpriteMaterial { get; private set; }
        public Sprite Diamond { get; private set; }
        public Sprite Soft { get; private set; }
        public TMP_FontAsset Font { get; private set; }

        sealed class Mote
        {
            public Transform Transform;
            public SpriteRenderer Sprite;
            public Vector3 Home;
            public float Phase;
        }
        sealed class Spark
        {
            public Transform Transform;
            public SpriteRenderer Sprite;
            public Vector3 Velocity;
            public Color Color;
            public float Life;
        }
        PrismChallengeRoom owner;
        Sprite stone, white;
        readonly List<Texture2D> textures = new List<Texture2D>();
        readonly List<Sprite> sprites = new List<Sprite>();
        readonly List<Mote> motes = new List<Mote>();
        readonly Spark[] sparks = new Spark[64];
        readonly Transform[] runeRoots = new Transform[3];
        readonly SpriteRenderer[] runeGems = new SpriteRenderer[3];
        Transform entranceRing, gateRing;
        SpriteRenderer gateVeil, gateGlow;
        TMP_Text gateText;
        GameObject effectsRoot;
        float animation;
        int sparkCursor;

        public void Initialize(PrismChallengeRoom room)
        {
            owner = room;
            Material = Resources.Load<Material>("PrismChallenge/PrismUnlit");
            SpriteMaterial = Resources.Load<Material>("PrismChallenge/PrismSprites");
            Font = TMP_Settings.defaultFontAsset;
            if (Font == null) Font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            white = TextureSprite(2, 2, (x, y) => Color.white);
            Soft = TextureSprite(64, 64, (x, y) =>
            {
                float r = new Vector2((x - 31.5f) / 31.5f, (y - 31.5f) / 31.5f).magnitude;
                return new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - r), 2));
            });
            Diamond = TextureSprite(64, 64, (x, y) =>
            {
                float distance = Mathf.Abs(x - 31.5f) / 25f + Mathf.Abs(y - 31.5f) / 30f;
                float shade = x < 31.5f ? .73f : 1f;
                return new Color(shade, shade, shade, Mathf.Clamp01((1 - distance) * 20));
            });
            stone = TextureSprite(128, 64, (x, y) =>
            {
                float nx = Mathf.Abs(x - 63.5f) / 63.5f;
                float height = y / 63f;
                float edge = .32f + height * .68f + .035f * Mathf.Sin(x * .34f);
                float alpha = Mathf.Clamp01((edge - nx) * 35f);
                float noise = Mathf.Repeat(Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f, 1);
                float facet = Mathf.Sin(x * .071f + y * .09f) > 0 ? .08f : 0;
                float shade = .28f + .35f * height + noise * .07f + facet;
                if (y > 58) shade += .16f;
                return new Color(shade, shade, shade, alpha);
            });
            effectsRoot = Child("Prism sparks (bounded pool)", transform, Vector3.zero);
            for (int i = 0; i < sparks.Length; i++)
            {
                var sprite = Picture("Spark", effectsRoot.transform, Vector3.zero, Vector2.one * .12f, Gold, Diamond, 65);
                sprite.enabled = false;
                sparks[i] = new Spark { Transform = sprite.transform, Sprite = sprite };
            }
        }

        public GameObject BuildEntrance(Vector3 point)
        {
            var root = Child("Optional crucible entrance", transform, point);
            entranceRing = Ring("Portal filigree", root.transform, new Vector3(0, 1.35f), 1.1f, Gold, .035f);
            Ring("Inner aperture", root.transform, new Vector3(0, 1.35f), .82f, Cyan, .025f);
            Picture("Portal glow", root.transform, new Vector3(0, 1.35f), new Vector2(4, 4), new Color(.35f, .7f, 1, .45f), Soft, 6);
            Picture("Portal heart", root.transform, new Vector3(0, 1.35f), new Vector2(.65f, 1f), Gold, Diamond, 18);
            Text("PRISM CRUCIBLE", root.transform, new Vector3(0, 3.25f), 3.2f, Gold, 6);
            Text("OPTIONAL   /   E", root.transform, new Vector3(0, 2.75f), 2.1f, Cyan, 5);
            Text("3 trials  ·  cosmetic reward", root.transform, new Vector3(0, -.55f), 1.9f, new Color(.72f, .83f, .95f), 7);
            return root;
        }

        public GameObject BuildRoom(Vector3 origin)
        {
            var root = Child("Prism Crucible • runtime room", transform, origin);
            Picture("A midnight pocket between worlds", root.transform, new Vector3(36, 5, 3), new Vector2(130, 65), new Color(.018f, .026f, .063f), white, 0);
            // Broken concentric vaults and hanging shards, all local to this room.
            for (int i = 0; i < 9; i++)
            {
                float x = -4 + i * 10;
                var ring = Ring("Vault arch", root.transform, new Vector3(x, 7, 2), 6.5f, new Color(.18f, .27f, .43f, .32f), .045f);
                ring.localScale = new Vector3(1, 1.4f, 1);
                Picture("Distant prism", root.transform, new Vector3(x + 2, 12 + i % 3, 1), new Vector2(.6f, 2.4f), new Color(.37f, .46f, .67f, .27f), Diamond, 3);
                Picture("Cavern light", root.transform, new Vector3(x, 4, 1), new Vector2(13, 16), new Color(.15f, .3f, .55f, .18f), Soft, 2);
            }

            // Left edge, right edge, top, world (-1 = neutral). The main player is unchanged.
            Island(root.transform, 0, 8, 0, -1, "Threshold");
            Island(root.transform, 10.5f, 15, 1.5f, 0, "Frost stair");
            Island(root.transform, 17, 21.5f, 3, 1, "Echo stair");
            Island(root.transform, 24, 32, 3, -1, "First resting island");
            Island(root.transform, 37, 47, 3, -1, "Second resting island");
            Island(root.transform, 50, 54.5f, 4.5f, 1, "Echo flight");
            Island(root.transform, 57, 61.5f, 6, 0, "Frost flight");
            Island(root.transform, 64, 75, 6, -1, "Prismwake dais");

            Text("THE PRISM CRUCIBLE", root.transform, new Vector3(4, 5.6f), 3.4f, Gold, 13);
            Text("An optional trial between worlds", root.transform, new Vector3(4, 4.8f), 2f, new Color(.65f, .78f, .9f), 12);
            Text("I  /  TWIN ASCENT", root.transform, new Vector3(18, 8), 2.5f, Violet, 12);
            Text("Q changes which steps are real", root.transform, new Vector3(18, 7.3f), 1.9f, Color.white, 12);
            Text("REST  /  SHIFT TO FROST", root.transform, new Vector3(28, 1.5f), 1.8f, Gold, 9);
            Text("II  /  PULSE CROSSING", root.transform, new Vector3(38, 10.4f), 2.5f, Cyan, 13);
            Text("SPACE, then B  ·  wait for quiet", root.transform, new Vector3(38, 9.7f), 1.9f, Color.white, 13);
            Text("III  /  TWO WORLDS, ONE LEAP", root.transform, new Vector3(57, 11.9f), 2.3f, Gold, 18);
            Text("Jump + Q onto ECHO. Rest. Jump + Q onto FROST.", root.transform, new Vector3(57, 11.2f), 1.8f, Color.white, 20);
            Text("SAFE REST", root.transform, new Vector3(43, 1.5f), 1.8f, Gold, 7);
            Text("E  /  RETURN AT ANY TIME", root.transform, new Vector3(4, -1.7f), 1.9f, Cyan, 12);
            Text("PRISMWAKE", root.transform, new Vector3(69, 9.7f), 3.3f, Gold, 10);
            Text("Your light follows you home", root.transform, new Vector3(69, 9f), 1.9f, Color.white, 11);

            Rune(root.transform, 0, new Vector2(19.25f, 4.05f), Violet);
            Rune(root.transform, 1, new Vector2(42, 4.05f), Cyan);
            Rune(root.transform, 2, new Vector2(68, 7.05f), Gold);
            BuildGate(root.transform);

            for (int i = 0; i < 60; i++)
            {
                // Deterministic placement never consumes Unity's shared random sequence.
                var home = new Vector3(-6 + Mathf.Repeat(i * 17.173f, 90), -1 + Mathf.Repeat(i * 5.731f, 15), 0);
                var sprite = Picture("Drifting light", root.transform, home, Vector2.one * (.06f + i % 3 * .025f), i % 2 == 0 ? Cyan : Violet, Soft, 5);
                motes.Add(new Mote { Transform = sprite.transform, Sprite = sprite, Home = home, Phase = i * 1.37f });
            }
            root.SetActive(false);
            return root;
        }

        void Island(Transform parent, float left, float right, float top, int world, string name)
        {
            float width = right - left;
            var root = Child(name, parent, new Vector3((left + right) * .5f, top));
            root.layer = 8;
            var shape = root.AddComponent<BoxCollider2D>();
            shape.size = new Vector2(width, .8f);
            shape.offset = Vector2.down * .4f;
            Color color = world == 0 ? Cyan : world == 1 ? Violet : new Color(.55f, .66f, .85f);
            Picture("Faceted floating stone", root.transform, new Vector3(0, -1.05f), new Vector2(width, 2.1f), color, stone, 10);
            Picture("Luminous landing edge", root.transform, new Vector3(0, -.04f), new Vector2(width, .085f), world < 0 ? Gold : color, white, 12);
            Picture("Underglow", root.transform, new Vector3(0, -.5f), new Vector2(width + 1, 2), new Color(color.r, color.g, color.b, .18f), Soft, 9);
            for (int i = 0; i < 3; i++)
                Picture("Hanging crystal", root.transform, new Vector3((i - 1) * width * .23f, -1.45f - i % 2 * .3f), new Vector2(.22f, .65f + i * .18f), color, Diamond, 11);
            if (world >= 0)
            {
                // DimensionMember caches children in Awake: add it only after the island is complete.
                var member = root.AddComponent<DimensionMember>();
                member.dimension = world;
                member.Apply(0);
                Text(world == 0 ? "FROST" : "ECHO", parent, new Vector3((left + right) * .5f, top - 2.6f), 1.6f, color, 5);
            }
        }

        void Rune(Transform parent, int index, Vector2 point, Color color)
        {
            var root = Child("Prism " + (index + 1), parent, point);
            runeRoots[index] = root.transform;
            Picture("Prism halo", root.transform, Vector3.zero, Vector2.one * 2.2f, new Color(color.r, color.g, color.b, .45f), Soft, 18);
            Ring("Prism orbit", root.transform, Vector3.zero, .57f, new Color(color.r, color.g, color.b, .6f), .023f);
            runeGems[index] = Picture("Prism heart", root.transform, Vector3.zero, new Vector2(.45f, .7f), color, Diamond, 22);
        }

        void BuildGate(Transform parent)
        {
            float x = PrismChallengeRules.GateX;
            var root = Child("Frost pulse gate (no damage)", parent, new Vector3(x, 6));
            gateVeil = Picture("Pulse veil", root.transform, Vector3.zero, new Vector2(.7f, 16), new Color(.3f, .9f, 1, .18f), white, 20);
            gateGlow = Picture("Pulse aura", root.transform, Vector3.zero, new Vector2(3, 17), new Color(.3f, .9f, 1, .35f), Soft, 19);
            gateRing = Ring("Pulse clock", parent, new Vector3(x, 8.5f), .65f, Cyan, .04f);
            gateText = Text("QUIET  /  B", parent, new Vector3(x, 7.4f), 1.9f, Cyan, 6);
            for (int i = 0; i < 6; i++)
                Picture("Gate shard", root.transform, new Vector3(i % 2 == 0 ? -.7f : .7f, -4 + i * 1.3f), new Vector2(.18f, .5f), Cyan, Diamond, 21);
        }

        public void RefreshRunes()
        {
            for (int i = 0; i < runeRoots.Length; i++)
            {
                if (runeRoots[i] == null) continue;
                runeRoots[i].gameObject.SetActive(i >= owner.Run.Runes);
                runeGems[i].color = i == owner.Run.Runes ? (i == 0 ? Violet : i == 1 ? Cyan : Gold) : new Color(.38f, .43f, .6f, .4f);
            }
        }

        void Update()
        {
            if (owner == null) return;
            animation += Time.deltaTime;
            if (entranceRing != null) entranceRing.localRotation = Quaternion.Euler(0, 0, animation * 12);
            if (owner.Inside)
            {
                float phase = PrismChallengeRules.GatePhase(owner.Run.Elapsed);
                bool quiet = phase < PrismChallengeRules.QuietDuration;
                bool warning = !quiet && phase < PrismChallengeRules.QuietDuration + PrismChallengeRules.WarningDuration;
                Color color = quiet ? Cyan : warning ? Gold : new Color(1, .35f, .48f);
                if (gateVeil != null) gateVeil.color = new Color(color.r, color.g, color.b, quiet ? .11f : warning ? .24f + Mathf.PingPong(animation * 2, .24f) : .72f);
                if (gateGlow != null) gateGlow.color = new Color(color.r, color.g, color.b, quiet ? .18f : .48f);
                if (gateRing != null) gateRing.localRotation = Quaternion.Euler(0, 0, -phase / PrismChallengeRules.GatePeriod * 360);
                if (gateText != null)
                {
                    gateText.text = quiet ? "QUIET  /  B" : warning ? "CHARGING" : "WAIT";
                    gateText.color = color;
                }
                for (int i = 0; i < runeRoots.Length; i++)
                    if (runeRoots[i] != null && runeRoots[i].gameObject.activeSelf)
                    {
                        runeGems[i].transform.localPosition = Vector3.up * Mathf.Sin(animation * 2.2f + i) * .12f;
                        runeGems[i].transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(animation + i) * 12);
                    }
                foreach (var mote in motes)
                {
                    mote.Transform.localPosition = mote.Home + new Vector3(Mathf.Sin(animation * .23f + mote.Phase) * .65f, Mathf.Sin(animation * .36f + mote.Phase) * .5f);
                    Color c = mote.Sprite.color;
                    c.a = .25f + .45f * (.5f + .5f * Mathf.Sin(animation + mote.Phase));
                    mote.Sprite.color = c;
                }
            }
            foreach (var spark in sparks)
            {
                if (spark == null || spark.Life <= 0) continue;
                spark.Life -= Time.deltaTime;
                spark.Sprite.enabled = spark.Life > 0;
                spark.Transform.position += spark.Velocity * Time.deltaTime;
                spark.Velocity += Vector3.down * (1.7f * Time.deltaTime);
                var color = spark.Color;
                color.a *= Mathf.Clamp01(spark.Life);
                spark.Sprite.color = color;
            }
        }

        public void Burst(Vector3 position, Color color, int count = 24)
        {
            for (int i = 0; i < Mathf.Min(count, sparks.Length); i++)
            {
                var spark = sparks[sparkCursor++ % sparks.Length];
                float angle = i * 2.399963f;
                spark.Transform.position = position;
                spark.Velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * (1.5f + i % 5 * .55f) + Vector3.up;
                spark.Life = 1.1f + i % 4 * .1f;
                spark.Color = color;
                spark.Sprite.enabled = true;
            }
        }

        public SpriteRenderer Picture(string name, Transform parent, Vector3 point, Vector2 size, Color color, Sprite sprite, int order)
        {
            var go = Child(name, parent, point);
            go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = SpriteMaterial;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        TMP_Text Text(string text, Transform parent, Vector3 point, float size, Color color, float width)
        {
            var go = Child(text, parent, point);
            var label = go.AddComponent<TextMeshPro>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(width, 1);
            label.GetComponent<MeshRenderer>().sortingOrder = 30;
            return label;
        }

        Transform Ring(string name, Transform parent, Vector3 point, float radius, Color color, float width)
        {
            var go = Child(name, parent, point);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = Material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 64;
            line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = color;
            line.sortingOrder = 8;
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2 / 64;
                float r = radius * (i % 8 == 0 ? .94f : 1);
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0));
            }
            return go.transform;
        }

        Sprite TextureSprite(int width, int height, System.Func<int, int, Color> pixel)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var colors = new Color[width * height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) colors[y * width + x] = pixel(x, y);
            texture.SetPixels(colors);
            texture.Apply(false, false);
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 64, 0, SpriteMeshType.FullRect);
            texture.Apply(false, true);
            textures.Add(texture);
            sprites.Add(sprite);
            return sprite;
        }

        static GameObject Child(string name, Transform parent, Vector3 point)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = point;
            return go;
        }

        void OnDestroy()
        {
            foreach (var sprite in sprites) if (sprite != null) Destroy(sprite);
            foreach (var texture in textures) if (texture != null) Destroy(texture);
        }
    }
}
