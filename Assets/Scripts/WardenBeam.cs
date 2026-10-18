using UnityEngine;

// Swept collider matches the visible energy lance; terrain shields the player.
public class WardenBeam : MonoBehaviour
{
    public RiftWarden Owner { get; private set; }
    Vector2 heading;float remaining=3;const float Length=2.8f,Width=.12f,Speed=15f;
    Mesh ribbon;
    readonly RaycastHit2D[] hits=new RaycastHit2D[24];
    public void Launch(Vector2 position,Vector2 direction,Material material,RiftWarden owner)
    {
        Owner=owner;heading=direction.normalized;transform.position=position;
        // Use the same animated filaments as the dimension barriers, in one icy hue.
        transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(heading.y,heading.x)*Mathf.Rad2Deg-90);
        ribbon=new Mesh{name="Warden lightning ribbon"};
        ribbon.vertices=new[]{new Vector3(-.18f,-Length*.5f),new Vector3(.18f,-Length*.5f),new Vector3(-.18f,Length*.5f),new Vector3(.18f,Length*.5f)};
        ribbon.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one};ribbon.triangles=new[]{0,2,1,2,3,1};ribbon.RecalculateBounds();
        gameObject.AddComponent<MeshFilter>().sharedMesh=ribbon;
        var visual=gameObject.AddComponent<MeshRenderer>();visual.sharedMaterial=material;visual.sortingOrder=40;
        var tint=new MaterialPropertyBlock();tint.SetColor("_Tint",new Color(.45f,.9f,1));tint.SetFloat("_Crackle",1);visual.SetPropertyBlock(tint);
    }
    void FixedUpdate()
    {
        if(Owner==null||!Owner.Engaged||(remaining-=Time.fixedDeltaTime)<=0){Destroy(gameObject);return;}
        float distance=Speed*Time.fixedDeltaTime;var filter=ContactFilter2D.noFilter;filter.useTriggers=false;
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
    void OnDestroy(){if(ribbon!=null)Destroy(ribbon);}
}
