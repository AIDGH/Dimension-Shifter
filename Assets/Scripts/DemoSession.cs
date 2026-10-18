using UnityEngine;

// Run-only progression. Respawning preserves discoveries; restarting creates a fresh run.
[DefaultExecutionOrder(-100)]
public class DemoSession : MonoBehaviour
{
    public static DemoSession Instance { get; private set; }
    public bool Started { get; private set; }
    public bool Paused { get; private set; }
    public float Elapsed { get; private set; }
    public bool HasEchoKey { get; private set; }
    public bool HasSpirit { get; private set; }
    public int Relays { get; private set; }
    public bool TrialCleared { get; set; }
    public bool BossDefeated { get; set; }
    public int Deaths { get; private set; }
    bool wasDead;
    public string Clock => System.TimeSpan.FromSeconds(Elapsed).ToString(@"mm\:ss");
    public string Objective => !HasEchoKey ? "Find the ECHO KEY in the violet sanctuary" : !HasSpirit ? "Use the key at the blue door · E" : Relays!=3 ? "C  Spirit bolt · awaken both dimension seals" : !TrialCleared ? "Clear the three waves in the proving grounds" : !BossDefeated ? "Defeat the Rift Warden · match its exposed core" : "The rift is open · reach the final sanctuary";
    void Awake()
    {
        Instance=this;Time.timeScale=1;AudioListener.pause=false;
        // A sustainable frame budget avoids a render/physics backlog on battery.
        QualitySettings.vSyncCount=0;Application.targetFrameRate=60;QualitySettings.maxQueuedFrames=1;
        Time.fixedDeltaTime=1f/60f;Time.maximumDeltaTime=.05f;
        GetComponent<SpriteRenderer>().sortingOrder=50;
        if(GetComponent<FrameDiagnostics>()==null)gameObject.AddComponent<FrameDiagnostics>();
    }
    void Start(){GameAudio.Warmup();if(!Started){Time.timeScale=0;GetComponent<PlayerMovement>().enabled=false;GetComponent<PlayerAttack>().enabled=false;}}
    public void Begin()
    {
        if(Started)return;Started=true;Time.timeScale=1;
        GetComponent<PlayerMovement>().enabled=true;GetComponent<PlayerAttack>().enabled=true;
        GetComponent<RunProgress>().ShowMessage("Find the violet key. Q changes worlds; E interacts. Walk into Spirit Bolt to collect it. ESC pauses.");
    }
    public void SetPaused(bool paused)
    {
        if(paused&&(!Started||GetComponent<PlayerHealth>().IsDead||GetComponent<RunProgress>().Completed))return;
        Paused=paused;Time.timeScale=paused?0:1;AudioListener.pause=paused;
    }
    void Update()
    {
        if ((Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl)) &&
            (Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.B))
            StartCoroutine(BossPractice());
        if(Input.GetKeyDown(KeyCode.Escape))SetPaused(!Paused);
        bool dead=GetComponent<PlayerHealth>().IsDead;
        if(dead&&!wasDead)Deaths++;wasDead=dead;
        if(Started&&!Paused&&!dead&&!GetComponent<RunProgress>().Completed)Elapsed+=Time.deltaTime;
    }
    public System.Collections.IEnumerator BossPractice()
    {
        var run=GetComponent<RunProgress>();
        var generator=FindAnyObjectByType<LevelGenerator>();
        if(generator==null||!generator.LoadAuthoredRoute())yield break;
        Begin();SetPaused(false);
        HasEchoKey=HasSpirit=true;Relays=3;TrialCleared=true;BossDefeated=false;
        foreach(var trial in FindObjectsByType<TrialArena>())trial.ResetTrial();
        var boss=generator.RecreatePracticeArena();
        // Wait for newly created actors to initialize before restoring their checkpoint.
        yield return null;
        if(boss==null)yield break;
        run.ResumeForPractice();
        boss.ResetForPractice();
        var position=new Vector3(boss.arenaLeft.position.x+3,boss.transform.position.y+.65f,transform.position.z);
        GetComponent<DimensionController>().SetDimension(0);
        run.SetCheckpoint(position,"Warden practice");run.Respawn();
        run.ShowMessage("BOSS PRACTICE · full health + Spirit Bolt. CTRL + SHIFT + B restarts this fight.");
    }
    public void ClaimKey(){if(HasEchoKey)return;HasEchoKey=true;GetComponent<PlayerRewards>().Collect(150,30);GetComponent<RunProgress>().ShowMessage("ECHO KEY acquired. Return to the blue door and press E.");}
    public void ClaimSpirit(){if(HasSpirit)return;HasSpirit=true;GetComponent<PlayerRewards>().Collect(250,50);GameAudio.Play("pickup",.65f);GetComponent<RunProgress>().ShowMessage("SPIRIT BOLT unlocked · C. Find the return portal, then awaken the two seals.");}
    public void ActivateRelay(int dimension){int bit=1<<dimension;if((Relays&bit)!=0)return;Relays|=bit;GetComponent<PlayerRewards>().Collect(150,30);GetComponent<RunProgress>().ShowMessage(Relays==3?"Both seals awakened. The proving grounds are open.":"One seal awakened. Find the other world's seal.");}
    void OnDestroy(){if(Instance==this){Instance=null;Time.timeScale=1;AudioListener.pause=false;}}
}
