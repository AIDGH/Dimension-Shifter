using UnityEngine;

// Short real-time hit pause and self-contained, temporary impact effects.
public class CombatFeedback : MonoBehaviour
{
    static CombatFeedback instance;
    static Sprite pixel;
    float resumeAt, previousScale;
    bool ownsPause;
    struct Mote { public SpriteRenderer sprite; public Vector2 velocity; public float life; }
    readonly System.Collections.Generic.List<Mote> motes = new System.Collections.Generic.List<Mote>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; pixel = null; }
    public static SpriteRenderer MakeSprite(string name, Transform parent)
    {
        if (pixel == null) pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = pixel; sr.sortingOrder = 100;
        return sr;
    }
    public static void Hit(Vector3 position, bool lethal)
    {
        if (instance == null) instance = new GameObject("Combat feedback").AddComponent<CombatFeedback>();
        instance.Emit(position, lethal);
    }
    void Emit(Vector3 position, bool lethal)
    {
        if (!ownsPause && Time.timeScale > 0f)
        {
            previousScale = Time.timeScale; ownsPause = true; Time.timeScale = 0f;
        }
        if (ownsPause) resumeAt = Mathf.Max(resumeAt, Time.unscaledTime + (lethal ? .05f : .035f));
        int count = lethal ? 12 : 5;
        for (int i = 0; i < count; i++)
        {
            var sr = MakeSprite("Impact mote", transform); sr.transform.position = position;
            sr.transform.localScale = Vector3.one * (lethal ? .045f : .025f);
            sr.transform.rotation = Quaternion.Euler(0, 0, 45);
            float angle = (i + .3f) * Mathf.PI * 2 / count;
            motes.Add(new Mote { sprite = sr, velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (lethal ? 1.8f : 1.2f), life = lethal ? .5f : .24f });
        }
        GameAudio.Play(lethal ? "slime_death" : "slime_hit", .55f);
    }
    void Update()
    {
        if (ownsPause && Time.unscaledTime >= resumeAt) RestoreTime();
        for (int i = motes.Count - 1; i >= 0; i--)
        {
            var mote = motes[i]; mote.life -= Time.deltaTime;
            if (mote.life <= 0 || mote.sprite == null) { if(mote.sprite != null)Destroy(mote.sprite.gameObject); motes.RemoveAt(i); continue; }
            mote.sprite.transform.position += (Vector3)(mote.velocity * Time.deltaTime);
            mote.velocity += Vector2.down * (2f * Time.deltaTime);
            mote.sprite.color = new Color(.7f, 1f, .88f, Mathf.Clamp01(mote.life / .2f)); motes[i] = mote;
        }
    }
    void RestoreTime() { if (ownsPause && Time.timeScale == 0f && (DemoSession.Instance==null || (DemoSession.Instance.Started&&!DemoSession.Instance.Paused))) Time.timeScale = previousScale; ownsPause = false; }
    void OnDisable() { RestoreTime(); }
}
