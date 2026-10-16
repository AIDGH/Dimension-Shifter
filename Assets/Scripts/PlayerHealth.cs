using UnityEngine;
using UnityEngine.UI;
using System.Collections; // 💡 این خط اضافه شده تا بتونیم سیستمِ زمان‌بندی (Coroutine) رو بسازیم

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI References")]
    public Image healthBarFill;

    [Header("Damage Visuals")]
    public SpriteRenderer spriteRenderer; // عکسِ کاراکتر برای تغییر رنگ
    public Color damageColor = Color.red; // رنگی که موقع دمیج خوردن می‌گیره
    public float iFramesDuration = 1.5f;  // چند ثانیه بعد از ضربه خوردن ضدضربه باشه؟
    public int numberOfFlashes = 4;       // تو این مدت چند بار چشمک بزنه؟

    private bool isInvincible = false; // آیا الان تو حالت ضدضربه است؟
    private Color originalColor;       // رنگ اصلی کاراکتر (که بعد از قرمز شدن برگرده به حالت اول)

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();

        // رنگ اصلی کاراکتر رو همون اول ذخیره می‌کنیم
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        // 🛑 اگه کاراکتر در حال چشمک زدن (ضدضربه) بود، کلاً دمیج نخور و برگرد!
        if (isInvincible) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); 
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // اگه زنده موند، سیستمِ قرمز شدن و چشمک زدن رو استارت بزن
            if (spriteRenderer != null)
            {
                StartCoroutine(DamageRoutine());
            }
        }
    }

    // 🎬 این همون جادوی زمان‌بندیِ ماست
    IEnumerator DamageRoutine()
    {
        // ۱. حالت ضدضربه رو فعال کن
        isInvincible = true;

        // ۲. رنگ کاراکتر رو درجا قرمز کن و دو دهمِ ثانیه نگه دار
        spriteRenderer.color = damageColor;
        yield return new WaitForSeconds(0.2f);

        // ۳. جادوی ریاضی برای محاسبه‌ی سرعت چشمک زدن
        float blinkDuration = (iFramesDuration - 0.2f) / (numberOfFlashes * 2);

        // ۴. حلقه چشمک زدن (نیمه‌شفاف شدن و دوباره عادی شدن)
        for (int i = 0; i < numberOfFlashes; i++)
        {
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.3f); // کمرنگ
            yield return new WaitForSeconds(blinkDuration);
            
            spriteRenderer.color = originalColor; // پررنگ
            yield return new WaitForSeconds(blinkDuration);
        }

        // ۵. در نهایت مطمئن می‌شیم رنگ برگشته به حالت عادی و ضدضربه بودن خاموش می‌شه
        spriteRenderer.color = originalColor;
        isInvincible = false;
    }

    void UpdateHealthUI()
    {
        if (healthBarFill != null && maxHealth > 0)
        {
            healthBarFill.fillAmount = currentHealth / maxHealth;
        }
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxHealth > 0 && healthBarFill != null)
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            UpdateHealthUI();
        }
    }
    #endif

    void Die()
    {
        Debug.Log("کاراکتر مرد! 💀");
        // به زودی اینجا منطق گیم‌اوفر رو اضافه می‌کنیم
    }
}