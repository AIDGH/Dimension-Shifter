using System.Collections.Generic;
using UnityEngine;

public class ChunkTrapSpawner : MonoBehaviour
{
    [Header("Trap Settings")]
    public GameObject trapPrefab;
    public int maxTraps = 4; // حداکثر تله تو یه چانک
    
    [Range(0f, 100f)]
    public float spawnChance = 50f; // احتمال (برای محاسبه امید ریاضی)
    
    public LayerMask groundLayer;
    public float yOffset = 0.59f;

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;
    public float obstacleClearanceRadius = 1f;

    [Header("Spawn Rules")]
    public float minXPadding = 2f;
    public float minDistanceBetweenTraps = 4f; 
    
    // 🛑 ترمز اضطراری: نهایتاً چند بار لیزر بندازه تا سیستم هنگ نکنه؟
    public int maxFailsafeAttempts = 50; 

    void Start()
    {
        SpawnTrapsProcedurally();
    }

    void SpawnTrapsProcedurally()
    {
        Transform endPos = transform.Find("EndPosition");
        if (endPos == null) return;

        float chunkWidth = endPos.localPosition.x;
        List<float> spawnedXPositions = new List<float>();

        // 🧠 جادوی تو: محاسبه امید ریاضی (مثلاً ۴ * ۰.۵ = ۲ تله قطعی)
        // تابع RoundToInt اگه بشه ۲.۵ گردش می‌کنه به ۳، اگه بشه ۲.۴ گردش می‌کنه به ۲
        int targetTrapCount = Mathf.RoundToInt(maxTraps * (spawnChance / 100f));

        int spawnedCount = 0; // چندتا با موفقیت کاشتیم؟
        int attempts = 0;     // چند بار لیزر انداختیم؟

        // 🔄 حلقه هوشمند: تا وقتی به تارگت نرسیدیم بگرد! (به شرطی که از ۵۰ بار بیشتر نشه)
        while (spawnedCount < targetTrapCount && attempts < maxFailsafeAttempts)
        {
            attempts++; // یه تلاش به حساب میاد
            
            float randomX = transform.position.x + Random.Range(minXPadding, chunkWidth - minXPadding);
            
            // چک کردن فاصله با تله‌های قبلی
            bool isTooClose = false;
            foreach (float pastX in spawnedXPositions)
            {
                if (Mathf.Abs(randomX - pastX) < minDistanceBetweenTraps)
                {
                    isTooClose = true;
                    break; 
                }
            }

            // اگه خیلی نزدیک بود، اصلاً لیزر ننداز و برو سراغ تلاش بعدی
            if (isTooClose) continue; 

            // شلیک لیزر
            Vector2 rayStart = new Vector2(randomX, transform.position.y + 15f);
            RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 30f, groundLayer);

            if (hit.collider != null)
            {
                if (Mathf.Abs(hit.normal.x) < 0.05f && hit.normal.y > 0.9f)
                {
                    Vector3 potentialSpawnPosition = new Vector3(hit.point.x, hit.point.y + yOffset, 0f);
                    
                    // اسکن موانع
                    Collider2D obstacleNearby = Physics2D.OverlapCircle(potentialSpawnPosition, obstacleClearanceRadius, obstacleLayer);

                    if (obstacleNearby == null)
                    {
                        Instantiate(trapPrefab, potentialSpawnPosition, Quaternion.identity, transform);
                        spawnedXPositions.Add(randomX);
                        
                        spawnedCount++; // ✅ یه دونه کاشتیم! به هدفمون نزدیک‌تر شدیم
                    }
                }
            }
        }
        
        // این خط رو گذاشتم تا تو کنسول ببینی چقدر خفن کار می‌کنه (بعداً می‌تونی پاکش کنی)
        Debug.Log($"هدف چانک: {targetTrapCount} تله | کاشته شده: {spawnedCount} | تعداد لیزر شلیک شده: {attempts}");
    }
}
