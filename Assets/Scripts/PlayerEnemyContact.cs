using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
[DefaultExecutionOrder(-10)]
public class PlayerEnemyContact : MonoBehaviour
{
    public float contactDamage = 20f;
    public float dashExitGrace = .18f;
    public LayerMask enemyLayers = 1 << 10;
    PlayerHealth health;
    Collider2D[] bodies;
    int[] originalExclusions;
    readonly Collider2D[] overlaps = new Collider2D[32];
    bool phasing, dashing;
    float graceRemaining;
    float stompReady,fallVelocity;
    readonly System.Collections.Generic.HashSet<EnemyHealth> stomped=new System.Collections.Generic.HashSet<EnemyHealth>();
    public int StompCount { get; private set; }
    public bool IsContactProtected => dashing || graceRemaining > 0f;
    void Awake()
    {
        health = GetComponent<PlayerHealth>(); bodies = GetComponents<Collider2D>();
        originalExclusions = new int[bodies.Length];
        for(int i=0;i<bodies.Length;i++) originalExclusions[i] = bodies[i].excludeLayers;
    }
    public void BeginDash()
    {
        dashing = true; phasing = true;
        for(int i=0;i<bodies.Length;i++) bodies[i].excludeLayers = originalExclusions[i] | enemyLayers.value;
    }
    public void EndDash() { if(!dashing)return;dashing = false; graceRemaining = dashExitGrace; }
    void FixedUpdate()
    {
        fallVelocity=GetComponent<Rigidbody2D>().linearVelocity.y;
        var foot=GetComponent<Collider2D>().bounds.min.y;
        stomped.RemoveWhere(e=>e==null||e.IsDead||foot>e.GetComponent<Collider2D>().bounds.max.y+.35f);
        graceRemaining = Mathf.Max(0, graceRemaining - Time.fixedDeltaTime);
        bool touching = false;
        var filter = new ContactFilter2D();filter.SetLayerMask(enemyLayers);filter.useTriggers = false;
        foreach(var body in bodies)
        {
            int count = body.Overlap(filter, overlaps);
            for(int i=0;i<count;i++)
            {
                var enemy = overlaps[i].GetComponentInParent<EnemyHealth>();
                if(enemy == null || enemy.IsDead)continue;
                touching = true; TryContact(enemy);
            }
        }
        // Avoid snapping out of a body if the dash ends inside a slime.
        // Damage immunity still expires after the short exit grace.
        if(phasing && !dashing && !touching) RestoreCollision();
    }
    public bool TryContact(EnemyHealth enemy)
    {
        if(enemy == null || enemy.IsDead || IsContactProtected) return false;
        var boss=enemy.GetComponent<RiftWarden>();if(boss!=null&&!boss.CanContactDamage)return false;
        if(TryStomp(enemy))return false;
        return health.TryTakeDamage(contactDamage);
    }
    public bool TryStomp(EnemyHealth enemy)
    {
        if(enemy==null||enemy.IsDead||stomped.Contains(enemy))return false;
        var ai=enemy.GetComponent<EnemyAttackAI>();
        if(ai==null||ai.kind!=EnemyAttackAI.AttackKind.Spore||Time.time<stompReady)return false;
        var shape=enemy.GetComponent<Collider2D>();var own=GetComponent<Collider2D>();var rb=GetComponent<Rigidbody2D>();
        if((rb.linearVelocity.y>-.5f&&fallVelocity>-.5f)||rb.linearVelocity.y>1f||own.bounds.min.y<shape.bounds.max.y-.22f||Mathf.Abs(own.bounds.center.x-shape.bounds.center.x)>shape.bounds.extents.x+.1f)return false;
        if(!enemy.TakeDamage(1,own.bounds.center,0))return false;
        stomped.Add(enemy);stompReady=Time.time+.25f;graceRemaining=.22f;GetComponent<PlayerMovement>().BounceFromEnemy(11.5f);StompCount++;GameAudio.Play("jump",.45f);return true;
    }
    void OnCollisionEnter2D(Collision2D hit) => TryContact(hit.collider.GetComponentInParent<EnemyHealth>());
    void OnCollisionStay2D(Collision2D hit) => TryContact(hit.collider.GetComponentInParent<EnemyHealth>());
    void RestoreCollision()
    {
        if(bodies == null)return;
        for(int i=0;i<bodies.Length;i++) if(bodies[i]!=null)bodies[i].excludeLayers = originalExclusions[i];
        phasing = false;
    }
    void OnDisable() { dashing=false;graceRemaining=0;fallVelocity=0;stomped.Clear();RestoreCollision(); }
}
