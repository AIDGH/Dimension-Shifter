using UnityEngine;
public class SpiritBolt : MonoBehaviour
{
    public Sprite[] frames;
    public float speed=12,lifetime=1.6f;
    public int damage=2;
    Vector2 direction;int world;float elapsed;SpriteRenderer visual;
    readonly RaycastHit2D[] hits=new RaycastHit2D[32];
    public void Launch(Vector2 heading,int dimension)
    {
        direction=heading.normalized;world=dimension;visual=gameObject.AddComponent<SpriteRenderer>();visual.sortingOrder=30;
        transform.localScale=new Vector3(direction.x<0?-1:1,1,1);
        visual.color=world==0?new Color(.65f,1,1):new Color(.9f,.6f,1);
    }
    void FixedUpdate()
    {
        elapsed+=Time.fixedDeltaTime;
        if(elapsed>lifetime||DimensionController.Instance==null||DimensionController.Instance.Current!=world){Destroy(gameObject);return;}
        if(frames!=null&&frames.Length>0)visual.sprite=frames[Mathf.FloorToInt(elapsed*12)%frames.Length];
        float distance=speed*Time.fixedDeltaTime;
        var filter=new ContactFilter2D();filter.SetLayerMask((1<<8)|(1<<9)|(1<<10)|(1<<11));filter.useTriggers=true;
        int count=Physics2D.CircleCast(transform.position,.16f,direction,filter,hits,distance);
        for(int i=0;i<count;i++)
        {
            var col=hits[i].collider;if(col==null)continue;
            var relay=col.GetComponentInParent<QuestInteractable>();
            if(relay!=null){if(relay.ReceiveBolt(world)){Destroy(gameObject);return;}continue;}
            if(col.isTrigger)continue;
            var enemy=col.GetComponentInParent<EnemyHealth>();if(enemy!=null){enemy.TakeDamage(damage,transform.position,1);Destroy(gameObject);return;}
            if(col.gameObject.layer==8||col.gameObject.layer==9){Destroy(gameObject);return;}
        }
        transform.position+=(Vector3)(direction*distance);
    }
}
