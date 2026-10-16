using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 5f, lifetime = 4f, damage = 15f, radius = .12f;
    Vector2 direction;
    EnemyHealth owner;
    float remaining;
    readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    public void Launch(Vector2 aim, EnemyHealth source) { direction = aim.normalized; owner = source; remaining = lifetime; }
    void FixedUpdate()
    {
        remaining -= Time.fixedDeltaTime;
        if (remaining <= 0) { Destroy(gameObject); return; }
        float distance = speed * Time.fixedDeltaTime;
        var filter = new ContactFilter2D(); filter.NoFilter(); filter.useTriggers = false;
        int n = Physics2D.CircleCast(transform.position, radius, direction, filter, hits, distance);
        // Cast order ensures walls shield the player, even at high speeds.
        for (int i = 0; i < n; i++)
        {
            var hit = hits[i].collider;
            if (hit == null || (owner != null && hit.GetComponentInParent<EnemyHealth>() == owner)) continue;
            var player = hit.GetComponentInParent<PlayerHealth>();
            if (player != null)
            {
                var contact = player.GetComponent<PlayerEnemyContact>();
                if (contact == null || !contact.IsContactProtected) player.TryTakeDamage(damage);
                Destroy(gameObject); return;
            }
            if (hit.gameObject.layer == 8 || hit.gameObject.layer == 9) { Destroy(gameObject); return; }
        }
        transform.position += (Vector3)(direction * distance);
        transform.Rotate(0, 0, 120f * Time.fixedDeltaTime);
    }
}
