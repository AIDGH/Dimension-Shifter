using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAttack : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;
    private Collider2D body;
    private PlayerMovement pm;
    private readonly RaycastHit2D[] dashHits = new RaycastHit2D[16];
    private bool isAttacking;
    private float attackElapsed;
    private int attackDamage;
    private readonly HashSet<EnemyHealth> struck = new HashSet<EnemyHealth>();
    private readonly Collider2D[] targets = new Collider2D[32];
    [Header("Blade hit window")]
    public int normalDamage = 1;
    public int dashDamage = 2;
    public LayerMask enemyLayers = 1 << 10;
    public LayerMask attackBlockingLayers = (1 << 8) | (1 << 9);
    public Vector2 hitboxSize = new Vector2(1.1f, 1f);
    public Vector2 hitboxOffset = new Vector2(.65f, -.05f);
    public float activeFrom = .0833f;
    public float activeUntil = .5f;
    public bool IsDashing => dashOwnsMovement;
    public float DashCooldownRemaining => Mathf.Max(0,nextDashTime-Time.time);
    private PlayerEnemyContact contact;
    private bool dashOwnsMovement;
    private bool previousMovementEnabled;
    private float previousGravity;

    [Header("Normal Attack (V)")]
    [Min(0.1f)] public float normalAttackRate = 2f;
    [Min(0.01f)] public float normalAttackDuration = 0.6667f;
    private float nextNormalAttackTime;

    [Header("Dash Attack (B)")]
    [Min(0f)] public float dashForce = 16f;
    [Min(0.01f)] public float dashAttackDuration = 0.45f;
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
        contact = GetComponent<PlayerEnemyContact>();
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
        attackElapsed = 0f; attackDamage = normalDamage; struck.Clear();
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.SetBool("IsAttacking", true);
            anim.SetTrigger("Attack");
        }
    }

    IEnumerator NormalAttackRoutine()
    {
        BeginAttack();
        GameAudio.Play("swing", .55f);
        // Keep normal movement, jumping and gravity under the movement controller.
        yield return new WaitForSeconds(Mathf.Max(0.01f, normalAttackDuration));
        FinishAttack();
    }

    IEnumerator DashAttackRoutine()
    {
        BeginAttack();
        attackDamage = dashDamage;
        previousGravity = rb.gravityScale;
        previousMovementEnabled = pm != null && pm.enabled;
        dashOwnsMovement = true;
        if(contact != null)contact.BeginDash();
        GameAudio.Play("dash", .6f);
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
            float speed = Mathf.Lerp(dashForce, dashForce * .35f, elapsed / duration);
            if (DashBlocked(facing, speed * Time.fixedDeltaTime)) speed = 0f;
            rb.linearVelocity = new Vector2(facing * speed, 0f);
            yield return physicsTick;
            elapsed += Time.fixedDeltaTime;
        }
        FinishAttack();
    }

    void FixedUpdate()
    {
        if (!isAttacking) return;
        attackElapsed += Time.fixedDeltaTime;
        if (dashOwnsMovement || (attackElapsed >= activeFrom && attackElapsed < activeUntil)) ResolveHits();
    }

    private void ResolveHits()
    {
        float facing = transform.lossyScale.x < 0 ? -1f : 1f;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.flipX) facing *= -1f;
        Vector2 origin = body != null ? (Vector2)body.bounds.center : rb.position;
        Vector2 center = origin + new Vector2((dashOwnsMovement ? .3f : hitboxOffset.x) * facing, hitboxOffset.y);
        Vector2 size = dashOwnsMovement ? new Vector2(1.8f, hitboxSize.y) : hitboxSize;
        var filter = new ContactFilter2D(); filter.SetLayerMask(enemyLayers); filter.useTriggers = true;
        int count = Physics2D.OverlapBox(center, size, 0, filter, targets);
        for (int i = 0; i < count; i++)
        {
            var enemy = targets[i].GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || struck.Contains(enemy)) continue;
            // Terrain between the blade and target blocks the strike.
            Vector2 point = targets[i].ClosestPoint(origin);
            var wallFilter = new ContactFilter2D(); wallFilter.SetLayerMask(attackBlockingLayers); wallFilter.useTriggers = false;
            if (Physics2D.Linecast(origin, point, wallFilter, dashHits) > 0) continue;
            if (enemy.TakeDamage(attackDamage, origin, attackDamage == dashDamage ? 4f : 2.8f)) struck.Add(enemy);
        }
    }

    void OnDrawGizmosSelected()
    {
        float facing = transform.lossyScale.x < 0 ? -1f : 1f;
        var sr = GetComponent<SpriteRenderer>(); if (sr != null && sr.flipX) facing *= -1f;
        var collider = GetComponent<Collider2D>();
        Vector3 origin = collider != null ? collider.bounds.center : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(origin + new Vector3(hitboxOffset.x * facing, hitboxOffset.y, 0), hitboxSize);
    }

    private bool DashBlocked(float facing, float distance)
    {
        if (body == null) return false;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(attackBlockingLayers);
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
            if(contact != null)contact.EndDash();
            rb.gravityScale = previousGravity;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (pm != null) pm.enabled = previousMovementEnabled;
            dashOwnsMovement = false;
        }
        isAttacking = false;
        struck.Clear();
        if (anim != null && anim.runtimeAnimatorController != null) anim.SetBool("IsAttacking", false);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        FinishAttack();
        if (cooldownText != null) cooldownText.gameObject.SetActive(false);
    }
}
