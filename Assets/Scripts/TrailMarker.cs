using UnityEngine;

public class TrailMarker : MonoBehaviour
{
    public enum MarkerKind { Hint, Checkpoint, Exit }
    public MarkerKind kind;
    [TextArea] public string message;
    public Transform spawnPoint;
    public SpriteRenderer beacon;
    public bool alignToGround;
    bool activated;
    void Start()
    {
        if(!alignToGround)return;
        var hit=Physics2D.Raycast(transform.position+Vector3.up*20,Vector2.down,40,1<<8);
        if(hit.collider!=null)transform.position=new Vector3(transform.position.x,hit.point.y+.9f,transform.position.z);
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        var run=other.GetComponentInParent<RunProgress>();
        if(run==null || activated || other.GetComponentInParent<PlayerHealth>().IsDead) return;
        activated=true;
        if(kind==MarkerKind.Checkpoint)run.SetCheckpoint(spawnPoint!=null?spawnPoint.position:transform.position,message);
        else if(kind==MarkerKind.Exit)run.Complete();
        else run.ShowMessage(message);
        var shrine=GetComponent<ShrineVisual>();if(shrine!=null)shrine.Activate();
        else if(beacon!=null)beacon.color=new Color(.7f,1f,.8f);
    }
}
