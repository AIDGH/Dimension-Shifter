using UnityEngine;
using UnityEngine.UI;

public class StoryIntro : MonoBehaviour
{
    Canvas canvas;
    CanvasGroup illustrationFade;
    Image artwork;
    RectTransform backdrop;
    Text title, body, chapter;
    Image[] progress;
    int page;
    float age;
    bool dismissed;
    Font font;
    Sprite[] illustrations;
    Texture2D roundedTexture;
    Sprite rounded;
    static readonly string[] titles = { "THE KEEPER'S APPRENTICE", "THE NIGHT THE WORLD SPLIT", "A WAY HOME" };
    static readonly string[] words = {
        "You grew up beneath the ancient tree, tending its lanterns and listening to the echoes carried by its roots.",
        "One night, a lantern answered from a world that should not exist. You reached for its light — and the rift swallowed the path home.",
        "Frost and Echo now share the same broken forest. Find the lost spirit, overcome the Rift Warden, and bring its sigil to the tree. Your way home waits beyond its roots."
    };

    void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        illustrations = new[] { Resources.Load<Sprite>("Story/01-Forest"), Resources.Load<Sprite>("Story/02-Door"), Resources.Load<Sprite>("Story/03-Tree") };
        rounded = MakeRounded();
        var root = new GameObject("Story prologue", typeof(RectTransform));
        canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 250;
        root.AddComponent<GraphicRaycaster>();
        var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
        var bg = Picture("Blue violet forest backdrop", root.transform, Vector2.zero, new Vector2(1800, 1125), Color.white);
        backdrop = bg.rectTransform; bg.sprite = Resources.Load<Sprite>("Story/Backdrop");
        var veil = Picture("Atmospheric shade", root.transform, Vector2.zero, Vector2.zero, new Color(.018f, .028f, .07f, .45f));
        Stretch(veil.rectTransform);
        chapter = Label("Chapter", root.transform, 18, new Vector2(0, 397), new Vector2(1100, 30), new Color(.48f, .87f, .95f));
        title = Label("Story heading", root.transform, 36, new Vector2(0, 348), new Vector2(1260, 60), Color.white);
        var artRoot = new GameObject("Illustration area", typeof(RectTransform)); artRoot.transform.SetParent(root.transform, false);
        var ar = artRoot.GetComponent<RectTransform>(); ar.sizeDelta = new Vector2(1120, 440); ar.anchoredPosition = new Vector2(0, 85);
        illustrationFade = artRoot.AddComponent<CanvasGroup>();
        artwork = Picture("Story illustration", artRoot.transform, Vector2.zero, new Vector2(1080, 430), Color.white);
        artwork.preserveAspect = true;
        var card = Picture("Story caption panel", root.transform, new Vector2(0, -247), new Vector2(1260, 188), new Color(.025f, .045f, .10f, .92f));
        card.sprite = rounded; card.type = Image.Type.Sliced;
        Picture("Caption accent", card.transform, new Vector2(0, 83), new Vector2(420, 2), new Color(.46f, .79f, .95f, .65f));
        body = Label("Story paragraph", card.transform, 25, new Vector2(0, 8), new Vector2(1120, 136), new Color(.89f, .94f, 1));
        progress = new Image[3];
        for (int i = 0; i < 3; i++) progress[i] = Picture("Chapter progress " + i, card.transform, new Vector2((i - 1) * 39, -72), new Vector2(27, 3), Color.white);
        Label("Story controls", root.transform, 18, new Vector2(0, -395), new Vector2(1300, 36), new Color(.76f, .84f, .95f)).text = "SPACE / CLICK  NEXT     ·     ESC  SKIP     ·     ENTER  PLAY";
        Show();
    }

    Image Picture(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var g = new GameObject(name, typeof(RectTransform)); g.transform.SetParent(parent, false);
        var r = g.GetComponent<RectTransform>(); r.sizeDelta = size; r.anchoredPosition = position;
        var image = g.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
    }
    Text Label(string name, Transform parent, int size, Vector2 position, Vector2 bounds, Color color)
    {
        var g = new GameObject(name, typeof(RectTransform)); g.transform.SetParent(parent, false);
        var r = g.GetComponent<RectTransform>(); r.sizeDelta = bounds; r.anchoredPosition = position;
        var t = g.AddComponent<Text>(); t.font = font; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
        t.color = color; t.raycastTarget = false; return t;
    }
    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
    Sprite MakeRounded()
    {
        const int n = 64; roundedTexture = new Texture2D(n, n, TextureFormat.RGBA32, false);
        roundedTexture.filterMode = FilterMode.Bilinear; roundedTexture.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float dx = Mathf.Max(0, Mathf.Abs(x - 31.5f) - 19.5f), dy = Mathf.Max(0, Mathf.Abs(y - 31.5f) - 19.5f);
            px[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(12 - Mathf.Sqrt(dx * dx + dy * dy)));
        }
        roundedTexture.SetPixels32(px); roundedTexture.Apply();
        return Sprite.Create(roundedTexture, new Rect(0, 0, n, n), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
    }
    void Show()
    {
        age = 0; chapter.text = "DIMENSION SHIFTER   /   " + (page + 1).ToString("00") + " — 03";
        title.text = titles[page]; body.text = words[page]; artwork.sprite = illustrations[page]; artwork.enabled = artwork.sprite != null;
        for (int i = 0; i < 3; i++) progress[i].color = i == page ? new Color(.45f, .91f, 1) : new Color(.35f, .40f, .56f);
    }
    void Update()
    {
        if (canvas == null || dismissed) return;
        if (DemoSession.Instance == null || DemoSession.Instance.Started) { Dismiss(); return; }
        age += Time.unscaledDeltaTime;
        illustrationFade.alpha = Mathf.Clamp01(age / .7f);
        artwork.rectTransform.localScale = Vector3.one * (1 + Mathf.Clamp01(age / 10) * .035f);
        backdrop.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * .08f) * 18, 0);
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return)) { Dismiss(); return; }
        if ((age > .3f && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))) || age > 10)
        { if (++page >= titles.Length) Dismiss(); else Show(); }
    }
    public void Dismiss() { dismissed = true; if (canvas != null) canvas.gameObject.SetActive(false); }
    void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
        if (rounded != null) Destroy(rounded);
        if (roundedTexture != null) Destroy(roundedTexture);
    }
}
