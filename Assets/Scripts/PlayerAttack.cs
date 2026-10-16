using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAttack : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;
    private Collider2D body;
    private PlayerMovement pm;
    private readonly RaycastHit2D[] dashHits = new RaycastHit2D[16];
    private bool isAttacking;
    private bool dashOwnsMovement;
    private bool previousMovementEnabled;
    private float previousGravity;

    [Header("Normal Attack (V)")]
    [Min(0.1f)] public float normalAttackRate = 2f;
    [Min(0.01f)] public float normalAttackDuration = 0.5f;
    private float nextNormalAttackTime;

    [Header("Dash Attack (B)")]
    [Min(0f)] public float dashForce = 10f;
    [Min(0.01f)] public float dashAttackDuration = 0.5f;
    [Min(0f)] public float dashCooldown = 3f;
    private float nextDashTime;

    // Retained for scene compatibility. Animation alignment must not teleport the collider.
    [HideInInspector] public float yLiftOffset = 0.1f;

    [Header("UI References")]
    public Text cooldownText;
    public float textDisplayDuration = 1f;
    private float showTextTimer;

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();
        pm = GetComponent<PlayerMovement>();
        if (cooldownText != null) cooldownText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (cooldownText != null)
        {
            bool show = showTextTimer > 0f && Time.time < nextDashTime;
            cooldownText.gameObject.SetActive(show);
            if (show)
            {
                cooldownText.text = (nextDashTime - Time.time).ToString("F1") + "s";
                showTextTimer -= Time.deltaTime;
            }
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (Time.time < nextDashTime) showTextTimer = textDisplayDuration;
            else if (!isAttacking)
            {
                nextDashTime = Time.time + dashCooldown;
                showTextTimer = 0f;
                StartCoroutine(DashAttackRoutine());
            }
        }
        if (!isAttacking && Time.time >= nextNormalAttackTime && Input.GetKeyDown(KeyCode.V))
        {
            nextNormalAttackTime = Time.time + 1f / Mathf.Max(0.1f, normalAttackRate);
            StartCoroutine(NormalAttackRoutine());
        }
    }

    private void BeginAttack()
    {
        isAttacking = true;
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.SetBool("IsAttacking", true);
            anim.SetTrigger("Attack");
        }
    }

    IEnumerator NormalAttackRoutine()
    {
        BeginAttack();
        // Keep normal movement, jumping and gravity under the movement controller.
        yield return new WaitForSeconds(Mathf.Max(0.01f, normalAttackDuration));
        FinishAttack();
    }

    IEnumerator DashAttackRoutine()
    {
        BeginAttack();
        previousGravity = rb.gravityScale;
        previousMovementEnabled = pm != null && pm.enabled;
        dashOwnsMovement = true;
        if (pm != null) pm.enabled = false;
        rb.gravityScale = 0f;
        float facing = transform.lossyScale.x < 0f ? -1f : 1f;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.flipX) facing *= -1f;
        float duration = Mathf.Max(0.01f, dashAttackDuration);
        float elapsed = 0f;
        WaitForFixedUpdate physicsTick = new WaitForFixedUpdate();
        while (elapsed < duration)
        {
            float speed = Mathf.Lerp(dashForce, 0f, elapsed / duration);
            if (DashBlocked(facing, speed * Time.fixedDeltaTime)) break;
            rb.linearVelocity = new Vector2(facing * speed, 0f);
            yield return physicsTick;
            elapsed += Time.fixedDeltaTime;
        }
        FinishAttack();
    }

    private bool DashBlocked(float facing, float distance)
    {
        if (body == null) return false;
        ContactFilter2D filter = new ContactFilter2D();
        if (pm != null) filter.SetLayerMask(pm.groundLayer);
        filter.useTriggers = false;
        Vector2 direction = Vector2.right * facing;
        int count = body.Cast(direction, filter, dashHits, distance + 0.01f);
        for (int i = 0; i < count; i++)
        {
            if (dashHits[i].rigidbody == rb) continue;
            if (Vector2.Dot(dashHits[i].normal, direction) < -0.1f) return true;
        }
        return false;
    }

    private void FinishAttack()
    {
        if (dashOwnsMovement)
        {
            rb.gravityScale = previousGravity;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (pm != null) pm.enabled = previousMovementEnabled;
            dashOwnsMovement = false;
        }
        isAttacking = false;
        if (anim != null && anim.runtimeAnimatorController != null) anim.SetBool("IsAttacking", false);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        FinishAttack();
        if (cooldownText != null) cooldownText.gameObject.SetActive(false);
    }
}
