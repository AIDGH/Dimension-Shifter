using UnityEngine;

// Persistent in both worlds. Attacks commit after a warning; the core opens during recovery.
public class RiftWarden : MonoBehaviour
{
    public enum Phase { Dormant, Chase, Windup, Charge, Volley, Exposed, Defeated }
    public Phase State { get; private set; }
    public Sprite[] idle,run,attack,stunned,death;
    public SpriteRenderer visual,warning;
    public EnemyProjectile projectilePrefab;
    public Collider2D entrance;
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
        State=phase;timer=duration;stateTime=0;if(warning!=null)warning.enabled=phase==Phase.Windup;
        var shape=GetComponent<BoxCollider2D>();float height=phase==Phase.Exposed?1.3f:2.25f;shape.size=new Vector2(1.65f,height);shape.offset=new Vector2(0,height*.5f);
    }
    void FixedUpdate()
    {
        if(player==null)return;
        if(health.IsDead)
        {
            if(State!=Phase.Defeated){Enter(Phase.Defeated,10);if(entrance!=null)entrance.enabled=false;if(DemoSession.Instance!=null)DemoSession.Instance.BossDefeated=true;RunProgress.Instance.ShowMessage("WARDEN DEFEATED · the final sanctuary is open.");}
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
                if(timer<=0||Mathf.Abs(delta)<2.6f){Move(0);Enter(Phase.Windup,Enraged?.7f:.95f);}
                break;
            case Phase.Windup:
                if(warning!=null)warning.color=Color.Lerp(new Color(1,.45f,.2f),Color.white,Mathf.PingPong(Time.time*6,1));
                if(timer<=0)Enter(Phase.Charge,.65f);
                break;
            case Phase.Charge:
                Move(lockedDirection*(Enraged?9f:7f));
                var center=(Vector2)transform.position+new Vector2(lockedDirection*1.5f,1.2f);
                var target=player.GetComponent<Collider2D>();
                if(target.bounds.Intersects(new Bounds(center,new Vector3(2.8f,2.4f,1))))
                {var contact=player.GetComponent<PlayerEnemyContact>();if(!contact.IsContactProtected)player.TryTakeDamage(25);}
                if(timer<=0){MeleeCount++;Move(0);FireFan();Enter(Phase.Volley,.9f);}
                break;
            case Phase.Volley:
                if(timer<=0){CoreDimension=cycle++%2==0?1:0;Enter(Phase.Exposed,Enraged?3.6f:4.1f);RunProgress.Instance.ShowMessage(CoreDimension==1?"VIOLET CORE exposed · Q to Echo, then strike or cast!":"BLUE CORE exposed · Q to Frost, then strike or cast!");}
                break;
            case Phase.Exposed:
                Move(0);if(timer<=0)Enter(Phase.Chase,Enraged?1.6f:2.3f);
                break;
        }
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
    void ResetEncounter()
    {
        if(State==Phase.Dormant)return;
        body.position=home;body.linearVelocity=Vector2.zero;health.ResetForRetry();Enter(Phase.Dormant,0);if(entrance!=null)entrance.enabled=false;
        foreach(var shot in FindObjectsByType<EnemyProjectile>())Destroy(shot.gameObject);
    }
    void LateUpdate()
    {
        if(State==Phase.Defeated||State==Phase.Dormant)stateTime+=Time.deltaTime;
        var frames=State==Phase.Chase?run:State==Phase.Windup||State==Phase.Charge?attack:State==Phase.Exposed?stunned:State==Phase.Defeated?death:idle;
        if(visual!=null&&frames!=null&&frames.Length>0)
        {
            int frame=State==Phase.Windup?Mathf.Min(2,Mathf.FloorToInt(stateTime*4)):State==Phase.Charge?Mathf.Min(frames.Length-1,3+Mathf.FloorToInt(stateTime*10)):State==Phase.Defeated?Mathf.Min(frames.Length-1,Mathf.FloorToInt(stateTime*8)):Mathf.FloorToInt(stateTime*8)%frames.Length;
            visual.sprite=frames[frame];visual.flipX=lockedDirection>0;
            // Feet are aligned from the imported opaque sprite bounds, independently of collision.
            if(State!=Phase.Defeated)visual.color=State==Phase.Exposed?(CoreDimension==1?new Color(1,.65f,1):new Color(.55f,1,1)):Color.white;
        }
    }
}
