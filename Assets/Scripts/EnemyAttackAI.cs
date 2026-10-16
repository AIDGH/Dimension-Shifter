using UnityEngine;

// Telegraph, commit to a direction, then recover. Hurt interrupts the wind-up.
[RequireComponent(typeof(EnemyHealth), typeof(Rigidbody2D))]
public class EnemyAttackAI : MonoBehaviour
{
    public enum AttackKind { Leap, Spore }
    public AttackKind kind;
    public float range = 5.5f, windup = .65f, recovery = 2.2f;
    public float leapSpeed = 5f, leapHeight = 6.5f;
    public EnemyProjectile projectilePrefab;
    public Transform muzzle;
    public SpriteRenderer warning;
    public bool IsWindingUp => winding;
    public int AttackCount { get; private set; }
    EnemyHealth health;
    Rigidbody2D body;
    Collider2D shape;
    PlayerHealth player;
    float timer = 1f;
    bool winding, airborne;
    Vector2 aim;
    readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    void Awake() { health = GetComponent<EnemyHealth>(); body = GetComponent<Rigidbody2D>(); shape = GetComponent<Collider2D>(); }
    void Start() { player = FindFirstObjectByType<PlayerHealth>(); }
    void FixedUpdate()
    {
        if (health.IsDead || health.IsStunned || player == null || player.IsDead) { CancelAttack(); return; }
        bool grounded = Grounded();
        if (airborne) { if (!grounded || body.linearVelocity.y > .1f) return; airborne = false; timer = recovery; Play("Slime_Idle"); }
        timer -= Time.fixedDeltaTime;
        if (winding)
        {
            if (warning != null) { warning.enabled = true; warning.color = Color.Lerp(new Color(1f,.65f,.2f), Color.white, Mathf.PingPong(Time.time * 8, 1)); }
            if (timer > 0) return;
            winding = false; if (warning != null) warning.enabled = false;
            AttackCount++;
            if (kind == AttackKind.Leap)
            {
                body.linearVelocity = new Vector2(Mathf.Sign(aim.x) * leapSpeed, leapHeight);
                airborne = true; Play("Slime_Attack"); GameAudio.Play("jump", .3f);
            }
            else if (projectilePrefab != null)
            {
                Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * .4f;
                var shot = Instantiate(projectilePrefab, origin, Quaternion.identity);
                shot.Launch(aim, health); GameAudio.Play("swing", .25f);
            }
            timer = recovery; return;
        }
        if (grounded) body.linearVelocity = new Vector2(0, body.linearVelocity.y);
        Vector2 source = muzzle != null ? muzzle.position : shape.bounds.center;
        Vector2 delta = (Vector2)player.GetComponent<Collider2D>().bounds.center - source;
        if (timer > 0 || !grounded || delta.magnitude > range || Mathf.Abs(delta.x) < .4f) return;
        var sight = new ContactFilter2D(); sight.SetLayerMask((1 << 8) | (1 << 9)); sight.useTriggers = false;
        if (Physics2D.Linecast(source, source + delta, sight, groundHits) > 0) return;
        aim = delta.normalized;
        winding = true; timer = windup;
        var sr = GetComponentInChildren<SpriteRenderer>(); if (sr != null) sr.flipX = delta.x > 0;
        Play("Slime_Idle");
    }
    bool Grounded()
    {
        var filter = new ContactFilter2D(); filter.SetLayerMask((1 << 8) | (1 << 9)); filter.useTriggers = false;
        int n = shape.Cast(Vector2.down, filter, groundHits, .1f);
        for (int i = 0; i < n; i++) if (groundHits[i].normal.y > .6f) return true;
        return false;
    }
    void Play(string state) { var a = GetComponent<Animator>(); if (a != null && a.HasState(0, Animator.StringToHash(state))) a.Play(state); }
    void CancelAttack() { winding = false; airborne = false; timer = recovery; if (warning != null) warning.enabled = false; }
    void OnDisable() { CancelAttack(); }
}
