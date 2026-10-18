using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyHealth : MonoBehaviour
{
    [Min(1)] public int maxHealth = 3;
    [Min(0f)] public float hurtDuration = 0.3334f;
    [Min(0f)] public float deathDuration = 0.3334f;
    [Min(0f)] public float healthBarDuration = 1.8f;
    public int rewardScore = 100, rewardXP = 25;
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;
    public bool IsStunned => !IsDead && hurtRemaining > 0f;
    public bool HealthBarVisible => barRoot != null && barRoot.activeSelf;

    Rigidbody2D body;
    EnemyPatrol patrol;
    Animator animator;
    SpriteRenderer sprite;
    Collider2D[] colliders;
    float hurtRemaining, deathRemaining, barRemaining, flashRemaining;
    bool resumePatrol;
    Color originalColor;
    MaterialPropertyBlock flash;
    GameObject barRoot;
    SpriteRenderer barBack, barFill;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        patrol = GetComponent<EnemyPatrol>();
        animator = GetComponent<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        colliders = GetComponentsInChildren<Collider2D>();
        CurrentHealth = Mathf.Max(1, maxHealth);
        if (sprite != null) { originalColor = sprite.color; flash = new MaterialPropertyBlock(); }
        if (Application.isPlaying && GetComponent<RiftWarden>()==null) CreateBar();
    }

    public bool TakeDamage(int damage, Vector2 attackerPosition, float knockback = 3f)
    {
        if (!isActiveAndEnabled || IsDead || damage <= 0) return false;
        var warden=GetComponent<RiftWarden>();
        if(warden!=null&&!warden.AcceptHit())return false;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        if (patrol != null) { if (!IsStunned) resumePatrol = patrol.enabled; patrol.enabled = false; }
        if (IsDead)
        {
            if(Application.isPlaying)
            {
                var rewards=FindAnyObjectByType<PlayerRewards>();
                if(rewards!=null)rewards.EnemyDefeated(rewardScore,rewardXP);
            }
            hurtRemaining = 0f;
            deathRemaining = deathDuration + 0.18f;
            foreach (var collider in colliders) collider.enabled = false;
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            if (barRoot != null) barRoot.SetActive(false);
            Play("Slime_Die");
        }
        else
        {
            hurtRemaining = warden!=null ? 0 : hurtDuration;
            float direction = body.position.x >= attackerPosition.x ? 1f : -1f;
            if(warden==null)body.linearVelocity = new Vector2(direction * knockback, 1.2f);
            Play("Slime_Hurt");
            barRemaining = healthBarDuration;
            if (barRoot != null) barRoot.SetActive(true);
        }
        flashRemaining = 0.075f;
        SetFlash(1f);
        if (Application.isPlaying) CombatFeedback.Hit(transform.position, IsDead);
        return true;
    }

    public void ResetForRetry()
    {
        if(IsDead)return;
        CurrentHealth=maxHealth;hurtRemaining=barRemaining=flashRemaining=0;
        if(barRoot!=null)barRoot.SetActive(false);
    }

    void Update() => Tick(Time.deltaTime);

    void Tick(float dt)
    {
        flashRemaining = Mathf.Max(0, flashRemaining - dt);
        SetFlash(flashRemaining > 0 ? 1f : 0f);
        if (IsDead)
        {
            deathRemaining -= dt;
            if (sprite != null)
            {
                var color = originalColor;
                color.a *= Mathf.Clamp01(deathRemaining / 0.18f);
                sprite.color = color;
            }
            if (deathRemaining <= 0f) Destroy(gameObject);
            return;
        }
        if (hurtRemaining > 0f)
        {
            hurtRemaining -= dt;
            if (hurtRemaining <= 0f)
            {
                if (patrol != null) patrol.enabled = resumePatrol;
                Play(patrol != null && patrol.enabled && Mathf.Abs(body.linearVelocity.x) > .1f ? "Slime_Move" : "Slime_Idle");
            }
        }
        barRemaining = Mathf.Max(0f, barRemaining - dt);
        if (barRoot != null)
        {
            barRoot.SetActive(barRemaining > 0f);
            float alpha = Mathf.Clamp01(barRemaining / 0.35f);
            barBack.color = new Color(0.03f, 0.06f, 0.09f, alpha * 0.9f);
            barFill.color = new Color(0.50f, 0.92f, 0.76f, alpha);
            float fraction = (float)CurrentHealth / maxHealth;
            barFill.transform.localScale = new Vector3(0.76f * fraction, 0.055f, 1f);
            barFill.transform.localPosition = new Vector3(-0.38f + 0.38f * fraction, 0, -0.01f);
        }
    }

    void LateUpdate()
    {
        if (barRoot == null || !barRoot.activeSelf) return;
        float y = colliders.Length > 0 ? colliders[0].bounds.max.y : transform.position.y + 0.5f;
        barRoot.transform.position = new Vector3(transform.position.x, y + 0.22f, transform.position.z);
    }
    void Play(string state)
    {
        if (animator != null && animator.runtimeAnimatorController != null && animator.HasState(0, Animator.StringToHash(state))) animator.Play(state, 0, 0f);
    }
    void SetFlash(float value)
    {
        if (sprite == null) return;
        sprite.GetPropertyBlock(flash);
        flash.SetFloat("_FlashAmount", value);
        sprite.SetPropertyBlock(flash);
    }
    void CreateBar()
    {
        barRoot = new GameObject(name + " health");
        barBack = CombatFeedback.MakeSprite("Border", barRoot.transform);
        barBack.transform.localScale = new Vector3(0.84f, 0.11f, 1);
        barFill = CombatFeedback.MakeSprite("Health", barRoot.transform);
        barFill.sortingOrder = barBack.sortingOrder + 1;
        barRoot.SetActive(false);
    }
    void OnDisable() { if (barRoot != null) barRoot.SetActive(false); SetFlash(0f); }
    void OnDestroy() { if (barRoot != null) Destroy(barRoot); }
}
