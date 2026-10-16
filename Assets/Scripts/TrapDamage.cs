using UnityEngine;

public class TrapDamage : MonoBehaviour
{
    [Header("Trap Settings")]
    public float damageAmount = 20f;
    public float wakeUpRadius = 6f; // شعاع رادار: چقدر نزدیک بشیم تا فقط بیدار بشه؟
    public float destroyDelay = 1.5f; // چند ثانیه بعد از اینکه خرد شد، کلاً از بازی پاک بشه؟

    private Animator anim;
    private Transform player;
    private bool isAwake = false;    // آیا بیدار شده (گنده شده)؟
    private bool hasAttacked = false; // آیا حمله کرده؟ (که دیگه دوبار دمیج نده)

    void Start()
    {
        anim = GetComponent<Animator>();
        
        // پیدا کردن کاراکتر تو صحنه
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if(p != null) player = p.transform;
    }

    void Update()
    {
        // 📡 رادارِ بیدارباش
        if (!isAwake && player != null)
        {
            // فاصله تله تا پلیر رو می‌سنجیم
            if (Vector2.Distance(transform.position, player.position) <= wakeUpRadius)
            {
                isAwake = true;
                anim.SetTrigger("WakeUp"); // پخش انیمیشن Grow و رفتن به Idle
            }
        }
    }

    // 🩸 سنسور برخورد فیزیکی (حمله اصلی)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // اگه بیدار شده بود، هنوز حمله نکرده بود، و کسی که بهش خورد Player بود...
        if (isAwake && !hasAttacked && collision.CompareTag("Player"))
        {
            hasAttacked = true; // تیک می‌زنیم که دیگه دوباره دمیج نده
            
            anim.SetTrigger("Attack"); // 🎬 پخش انیمیشن حمله (که بعدش خودکار میره روی Die)
            
            // دمیج دادن به پلیر
            PlayerHealth health = collision.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damageAmount);
            }

            // کلاً این آبجکت رو بعد از چند ثانیه از رمِ سیستم پاک می‌کنیم
            Destroy(gameObject, destroyDelay); 
        }
    }

    // 🪄 کشیدنِ دایره زرد برای تنظیم راحت‌ترِ رادار تو محیط یونیتی
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wakeUpRadius);
    }
}