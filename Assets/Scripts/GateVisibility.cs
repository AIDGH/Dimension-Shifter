using UnityEngine;
public class GateVisibility : MonoBehaviour
{
    public Collider2D shape;public Renderer visual;
    void LateUpdate(){if(shape!=null&&visual!=null)visual.enabled=shape.enabled;}
}
