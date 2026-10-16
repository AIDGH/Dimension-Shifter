using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform player;
    public PlayerMovement playerMovement; // برای تشخیص روی زمین بودن

    [Header("Smoothness")]
    public float smoothTimeX = 0.15f;
    public float smoothTimeY = 0.25f; // Y یکم کندتر باشه سینمایی‌تره

    [Header("Limits")]
    public float minX;
    public float maxX;

    // متغیرهای داخلی برای محاسبه سرعت فنر
    private float velocityX = 0f;
    private float velocityY = 0f;
    private float targetY;
    [Min(0.5f)] public float airborneVerticalDeadzone = 3f;
    public float yOffset = 2f; // تا دوربین یکم بالاتر از پای کاراکتر رو ببینه

    public void SnapToPlayer()
    {
        if (player == null) return;
        velocityX = velocityY = 0f; targetY = player.position.y + yOffset;
        transform.position = new Vector3(Mathf.Clamp(player.position.x, minX, maxX), targetY, transform.position.z);
    }

    void Start()
    {
        if (player != null)
        {
            targetY = player.position.y + yOffset;
            if (playerMovement == null)
                playerMovement = player.GetComponent<PlayerMovement>();
        }
    }

    // 💡 استفاده از LateUpdate برای جلوگیری از تداخل با فیزیک
    void LateUpdate() 
    {
        if (player == null || playerMovement == null) return;

        // محاسبه X (با محدودیت لبه‌های نقشه)
        float clampedX = Mathf.Clamp(player.position.x, minX, maxX);

        // محاسبه Y (فقط وقتی رو زمینیم آپدیت میشه تا با پرش بالا نره)
        float desiredY = player.position.y + yOffset;
        if (playerMovement.isGrounded)
        {
            targetY = desiredY;
        }
        else if (Mathf.Abs(desiredY - targetY) > airborneVerticalDeadzone)
        {
            // Follow long falls or high jumps before the player leaves the screen.
            targetY = desiredY - Mathf.Sign(desiredY - targetY) * airborneVerticalDeadzone;
        }

        // جادوی SmoothDamp به جای Lerp
        float smoothedX = Mathf.SmoothDamp(transform.position.x, clampedX, ref velocityX, smoothTimeX);
        float smoothedY = Mathf.SmoothDamp(transform.position.y, targetY, ref velocityY, smoothTimeY);

        // 💡 منطقه مرگ (Deadzone) هم سر جاش باشه
        if (Mathf.Abs(transform.position.x - clampedX) < 0.01f) { smoothedX = clampedX; velocityX = 0f; }
        if (Mathf.Abs(transform.position.y - targetY) < 0.01f) { smoothedY = targetY; velocityY = 0f; }

        transform.position = new Vector3(smoothedX, smoothedY, transform.position.z);
    }
}