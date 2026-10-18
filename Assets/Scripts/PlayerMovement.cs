using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float acceleration = 30f;
    public float groundDeceleration = 50f;
    public float airDeceleration = 5f;
    [Range(0f, 1f)] public float landingMomentumCut = 0.8f;

    [Header("Jump Settings")]
    public float jumpForce = 16f;
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;
    [Min(0f)] public float coyoteTime = 0.1f;
    [Min(0f)] public float jumpBufferTime = 0.12f;
    [Min(0)] public int extraAirJumps = 1;
    [Min(1f)] public float fallGravityMultiplier = 1.2f;
    [Min(1f)] public float maxFallSpeed = 30f;

    [Header("Ground Check Settings")]
    public Transform groundCheck; // Kept for existing scene references and gizmos.
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    [HideInInspector] public bool isGrounded;
    [Range(0.01f, 0.2f)] public float groundProbeDistance = 0.08f;
    [Range(0f, 70f)] public float maxSlopeAngle = 50f;
    public bool alignToSlope = true;
    [Min(0f)] public float slopeRotationSpeed = 240f;

    [Header("Movement Feel Settings")]
    [Tooltip("On: accelerate and brake gradually. Off: respond immediately.")]
    public bool useAcceleration = true;

    // Previously serialized but unused settings: retained without cluttering the Inspector.
    [HideInInspector] public float maxSpeed = 5f;
    [HideInInspector] public float accelerationRate = 20f;
    [HideInInspector] public float decelerationRate = 20f;

    private Rigidbody2D rb;
    private Collider2D body;
    private Animator anim;
    private ContactFilter2D groundFilter;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[16];
    private readonly RaycastHit2D[] wallHits = new RaycastHit2D[16];
    private readonly RaycastHit2D[] surfaceHits = new RaycastHit2D[16];
    private float groundDistance;
    private float slopeAngle;
    private Vector2 groundNormal = Vector2.up;
    private float baseGravity;
    private float moveInput;
    private float jumpBufferRemaining;
    private float coyoteRemaining;
    private float groundIgnoreRemaining;
    private int airJumpsRemaining;
    private bool jumpReleased;
    private bool bufferedJumpReleased;
    public float LastInputToPhysicsMs { get; private set; }
    double inputChangedAt;
    bool pendingInputSample;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        baseGravity = rb.gravityScale > 0f ? rb.gravityScale : 4f;
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        rb.rotation = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        airJumpsRemaining = extraAirJumps;
    }

    void Update()
    {
        if(DemoSession.Instance!=null&&DemoSession.Instance.Paused)
        {
            moveInput=0; jumpBufferRemaining=0; jumpReleased=false; bufferedJumpReleased=false;
            return;
        }
        float nextInput = Input.GetAxisRaw("Horizontal");
        if(nextInput != moveInput){inputChangedAt=Time.realtimeSinceStartupAsDouble;pendingInputSample=true;}
        moveInput = nextInput;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferRemaining = jumpBufferTime;
            bufferedJumpReleased = false;
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            jumpReleased = true;
            bufferedJumpReleased = true;
        }
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(moveInput);
            transform.localScale = scale;
        }
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
            anim.SetBool("IsGrounded", isGrounded);
        }
    }

    void FixedUpdate()
    {
        if(pendingInputSample){LastInputToPhysicsMs=(float)((Time.realtimeSinceStartupAsDouble-inputChangedAt)*1000);pendingInputSample=false;}
        float dt = Time.fixedDeltaTime;
        groundIgnoreRemaining = Mathf.Max(0f, groundIgnoreRemaining - dt);
        bool wasGrounded = isGrounded;
        ProbeGround();
        rb.MoveRotation(Mathf.MoveTowardsAngle(rb.rotation,
            isGrounded && alignToSlope ? slopeAngle : 0f, slopeRotationSpeed * dt));
        if (isGrounded)
        {
            coyoteRemaining = coyoteTime;
            airJumpsRemaining = extraAirJumps;
            if (!wasGrounded)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x * landingMomentumCut, rb.linearVelocity.y);
        }
        else coyoteRemaining = Mathf.Max(0f, coyoteRemaining - dt);

        float targetSpeed = moveInput * Mathf.Max(0f, moveSpeed);
        float rate = Mathf.Abs(targetSpeed) > 0.01f
            ? acceleration : (isGrounded ? groundDeceleration : airDeceleration);
        Vector2 tangent = new Vector2(groundNormal.y, -groundNormal.x);
        float currentSpeed = isGrounded ? Vector2.Dot(rb.linearVelocity, tangent) : rb.linearVelocity.x;
        float speed = useAcceleration
            ? Mathf.MoveTowards(currentSpeed, targetSpeed, Mathf.Max(0f, rate) * dt)
            : targetSpeed;

        // A wall must stop horizontal drive instead of holding a rounded body against it.
        if (IsBlockedByWall(speed, dt)) speed = 0f;
        if (isGrounded)
        {
            // Probing ahead is not contact: close the small gap before cancelling gravity.
            if (groundDistance > 0.005f)
                rb.position += Vector2.down * (groundDistance - 0.005f);
            rb.gravityScale = 0f;
            rb.linearVelocity = tangent * speed;
        }
        else
        {
            rb.gravityScale = baseGravity * (rb.linearVelocity.y < 0f ? fallGravityMultiplier : 1f);
            rb.linearVelocity = new Vector2(speed, Mathf.Max(rb.linearVelocity.y, -maxFallSpeed));
        }

        bool canGroundJump = isGrounded || coyoteRemaining > 0f;
        if (jumpBufferRemaining > 0f && (canGroundJump || airJumpsRemaining > 0))
        {
            if (!canGroundJump) airJumpsRemaining--;
            coyoteRemaining = 0f;
            jumpBufferRemaining = 0f;
            groundIgnoreRemaining = 0.1f;
            isGrounded = false;
            rb.gravityScale = baseGravity;
            float launchSpeed = Mathf.Max(0f, jumpForce) / rb.mass;
            if (bufferedJumpReleased) launchSpeed *= jumpCutMultiplier;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, launchSpeed);
            GameAudio.Play(canGroundJump ? "jump" : "air_jump", .4f);
            // A released buffered jump has already been shortened above.
            jumpReleased = false;
        }
        else if (jumpReleased && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
        jumpReleased = false;
        jumpBufferRemaining = Mathf.Max(0f, jumpBufferRemaining - dt);
    }

    public void BounceFromEnemy(float speed)
    {
        groundIgnoreRemaining=.18f;isGrounded=false;coyoteRemaining=0;jumpBufferRemaining=0;
        jumpReleased=bufferedJumpReleased=false;rb.gravityScale=baseGravity;
        rb.linearVelocity=new Vector2(rb.linearVelocity.x,speed);
    }
    private void ProbeGround()
    {
        isGrounded = false;
        groundNormal = Vector2.up;
        groundDistance = 0f;
        slopeAngle = 0f;
        if (groundIgnoreRemaining > 0f || body == null) return;
        groundFilter = new ContactFilter2D();
        groundFilter.SetLayerMask(groundLayer);
        groundFilter.useTriggers = false;
        float minNormalY = Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);
        float reach = body.bounds.extents.y / minNormalY + groundProbeDistance + 0.05f;
        int count = Physics2D.Raycast(body.bounds.center, Vector2.down, groundFilter, surfaceHits, reach);
        for (int i = 0; i < count; i++)
        {
            var hit = surfaceHits[i];
            if (hit.collider == null || hit.rigidbody == rb || hit.distance <= 0f) continue;
            // Require a surface under the centre of the feet. A rock's rounded
            // side cannot support a character whose feet are outside its outline.
            if (hit.normal.y < minNormalY) return;
            var separation = body.Distance(hit.collider);
            if (!separation.isValid || separation.distance > groundProbeDistance) return;
            var enemy=hit.collider.GetComponentInParent<EnemyHealth>();
            if(enemy!=null&&GetComponent<PlayerEnemyContact>().TryStomp(enemy))return;
            groundNormal = hit.normal;
            groundDistance = Mathf.Max(0f, separation.distance) / hit.normal.y;
            int touches = body.Cast(Vector2.down, groundFilter, groundHits, groundProbeDistance);
            for (int j = 0; j < touches; j++)
                if (groundHits[j].collider == hit.collider && groundHits[j].normal.y >= minNormalY) {
                    groundDistance = groundHits[j].distance;
                    break;
                }
            slopeAngle = Mathf.Atan2(-hit.normal.x, hit.normal.y) * Mathf.Rad2Deg;
            isGrounded = true;
            return;
        }
    }

    private Vector2 SurfaceNormal(float offset)
    {
        Vector2 origin = (Vector2)body.bounds.center + Vector2.right * offset;
        float distance = body.bounds.extents.y / Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad)
            + groundProbeDistance + 0.05f;
        int count = Physics2D.Raycast(origin, Vector2.down, groundFilter, surfaceHits, distance);
        for (int i = 0; i < count; i++)
            if (surfaceHits[i].collider != null && surfaceHits[i].rigidbody != rb && surfaceHits[i].distance > 0f)
                return surfaceHits[i].normal;
        return Vector2.zero;
    }

    private bool IsBlockedByWall(float speed, float dt)
    {
        if (Mathf.Abs(speed) < 0.001f) return false;
        // Set the filter here too: ground probing can be skipped just after a jump.
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);
        filter.useTriggers = false;
        Vector2 direction = speed > 0f ? Vector2.right : Vector2.left;
        int count = body.Cast(direction, filter, wallHits, Mathf.Abs(speed) * dt + 0.01f);
        float minNormalY = Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = wallHits[i];
            if (hit.collider == null || hit.rigidbody == rb) continue;
            if (Vector2.Dot(hit.normal, direction) < -0.1f && hit.normal.y < minNormalY)
                return true;
        }
        return false;
    }

    void OnDisable()
    {
        moveInput = 0f;
        jumpBufferRemaining = 0f;
        jumpReleased = false;
        coyoteRemaining = 0f;
        isGrounded = false;
        if (rb != null) rb.gravityScale = baseGravity;
    }
}
