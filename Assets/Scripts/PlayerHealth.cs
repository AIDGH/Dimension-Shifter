using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public Image healthBarFill;
    [Header("Damage Visuals")]
    public SpriteRenderer spriteRenderer;
    public Color damageColor = new Color(1f, .35f, .35f);
    public float iFramesDuration = 2f;
    public int numberOfFlashes = 6;
    public bool IsInvincible => invincibleRemaining > 0f;
    public bool IsDead => currentHealth <= 0f;
    float invincibleRemaining;
    Color originalColor = Color.white;

    void Awake()
    {
        currentHealth = maxHealth;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        UpdateHealthUI();
    }
    public void TakeDamage(float amount) => TryTakeDamage(amount);
    public bool TryTakeDamage(float amount)
    {
        if (!isActiveAndEnabled || IsDead || IsInvincible || amount <= 0f) return false;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        invincibleRemaining = Mathf.Max(.1f, iFramesDuration);
        UpdateHealthUI();
        GameAudio.Play(IsDead ? "player_death" : "hurt", .7f);
        if (IsDead)
        {
            var attack = GetComponent<PlayerAttack>(); if (attack != null) attack.enabled = false;
            var motor = GetComponent<PlayerMovement>(); if (motor != null) motor.enabled = false;
            var body = GetComponent<Rigidbody2D>(); if (body != null) body.linearVelocity = Vector2.zero;
        }
        return true;
    }
    void Update()
    {
        Tick(Time.deltaTime);
        if (IsDead && Input.GetKeyDown(KeyCode.R))
        {
            var run = GetComponent<RunProgress>();
            if (run != null) run.Respawn(); else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
    void Tick(float dt)
    {
        invincibleRemaining = Mathf.Max(0, invincibleRemaining - dt);
        if (spriteRenderer == null) return;
        if (!IsInvincible) { spriteRenderer.color = originalColor; return; }
        float elapsed = iFramesDuration - invincibleRemaining;
        var color = elapsed < .12f ? damageColor : originalColor;
        if (elapsed >= .12f && Mathf.FloorToInt(elapsed * Mathf.Max(1, numberOfFlashes) * 2 / Mathf.Max(.1f,iFramesDuration)) % 2 == 0) color.a *= .35f;
        spriteRenderer.color = color;
    }
    public void RestoreHealth()
    {
        currentHealth = maxHealth; invincibleRemaining = iFramesDuration;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        UpdateHealthUI();
    }
    void UpdateHealthUI() { if (healthBarFill != null) healthBarFill.fillAmount = maxHealth > 0 ? currentHealth / maxHealth : 0f; }
    void OnDisable() { if(spriteRenderer != null)spriteRenderer.color = originalColor; }
    void OnGUI()
    {
        if (!IsDead) return;
        var style = new GUIStyle(GUI.skin.box) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
        GUI.Box(new Rect(Screen.width/2f-180, Screen.height/2f-45,360,90), "You fell\nPress R to retry", style);
    }
}
