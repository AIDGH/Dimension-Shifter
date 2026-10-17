using UnityEngine;

public class DimensionController : MonoBehaviour
{
    public static DimensionController Instance { get; private set; }
    public int Current { get; private set; }
    public float cooldown = 2f;
    public float CooldownRemaining => Mathf.Max(0, nextShift - Time.time);
    public string LastMessage { get; private set; }
    float nextShift;
    SpriteRenderer[] backgrounds;
    Color[] backgroundColors;
    PlayerHealth player;
    Collider2D shape;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }
    void Awake() { Instance=this; player=GetComponent<PlayerHealth>(); shape=GetComponent<Collider2D>(); }
    void Start()
    {
        var layers=FindObjectsByType<Parallax>(); backgrounds=new SpriteRenderer[layers.Length]; backgroundColors=new Color[layers.Length];
        for(int i=0;i<layers.Length;i++){backgrounds[i]=layers[i].GetComponent<SpriteRenderer>();if(backgrounds[i]!=null)backgroundColors[i]=backgrounds[i].color;}
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q)) TryShift();
        if(backgrounds==null)return;
        for(int i=0;i<backgrounds.Length;i++)if(backgrounds[i]!=null)
        {
            var tint=Current==0?Color.white:new Color(.86f,.48f,1.12f,1);
            backgrounds[i].color=Color.Lerp(backgrounds[i].color,backgroundColors[i]*tint,Time.deltaTime*5);
        }
    }
    public bool TryShift()
    {
        if((DemoSession.Instance!=null&&(!DemoSession.Instance.Started||DemoSession.Instance.Paused)) || player.IsDead || Time.time < nextShift || (RunProgress.Instance != null && RunProgress.Instance.Completed)) return false;
        var members=FindObjectsByType<DimensionMember>();
        foreach(var member in members)
            if(member.WouldOverlap(shape,1-Current)) { LastMessage="Step clear of the ghost barrier before shifting"; if(RunProgress.Instance != null)RunProgress.Instance.ShowMessage(LastMessage); return false; }
        SetDimension(1-Current); nextShift=Time.time+cooldown; LastMessage=""; GameAudio.Play("shift",.5f); return true;
    }
    public void SetDimension(int value)
    {
        Current=Mathf.Clamp(value,0,1);
        foreach(var member in FindObjectsByType<DimensionMember>()) member.Apply(Current);
        foreach(var actor in FindObjectsByType<DimensionActor>()) actor.Apply(Current);
        foreach(var shot in FindObjectsByType<EnemyProjectile>()) Destroy(shot.gameObject);
        Physics2D.SyncTransforms();
    }
    void OnDestroy() { if(Instance==this)Instance=null; }
}
