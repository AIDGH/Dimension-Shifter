using UnityEngine;
public class SpiritWeapon : MonoBehaviour
{
    public Sprite[] frames;
    public float cooldown=1.35f;
    float readyAt;
    public float CooldownRemaining=>Mathf.Max(0,readyAt-Time.time);
    void Update(){if(Input.GetKeyDown(KeyCode.C))Fire();}
    public bool Fire()
    {
        var s=GetComponent<DemoSession>();var health=GetComponent<PlayerHealth>();
        if(s==null||!s.Started||s.Paused||!s.HasSpirit||health.IsDead||GetComponent<RunProgress>().Completed||Time.time<readyAt||GetComponent<PlayerAttack>().IsDashing)return false;
        readyAt=Time.time+cooldown;
        var sr=GetComponent<SpriteRenderer>();float facing=transform.lossyScale.x<0?-1:1;if(sr!=null&&sr.flipX)facing*=-1;
        var g=new GameObject("Spirit bolt");g.transform.position=GetComponent<Collider2D>().bounds.center;
        var bolt=g.AddComponent<SpiritBolt>();bolt.frames=frames;bolt.Launch(Vector2.right*facing,DimensionController.Instance.Current);
        GameAudio.Play("cast",.5f);return true;
    }
}
