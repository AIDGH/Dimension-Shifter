using UnityEngine;

public class ShrineVisual : MonoBehaviour
{
    public SpriteRenderer shrine;
    public bool Activated { get; private set; }
    float flash;
    public void Activate(){Activated=true;flash=1;}
    void Update()
    {
        if(shrine==null)return;
        flash=Mathf.MoveTowards(flash,0,Time.deltaTime);
        float glow=Activated?.94f+.06f*Mathf.Sin(Time.time*2):.67f;
        shrine.color=Color.Lerp(new Color(glow,glow,glow,1),new Color(.7f,1f,1f,1),flash);
    }
}
