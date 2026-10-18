using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 2f;
    public bool movingRight;
    [Tooltip("The original slime sprite faces left.")]
    public bool spriteFacesRight;
    [Min(0.02f)] public float turnCooldown = 0.15f;

    [Header("Sensors")]
    public Transform ledgeDetector;
    public float groundRayDistance = 1.5f;
    public float wallRayDistance = 0.5f;
    public LayerMask walkableLayers;

    private Rigidbody2D rb;
    Animator animator;
    EnemyHealth health;
    Vector2 lastPosition;
    private Collider2D body;
    private float turnTimer;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[16];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        ApplyFacing();
    }

    void FixedUpdate()
    {
        SyncMotionAnimation();
        turnTimer = Mathf.Max(0f, turnTimer - Time.fixedDeltaTime);
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(walkableLayers);
        filter.useTriggers = false;
        bool grounded = HasGround(filter);
        Vector2 direction = movingRight ? Vector2.right : Vector2.left;
        bool wall = HasWall(direction, filter);
        bool ledge = false;
        if (grounded)
        {
            // Derive the leading edge from the actual collider, independent of artwork mirroring.
            Vector2 sensor = new Vector2(body.bounds.center.x + direction.x * (body.bounds.extents.x + 0.08f), body.bounds.min.y + .15f);
            int count = Physics2D.Raycast(sensor, Vector2.down, filter, hits, groundRayDistance);
            bool groundAhead = false;
            for (int i = 0; i < count; i++)
                if (hits[i].rigidbody != rb && hits[i].normal.y > 0.5f) groundAhead = true;
            ledge = !groundAhead;
        }
        // No repeated flipping while falling or while still touching the previous obstacle.
        if (grounded && turnTimer <= 0f && (wall || ledge))
        {
            movingRight = !movingRight;
            ApplyFacing();
            turnTimer = turnCooldown;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        rb.linearVelocity = new Vector2(wall || ledge ? 0f : (movingRight ? speed : -speed), rb.linearVelocity.y);
    }

    void OnEnable() { lastPosition = transform.position; }

    void SyncMotionAnimation()
    {
        if (animator == null || animator.runtimeAnimatorController == null || (health != null && (health.IsDead || health.IsStunned))) return;
        float travelled = Mathf.Abs(rb.position.x - lastPosition.x);
        lastPosition = rb.position;
        string state = travelled > .001f ? "Slime_Move" : "Slime_Idle";
        if (animator.HasState(0, Animator.StringToHash(state)) && !animator.GetCurrentAnimatorStateInfo(0).IsName(state)) animator.Play(state);
    }

    private bool HasGround(ContactFilter2D filter)
    {
        int count = body.Cast(Vector2.down, filter, hits, 0.08f);
        for (int i = 0; i < count; i++)
            if (hits[i].rigidbody != rb && hits[i].normal.y > 0.5f && hits[i].point.y < body.bounds.center.y)
                return true;
        return false;
    }

    private bool HasWall(Vector2 direction, ContactFilter2D filter)
    {
        // Shape casting covers the whole body, so a misplaced point sensor cannot miss a wall.
        int count = body.Cast(direction, filter, hits, Mathf.Max(0.02f, wallRayDistance));
        for (int i = 0; i < count; i++)
            if (hits[i].rigidbody != rb && Vector2.Dot(hits[i].normal, direction) < -0.5f && hits[i].normal.y < 0.5f)
                return true;
        return false;
    }

    private void ApplyFacing()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (movingRight == spriteFacesRight ? 1f : -1f);
        transform.localScale = scale;
    }

    void OnDrawGizmosSelected()
    {
        if (ledgeDetector == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(ledgeDetector.position, ledgeDetector.position + Vector3.down * groundRayDistance);
        Gizmos.color = Color.blue;
        Vector3 direction = movingRight ? Vector3.right : Vector3.left;
        Gizmos.DrawLine(ledgeDetector.position, ledgeDetector.position + direction * wallRayDistance);
    }
}
