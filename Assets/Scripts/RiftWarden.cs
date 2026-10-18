using UnityEngine;

// Persistent in both worlds. Attacks commit after a warning; the core opens during recovery.
public class RiftWarden : MonoBehaviour
{
    public enum Phase { Dormant, Chase, Windup, Charge, Volley, Exposed, Defeated, Leap, BeamWindup }
    public Phase State { get; private set; }
    public Sprite[] idle,run,attack,stunned,death;
    public SpriteRenderer visual,warning;
    public EnemyProjectile projectilePrefab;
    public Collider2D entrance;
    public GameObject slimePrefab,mushroomPrefab;
    public Material beamMaterial;
    public bool CanContactDamage=>State==Phase.Charge||State==Phase.Leap;
    public int JumpCount { get; private set; }
    public int BeamCount { get; private set; }
    public int SummonCount { get; private set; }
    readonly System.Collections.Generic.List<GameObject> helpers=new System.Collections.Generic.List<GameObject>();
    LineRenderer aimLine;Vector2 beamAim;int attackChoice;
    public Transform arenaLeft,arenaRight;
    public int CoreDimension { get; private set; }=1;
    public bool Engaged=>State!=Phase.Dormant&&State!=Phase.Defeated;
    public int MeleeCount { get; private set; }
    public int ShotCount { get; private set; }
    public bool Enraged=>health!=null&&health.CurrentHealth<=health.maxHealth/2;
    EnemyHealth health;Rigidbody2D body;PlayerHealth player;
    float timer,stateTime,nextHit,lockedDirection;int cycle;Vector3 home;
    void Awake(){health=GetComponent<EnemyHealth>();body=GetComponent<Rigidbody2D>();home=transform.position;}
    void Start(){player=FindAnyObjectByType<PlayerHealth>();if(entrance!=null)entrance.enabled=false;}
    public bool AcceptHit()
    {
        if(State!=Phase.Exposed||DimensionController.Instance.Current!=CoreDimension||Time.time<nextHit)return false;
        nextHit=Time.time+.28f;return true;
    }
    void Enter(Phase phase,float duration)
    {
        State=phase;timer=duration;stateTime=0;if(aimLine!=null)aimLine.enabled=phase==Phase.BeamWindup;if(warning!=null)warning.enabled=phase==Phase.Windup||phase==Phase.BeamWindup;
        var shape=GetComponent<BoxCollider2D>();float height=phase==Phase.Exposed?1.3f:2.25f;shape.size=new Vector2(1.65f,height);shape.offset=new Vector2(0,height*.5f);
    }
    void FixedUpdate()
    {
        if(player==null)return;
        if(health.IsDead)
        {
            if(State!=Phase.Defeated){ClearHelpers();ClearBeams();Enter(Phase.Defeated,10);if(entrance!=null)entrance.enabled=false;if(DemoSession.Instance!=null)DemoSession.Instance.BossDefeated=true;RunProgress.Instance.ShowMessage("WARDEN DEFEATED · the final sanctuary is open.");}
            return;
        }
        if(player.IsDead||(State!=Phase.Dormant&&(player.transform.position.x<arenaLeft.position.x-2||player.transform.position.x>arenaRight.position.x+2)))
        {ResetEncounter();return;}
        if(DemoSession.Instance!=null&&!DemoSession.Instance.Started)return;
        if(State==Phase.Dormant)
        {
            if(player.transform.position.x>arenaLeft.position.x+2&&player.transform.position.x<arenaRight.position.x&&Mathf.Abs(player.transform.position.y-home.y)<5)
            {if(entrance!=null)entrance.enabled=true;cycle=0;Enter(Phase.Chase,2.3f);RunProgress.Instance.ShowMessage("RIFT WARDEN · evade its strike, then hit the glowing core in the matching world.");}
            return;
        }
        timer-=Time.fixedDeltaTime;stateTime+=Time.fixedDeltaTime;
        float delta=player.transform.position.x-body.position.x;
        switch(State)
        {
            case Phase.Chase:
                Move(Mathf.Abs(delta)>2.5f?Mathf.Sign(delta)*(Enraged?4.8f:3.8f):0);
                if(Mathf.Abs(delta)> .2f)lockedDirection=Mathf.Sign(delta);
                if(timer<=0||Mathf.Abs(delta)<2.6f){Move(0);attackChoice=Random.Range(0,3);
                    if(attackChoice==2){beamAim=((Vector2)player.GetComponent<Collider2D>().bounds.center-((Vector2)transform.position+Vector2.up*1.4f)).normalized;Enter(Phase.BeamWindup,2f);GameAudio.Play("laser_charge",.45f);}
                    else Enter(Phase.Windup,attackChoice==1?1.05f:Enraged?.7f:.95f);}
                break;
            case Phase.Windup:
                if(warning!=null)warning.color=Color.Lerp(new Color(1,.45f,.2f),Color.white,Mathf.PingPong(Time.time*6,1));
                if(timer<=0)
                {
                    if(attackChoice==1){JumpCount++;Enter(Phase.Leap,2f);body.linearVelocity=new Vector2(lockedDirection*5.5f,10);GameAudio.Play("boss_jump",.5f);}
                    else Enter(Phase.Charge,.65f);
                }
                break;
            case Phase.Charge:
                Move(lockedDirection*(Enraged?9f:7f));
                // Contact damage is resolved by the real body collider, never an invisible forward box.
                if(timer<=0){MeleeCount++;Move(0);FireFan();Enter(Phase.Volley,.9f);}
                break;
            case Phase.Volley:
                if(timer<=0){CoreDimension=Random.Range(0,2);cycle++;MaybeSummon();Enter(Phase.Exposed,Enraged?3.6f:4.1f);RunProgress.Instance.ShowMessage(CoreDimension==1?"VIOLET CORE exposed · Q to Echo, then strike or cast!":"BLUE CORE exposed · Q to Frost, then strike or cast!");}
                break;
            case Phase.Leap:
                Move(lockedDirection*5.5f);
                if((stateTime>.25f&&Landed())||timer<=0)
                {Move(0);MeleeCount++;GameAudio.Play("boss_land",.6f);FireFan();Enter(Phase.Volley,.7f);}
                break;
            case Phase.BeamWindup:
                Move(0);
                var origin=(Vector2)transform.position+new Vector2(lockedDirection*.8f,1.4f);
                if(timer>.65f)beamAim=((Vector2)player.GetComponent<Collider2D>().bounds.center-origin).normalized;
                if(aimLine==null)
                {var g=new GameObject("Beam aim warning");g.transform.SetParent(transform);aimLine=g.AddComponent<LineRenderer>();aimLine.sharedMaterial=warning!=null?warning.sharedMaterial:beamMaterial;aimLine.positionCount=2;aimLine.startWidth=aimLine.endWidth=.045f;aimLine.sortingOrder=45;}
                aimLine.enabled=true;aimLine.startColor=aimLine.endColor=new Color(1,.65f,.2f,.55f+.4f*Mathf.PingPong(Time.time*4,1));
                aimLine.SetPosition(0,origin);aimLine.SetPosition(1,origin+beamAim*18);
                if(timer<=0)
                {var g=new GameObject("Warden energy lance");var beam=g.AddComponent<WardenBeam>();beam.Launch(origin+beamAim*1.45f,beamAim,beamMaterial,this);BeamCount++;GameAudio.Play("laser_fire",.65f);Enter(Phase.Volley,.65f);}
                break;
            case Phase.Exposed:
                Move(0);if(timer<=0)Enter(Phase.Chase,Enraged?1.6f:2.3f);
                break;
        }
    }
    readonly RaycastHit2D[] floorHits=new RaycastHit2D[8];
    bool Landed()
    {
        if(body.linearVelocity.y>.1f)return false;
        var filter=new ContactFilter2D();filter.SetLayerMask((1<<8)|(1<<9));filter.useTriggers=false;
        int count=GetComponent<Collider2D>().Cast(Vector2.down,filter,floorHits,.08f);
        for(int i=0;i<count;i++)if(floorHits[i].normal.y>.6f)return true;
        return false;
    }
    void Move(float speed)
    {
        if((body.position.x<arenaLeft.position.x+1.5f&&speed<0)||(body.position.x>arenaRight.position.x-1.5f&&speed>0))speed=0;
        body.linearVelocity=new Vector2(speed,body.linearVelocity.y);
    }
    void FireFan()
    {
        if(projectilePrefab==null)return;
        Vector2 origin=(Vector2)transform.position+new Vector2(lockedDirection,1.6f);Vector2 aim=((Vector2)player.GetComponent<Collider2D>().bounds.center-origin).normalized;
        int count=Enraged?5:3;
        for(int i=0;i<count;i++)
        {var shot=Instantiate(projectilePrefab,origin,Quaternion.identity);shot.speed=Enraged?7:5.5f;shot.damage=20;shot.lifetime=4;shot.Launch(Quaternion.Euler(0,0,(i-(count-1)*.5f)*17)*aim,health);ShotCount++;}
        GameAudio.Play("dash",.5f);
    }
    void MaybeSummon()
    {
        helpers.RemoveAll(g=>g==null||g.GetComponentInChildren<EnemyHealth>(true)==null||g.GetComponentInChildren<EnemyHealth>(true).IsDead);
        if(cycle<2||helpers.Count>0||Random.value>.45f||slimePrefab==null||mushroomPrefab==null)return;
        for(int world=0;world<2;world++)
        {
            float x=Mathf.Clamp(body.position.x+(world==0?-7:7),arenaLeft.position.x+3,arenaRight.position.x-3);
            if(Mathf.Abs(x-player.transform.position.x)<3)x=world==0?arenaLeft.position.x+3:arenaRight.position.x-3;
            var floor=Physics2D.Raycast(new Vector2(x,home.y+3),Vector2.down,8,1<<8);if(floor.collider==null)continue;
            var anchor=new GameObject("Warden helper "+world);anchor.transform.position=new Vector3(x,floor.point.y);
            var child=Instantiate(world==0?slimePrefab:mushroomPrefab,anchor.transform);child.transform.localPosition=Vector3.up*(world==0?.48f:0);
            var ai=child.GetComponent<EnemyAttackAI>();ai.windup=Mathf.Max(ai.windup,.9f);
            var actor=anchor.AddComponent<DimensionActor>();actor.dimension=world;actor.content=child;actor.Apply(DimensionController.Instance.Current);helpers.Add(anchor);SummonCount++;
        }
        RunProgress.Instance.ShowMessage("Reinforcements in BOTH worlds · clear them or focus the Warden!");
    }
    void ClearHelpers(){foreach(var g in helpers)if(g!=null)Destroy(g);helpers.Clear();}
    void ClearBeams(){foreach(var beam in FindObjectsByType<WardenBeam>())if(beam.Owner==this)Destroy(beam.gameObject);}
    void OnDestroy(){ClearHelpers();ClearBeams();}
    void ResetEncounter()
    {
        if(State==Phase.Dormant)return;
        ClearHelpers();ClearBeams();nextHit=0;body.position=home;body.linearVelocity=Vector2.zero;health.ResetForRetry();Enter(Phase.Dormant,0);if(entrance!=null)entrance.enabled=false;
        foreach(var shot in FindObjectsByType<EnemyProjectile>())Destroy(shot.gameObject);
    }
    public void ResetForPractice()
    {
        ClearHelpers();ClearBeams();nextHit=0;cycle=0;
        body.position=home;body.linearVelocity=Vector2.zero;health.ResetForRetry();
        Enter(Phase.Dormant,0);if(entrance!=null)entrance.enabled=false;
    }
    void LateUpdate()
    {
        if(State==Phase.Defeated||State==Phase.Dormant)stateTime+=Time.deltaTime;
        var frames=State==Phase.Chase?run:State==Phase.Windup||State==Phase.Charge||State==Phase.Leap||State==Phase.BeamWindup?attack:State==Phase.Exposed?stunned:State==Phase.Defeated?death:idle;
        if(visual!=null&&frames!=null&&frames.Length>0)
        {
            int frame=State==Phase.Windup?Mathf.Min(2,Mathf.FloorToInt(stateTime*4)):State==Phase.Charge?Mathf.Min(frames.Length-1,3+Mathf.FloorToInt(stateTime*10)):State==Phase.Defeated?Mathf.Min(frames.Length-1,Mathf.FloorToInt(stateTime*8)):Mathf.FloorToInt(stateTime*8)%frames.Length;
            visual.sprite=frames[frame];visual.flipX=lockedDirection>0;
            // Feet are aligned from the imported opaque sprite bounds, independently of collision.
            if(State!=Phase.Defeated)visual.color=State==Phase.Exposed?(CoreDimension==1?new Color(1,.65f,1):new Color(.55f,1,1)):Color.white;
        }
    }
}
