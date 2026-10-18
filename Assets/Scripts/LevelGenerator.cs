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
    [Tooltip("Authored encounters inserted between randomly chosen chunks.")]
    public GameObject[] encounterChunks;
    public bool authoredOnly;
    int encounterIndex, midSpawnCount;

    [Header("Level Settings")]
    public float spawnDistance = 20f;

    // 💡 متغیر levelMaxX رو مخفی کردیم چون حالا خودش حساب می‌کنه!
    private float levelMaxX; 
    
    private Transform lastEndPosition;
    private bool levelFinished = false;
    private float endChunkLength;
    GameObject bossArenaPrefab, bossArenaInstance;
    Vector3 bossArenaPosition;

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
        if (levelFinished || player == null || lastEndPosition == null) return;

        if (Vector2.Distance(player.position, lastEndPosition.position) < spawnDistance)
        {
            float remainingSpace = levelMaxX - lastEndPosition.position.x - endChunkLength;

            GameObject next = ChooseRouteChunk(remainingSpace);
            if (next != null)
            {
                SpawnSpecificChunk(next, lastEndPosition.position);
                midSpawnCount++;
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

    public GameObject ChooseRouteChunk(float remainingSpace)
    {
        if(authoredOnly)return encounterChunks!=null&&encounterIndex<encounterChunks.Length?encounterChunks[encounterIndex++]:null;
        if (encounterChunks != null && encounterIndex < encounterChunks.Length && midSpawnCount % 2 == 0)
        {
            var encounter = encounterChunks[encounterIndex];
            if (encounter != null && GetChunkLength(encounter) <= remainingSpace) { encounterIndex++; return encounter; }
        }
        return ChooseNextChunk(remainingSpace);
    }

    private readonly HashSet<GameObject> usedInCycle = new HashSet<GameObject>();
    private GameObject previousChunk;

    // Weighted selection without replacement: give every fitting chunk a turn.
    public GameObject ChooseNextChunk(float remainingSpace)
    {
        var fitting = new List<WeightedChunk>();
        foreach (var item in midChunks)
            if (item != null && item.chunkPrefab != null && item.length > 0f && item.length <= remainingSpace)
                fitting.Add(item);
        if (fitting.Count == 0) return null;
        var candidates = fitting.FindAll(item => !usedInCycle.Contains(item.chunkPrefab));
        if (candidates.Count == 0) { usedInCycle.Clear(); candidates.AddRange(fitting); }
        if (candidates.Count > 1) candidates.RemoveAll(item => item.chunkPrefab == previousChunk);
        int total = 0;
        foreach (var item in candidates) total += Mathf.Max(1, item.weight);
        int roll = Random.Range(0, total);
        foreach (var item in candidates)
        {
            roll -= Mathf.Max(1, item.weight);
            if (roll >= 0) continue;
            previousChunk = item.chunkPrefab; usedInCycle.Add(previousChunk); return previousChunk;
        }
        return null;
    }

    void SpawnSpecificChunk(GameObject prefab, Vector3 position)
    {
        GameObject newChunk = Instantiate(prefab, position, Quaternion.identity);
        if(newChunk.GetComponentInChildren<RiftWarden>()!=null){bossArenaPrefab=prefab;bossArenaInstance=newChunk;bossArenaPosition=position;}
        lastEndPosition = newChunk.transform.Find("EndPosition");
    }

    // Practice teleport needs the real authored route, including its final camera bounds.
    public bool LoadAuthoredRoute()
    {
        if (!authoredOnly || lastEndPosition == null) return false;
        while (encounterIndex < encounterChunks.Length)
            SpawnSpecificChunk(encounterChunks[encounterIndex++], lastEndPosition.position);
        if (!levelFinished && endChunks.Length > 0) SpawnSpecificChunk(endChunks[0], lastEndPosition.position);
        levelFinished = true;
        return true;
    }
    public RiftWarden RecreatePracticeArena()
    {
        if(bossArenaPrefab==null)return null;
        if(bossArenaInstance!=null)Destroy(bossArenaInstance);
        bossArenaInstance=Instantiate(bossArenaPrefab,bossArenaPosition,Quaternion.identity);
        return bossArenaInstance.GetComponentInChildren<RiftWarden>();
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
