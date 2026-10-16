using UnityEngine;

public class TimedHazard : MonoBehaviour
{
    public float restDuration = 2f, warningDuration = .9f, activeDuration = .8f, damage = 20f;
    public float phaseOffset;
    public SpriteRenderer warning;
    public Transform spikes;
    public bool IsActive { get; private set; }
    public bool IsWarning { get; private set; }
    Collider2D area;
    DimensionMember dimension;
    readonly Collider2D[] overlaps = new Collider2D[8];
    void Awake() { area = GetComponent<Collider2D>(); dimension = GetComponent<DimensionMember>(); }
    void Update()
    {
        float time = Mathf.Repeat(Time.time + phaseOffset, restDuration + warningDuration + activeDuration);
        bool present = dimension == null || dimension.IsPresent;
        IsWarning = present && time >= restDuration && time < restDuration + warningDuration;
        IsActive = present && time >= restDuration + warningDuration;
        if (warning != null) { warning.enabled = present; warning.color = IsActive ? new Color(1,.25f,.3f) : IsWarning ? new Color(1,.7f,.25f, .45f + .55f * Mathf.PingPong(Time.time * 6,1)) : new Color(.3f,.55f,.6f,.4f); }
        if (spikes != null) { var scale = spikes.localScale; scale.y = IsActive ? 1 : .08f; spikes.localScale = scale; }
    }
    void FixedUpdate()
    {
        if (!IsActive || (dimension != null && !dimension.IsPresent)) return;
        var filter = new ContactFilter2D(); filter.NoFilter(); filter.useTriggers = false;
        int n = area.Overlap(filter, overlaps);
        for (int i = 0; i < n; i++) { var hp = overlaps[i].GetComponentInParent<PlayerHealth>(); if (hp != null) hp.TryTakeDamage(damage); }
    }
}
