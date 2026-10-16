using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WeightedChunk
{
    public GameObject chunkPrefab;
    [Range(1, 100)]
    public int weight = 10;
    
    [HideInInspector] 
    public float length; 
}

public class LevelGenerator : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform startSpawnPoint;
    public CameraFollow cameraFollow; // 💡 لینک به اسکریپت دوربین برای خوندنِ اتوماتیک

    [Header("Chunk Pools")]
    public GameObject[] startChunks;
    public WeightedChunk[] midChunks;
    public GameObject[] endChunks;

    [Header("Level Settings")]
    public float spawnDistance = 20f;

    // 💡 متغیر levelMaxX رو مخفی کردیم چون حالا خودش حساب می‌کنه!
    private float levelMaxX; 
    
    private Transform lastEndPosition;
    private bool levelFinished = false;
    private float endChunkLength;

    void Start()
    {
        // 🧠 جادوی ریاضی: محاسبه‌ی اتوماتیک عرض دوربین
        Camera cam = Camera.main;
        // نصف عرضِ دوربین در دنیای یونیتی دقیقاً می‌شه سایز دوربین ضرب‌در نسبت تصویر
        float cameraHalfWidth = cam.orthographicSize * cam.aspect;

        if (cameraFollow != null)
        {
            // ماکسیمم واقعی = جایی که مرکز دوربین قفل می‌شه + نصفِ عرضِ مانیتور!
            levelMaxX = cameraFollow.maxX + cameraHalfWidth + 4;
        }
        else
        {
            Debug.LogError("آهای مهندس! اسکریپت CameraFollow رو تو Inspector به LevelGenerator ندادی!");
        }

        // خوندنِ اتوماتیکِ طول چانک‌ها
        foreach (var item in midChunks)
        {
            item.length = GetChunkLength(item.chunkPrefab);
        }
        
        if (endChunks.Length > 0)
            endChunkLength = GetChunkLength(endChunks[0]); 

        // ساخت چانک اول
        if (startChunks.Length > 0)
        {
            GameObject startChunk = startChunks[Random.Range(0, startChunks.Length)];
            SpawnSpecificChunk(startChunk, startSpawnPoint.position);
        }
    }

    void Update()
    {
        if (levelFinished) return;

        if (Vector2.Distance(player.position, lastEndPosition.position) < spawnDistance)
        {
            float remainingSpace = levelMaxX - lastEndPosition.position.x - endChunkLength;

            List<WeightedChunk> validChunks = new List<WeightedChunk>();
            int totalWeight = 0;

            foreach (var item in midChunks)
            {
                if (item.length <= remainingSpace)
                {
                    validChunks.Add(item);
                    totalWeight += item.weight;
                }
            }

            if (validChunks.Count > 0)
            {
                int randomValue = Random.Range(0, totalWeight);
                foreach (var item in validChunks)
                {
                    if (randomValue < item.weight)
                    {
                        SpawnSpecificChunk(item.chunkPrefab, lastEndPosition.position);
                        break;
                    }
                    randomValue -= item.weight;
                }
            }
            else
            {
                if (endChunks.Length > 0)
                {
                    GameObject endChunkToSpawn = endChunks[Random.Range(0, endChunks.Length)];
                    SpawnSpecificChunk(endChunkToSpawn, lastEndPosition.position);
                }
                levelFinished = true;
            }
        }
    }

    void SpawnSpecificChunk(GameObject prefab, Vector3 position)
    {
        GameObject newChunk = Instantiate(prefab, position, Quaternion.identity);
        lastEndPosition = newChunk.transform.Find("EndPosition");
    }

    float GetChunkLength(GameObject chunkPrefab)
    {
        Transform endPos = chunkPrefab.transform.Find("EndPosition");
        if (endPos != null)
        {
            return endPos.localPosition.x; 
        }
        Debug.LogError("EndPosition not found in " + chunkPrefab.name);
        return 0f;
    }
}