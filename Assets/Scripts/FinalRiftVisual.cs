using UnityEngine;
public class FinalRiftVisual:MonoBehaviour
{
 public Material material;LineRenderer[] rings;
 void Start(){rings=new LineRenderer[3];for(int j=0;j<rings.Length;j++){var g=new GameObject("Home rift filament");g.transform.SetParent(transform,false);var l=rings[j]=g.AddComponent<LineRenderer>();l.sharedMaterial=material;l.useWorldSpace=false;l.loop=true;l.positionCount=65;l.widthMultiplier=j==0?.11f:.045f;l.sortingOrder=28;l.startColor=l.endColor=j==0?new Color(1,.87f,.45f):new Color(.6f,.9f,1,.8f);}}
 void Update(){if(rings==null)return;for(int j=0;j<rings.Length;j++)for(int i=0;i<65;i++){float a=i*Mathf.PI*2/64;float r=1+Mathf.Sin(a*3+Time.time*2+j)*.035f;rings[j].SetPosition(i,new Vector3(Mathf.Cos(a)*(.68f+j*.13f)*r,Mathf.Sin(a)*(1.65f-j*.14f),0));}}
}
