using UnityEngine;

// Swept collider matches the visible energy lance; terrain shields the player.
public class WardenBeam : MonoBehaviour
{
    public RiftWarden Owner { get; private set; }
    Vector2 heading;float remaining=3;const float Length=2.8f,Width=.22f,Speed=15f;
    readonly RaycastHit2D[] hits=new RaycastHit2D[24];
    public void Launch(Vector2 position,Vector2 direction,Material material,RiftWarden owner)
    {
        Owner=owner;heading=direction.normalized;transform.position=position;
        var line=gameObject.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=false;line.positionCount=2;line.startWidth=line.endWidth=Width;line.numCapVertices=6;line.sortingOrder=40;
        line.startColor=new Color(.5f,.85f,1);line.endColor=new Color(.85f,.45f,1);
        line.SetPosition(0,-heading*Length*.5f);line.SetPosition(1,heading*Length*.5f);
    }
    void FixedUpdate()
    {
        if(Owner==null||!Owner.Engaged||(remaining-=Time.fixedDeltaTime)<=0){Destroy(gameObject);return;}
        float distance=Speed*Time.fixedDeltaTime;var filter=new ContactFilter2D();filter.NoFilter();filter.useTriggers=false;
        int count=Physics2D.BoxCast(transform.position,new Vector2(Length,Width),Mathf.Atan2(heading.y,heading.x)*Mathf.Rad2Deg,heading,filter,hits,distance);
        for(int i=0;i<count;i++)
        {
            var col=hits[i].collider;if(col==null||col.GetComponentInParent<RiftWarden>()==Owner)continue;
            var player=col.GetComponentInParent<PlayerHealth>();
            if(player!=null){if(!player.GetComponent<PlayerEnemyContact>().IsContactProtected)player.TryTakeDamage(22);Destroy(gameObject);return;}
            if(col.gameObject.layer==8||col.gameObject.layer==9){Destroy(gameObject);return;}
        }
        transform.position+=(Vector3)(heading*distance);
    }
}
