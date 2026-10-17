using UnityEngine;

// Run-only progression. Respawning preserves discoveries; restarting creates a fresh run.
public class DemoSession : MonoBehaviour
{
    public static DemoSession Instance { get; private set; }
    public bool Started { get; private set; }
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
    void Awake(){Instance=this;Time.timeScale=1;}
    void Start(){if(!Started){Time.timeScale=0;GetComponent<PlayerMovement>().enabled=false;GetComponent<PlayerAttack>().enabled=false;}}
    public void Begin()
    {
        if(Started)return;Started=true;Time.timeScale=1;
        GetComponent<PlayerMovement>().enabled=true;GetComponent<PlayerAttack>().enabled=true;
        GetComponent<RunProgress>().ShowMessage("Find the violet key. Q changes worlds; E interacts. C casts after discovering Spirit Bolt.");
    }
    void Update()
    {
        bool dead=GetComponent<PlayerHealth>().IsDead;
        if(dead&&!wasDead)Deaths++;wasDead=dead;
        if(Started&&!dead&&!GetComponent<RunProgress>().Completed)Elapsed+=Time.deltaTime;
    }
    public void ClaimKey(){if(HasEchoKey)return;HasEchoKey=true;GetComponent<PlayerRewards>().Collect(150,30);GetComponent<RunProgress>().ShowMessage("ECHO KEY acquired. Return to the blue door and press E.");}
    public void ClaimSpirit(){if(HasSpirit)return;HasSpirit=true;GetComponent<PlayerRewards>().Collect(250,50);GetComponent<RunProgress>().ShowMessage("SPIRIT BOLT unlocked · C. Find the return portal, then awaken the two seals.");}
    public void ActivateRelay(int dimension){int bit=1<<dimension;if((Relays&bit)!=0)return;Relays|=bit;GetComponent<PlayerRewards>().Collect(150,30);GetComponent<RunProgress>().ShowMessage(Relays==3?"Both seals awakened. The proving grounds are open.":"One seal awakened. Find the other world's seal.");}
    void OnDestroy(){if(Instance==this){Instance=null;Time.timeScale=1;}}
}
