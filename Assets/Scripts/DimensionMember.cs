using UnityEngine;

public class DimensionMember : MonoBehaviour
{
    [Range(0,1)] public int dimension;
    public bool IsPresent { get; private set; }
    Collider2D[] shapes;
    bool[] originalTriggers;
    SpriteRenderer[] sprites;
    Color[] colors;
    LineRenderer[] beams;
    Color[] beamColors;
    UnityEngine.Tilemaps.Tilemap[] maps;
    Color[] mapColors;
    MeshRenderer[] meshes;
    Color[] meshColors;
    void Awake()
    {
        shapes = GetComponentsInChildren<Collider2D>(); originalTriggers = new bool[shapes.Length];
        for(int i=0;i<shapes.Length;i++) originalTriggers[i]=shapes[i].isTrigger;
        sprites = GetComponentsInChildren<SpriteRenderer>(); colors = new Color[sprites.Length];
        for(int i=0;i<sprites.Length;i++) colors[i]=sprites[i].color;
        beams=GetComponentsInChildren<LineRenderer>();beamColors=new Color[beams.Length];
        for(int i=0;i<beams.Length;i++)beamColors[i]=beams[i].startColor;
        maps=GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>();mapColors=new Color[maps.Length];
        for(int i=0;i<maps.Length;i++)mapColors[i]=maps[i].color;
        meshes=GetComponentsInChildren<MeshRenderer>();meshColors=new Color[meshes.Length];
        for(int i=0;i<meshes.Length;i++)meshColors[i]=meshes[i].sharedMaterial.GetColor("_Tint");
    }
    void Start() { Apply(DimensionController.Instance != null ? DimensionController.Instance.Current : 0); }
    public bool WouldOverlap(Collider2D player, int next)
    {
        if(next != dimension || shapes == null) return false;
        for(int i=0;i<shapes.Length;i++)
        {
            if(originalTriggers[i] || !shapes[i].enabled) continue;
            var distance=shapes[i].Distance(player);
            if(distance.isOverlapped && distance.distance < -.015f) return true;
        }
        return false;
    }
    public void Apply(int current)
    {
        IsPresent = dimension == current;
        for(int i=0;i<shapes.Length;i++) shapes[i].isTrigger = !IsPresent || originalTriggers[i];
        float alpha=IsPresent?1f:.14f;
        for(int i=0;i<sprites.Length;i++) { var c=colors[i]; c.a *= alpha; sprites[i].color=c; }
        for(int i=0;i<beams.Length;i++) { var c=beamColors[i];c.a*=alpha;beams[i].startColor=beams[i].endColor=c; }
        for(int i=0;i<maps.Length;i++) { var c=mapColors[i];c.a*=alpha;maps[i].color=c; }
        for(int i=0;i<meshes.Length;i++) { var c=meshColors[i];c*=dimension==0?new Color(.6f,1,1,alpha):new Color(1,.6f,1,alpha);var block=new MaterialPropertyBlock();meshes[i].GetPropertyBlock(block);block.SetColor("_Tint",c);meshes[i].SetPropertyBlock(block); }
    }
}
