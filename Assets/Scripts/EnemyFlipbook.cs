using UnityEngine;

public class EnemyFlipbook : MonoBehaviour
{
    public Sprite[] idleFrames;
    public float framesPerSecond = 9f;
    public SpriteRenderer target;
    EnemyHealth health;
    void Awake() { health = GetComponent<EnemyHealth>(); }
    void LateUpdate()
    {
        if (target == null || idleFrames == null || idleFrames.Length == 0 || health.IsDead) return;
        target.sprite = idleFrames[Mathf.FloorToInt(Time.time * framesPerSecond) % idleFrames.Length];
        var p = target.transform.localPosition;
        p.y = -target.sprite.bounds.min.y * target.transform.localScale.y;
        target.transform.localPosition = p;
    }
}
