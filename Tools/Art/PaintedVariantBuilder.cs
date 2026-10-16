using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Run in an isolated project with -executeMethod PaintedVariantBuilder.Build.
// The original scene, tilemaps and chunk prefabs are never saved over.
public static class PaintedVariantBuilder
{
    const string Art="Assets/Art/Painted";
    const string ChunkRoot="Assets/Prefabs/PaintedChunks";
    static Material material;
    static int meshCount;
    static Mesh floraMesh;
    static Material floraMaterial;
    public static void Build()
    {
        try
        {
            Directory.CreateDirectory(Art+"/Meshes");Directory.CreateDirectory(ChunkRoot);
            AssetDatabase.Refresh();
            var importer=(TextureImporter)AssetImporter.GetAtPath(Art+"/ColdCavernStone.png");
            importer.textureType=TextureImporterType.Default;
            importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Bilinear;
            importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            material=new Material(Shader.Find("DimensionShifter/PaintedTerrain"));
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/ColdCavernStone.png");
            material.SetColor("_Tint",new Color(0.8f,0.9f,1f,1f));
            material.SetColor("_EdgeColor",new Color(0.48f,0.72f,0.76f,1f));
            ReplaceAsset(material,Art+"/ColdCavernStone.mat");
            BuildFlora();
            var remap=new Dictionary<string,GameObject>();
            foreach(string path in Directory.GetFiles("Assets/Prefabs/Chunks","*.prefab",SearchOption.AllDirectories))
            {
                string sourcePath=path.Replace('\\','/');
                string name=Path.GetFileNameWithoutExtension(sourcePath);
                var root=PrefabUtility.LoadPrefabContents(sourcePath);
                try
                {
                    root.name=name+"_Painted";
                    PaintRoot(root,name);
                    string output=ChunkRoot+"/"+name+"_Painted.prefab";
                    remap[sourcePath]=PrefabUtility.SaveAsPrefabAsset(root,output);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            var spriteMaterial=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            ReplaceAsset(spriteMaterial,Art+"/PaintedSprites.mat");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity",OpenSceneMode.Single);
            foreach(var root in scene.GetRootGameObjects())
            {
                if(root.GetComponentInChildren<Tilemap>(true)!=null) PaintRoot(root,"Scene_"+root.name);
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if(t.name=="Forest_1") t.gameObject.SetActive(true);
                    if(t.name=="Halloween_2") t.gameObject.SetActive(false);
                    // Existing hand-painted blue forest stays; lower contrast behind terrain.
                    var anySprite=t.GetComponent<SpriteRenderer>();
                    if(anySprite != null) anySprite.sharedMaterial=spriteMaterial;
                    var sr=t.GetComponent<SpriteRenderer>();
                    if(t.GetComponent<Parallax>()!=null && sr!=null)
                    {
                        sr.color=new Color(sr.color.r*0.56f,sr.color.g*0.66f,sr.color.b*0.79f,sr.color.a);
                    }
                }
            }
            var generator=UnityEngine.Object.FindFirstObjectByType<LevelGenerator>();
            if(generator!=null)
            {
                for(int i=0;i<generator.startChunks.Length;i++)generator.startChunks[i]=Map(generator.startChunks[i],remap);
                foreach(var item in generator.midChunks)item.chunkPrefab=Map(item.chunkPrefab,remap);
                for(int i=0;i<generator.endChunks.Length;i++)generator.endChunks[i]=Map(generator.endChunks[i],remap);
            }
            var player=GameObject.FindGameObjectWithTag("Player");
            if(player!=null) AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Red/Player_Idle.anim").SampleAnimation(player,0f);
            // This variant uses a 2D orthographic camera with the existing background depths.
            Camera.main.orthographic=true;
            Camera.main.orthographicSize=5.4f;
            foreach(var behaviour in Camera.main.GetComponents<MonoBehaviour>())
                if(behaviour.GetType().Name=="PixelPerfectCamera")behaviour.enabled=false;
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/PaintedDimension.unity");
            AssetDatabase.SaveAssets();
            File.WriteAllText("painted-build-results.txt",$"Created {remap.Count} separate painted chunk prefabs and {meshCount} terrain meshes.\nCreated Assets/Scenes/PaintedDimension.unity. Original chunks/scene preserved.\n");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static GameObject Map(GameObject input,Dictionary<string,GameObject> map)
    {
        if(input!=null && map.TryGetValue(AssetDatabase.GetAssetPath(input),out var result))return result;
        return input;
    }
    static void ReplaceAsset(UnityEngine.Object obj,string path)
    {
        if(AssetDatabase.LoadMainAssetAtPath(path)!=null)AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(obj,path);
    }
    static void PaintRoot(GameObject root,string prefix)
    {
        foreach(var map in root.GetComponentsInChildren<Tilemap>(true))
        {
            var renderer=map.GetComponent<TilemapRenderer>();
            if(renderer==null)continue;
            if(map.GetComponent<TilemapCollider2D>()==null && map.GetComponent<CompositeCollider2D>()==null)
            {
                renderer.enabled=false; // Pixel decorations are kept but hidden in this version.
                continue;
            }
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var indices=new List<int>();
            foreach(Vector3Int cell in map.cellBounds.allPositionsWithin)
            {
                if(!map.HasTile(cell))continue;
                var sprite=map.GetSprite(cell);
                if(sprite==null)continue;
                var type=map.GetColliderType(cell);
                if(type==Tile.ColliderType.None)continue;
                var contours=new List<List<Vector2>>();
                if(type==Tile.ColliderType.Grid)
                    contours.Add(new List<Vector2>{new Vector2(-.5f,-.5f),new Vector2(.5f,-.5f),new Vector2(.5f,.5f),new Vector2(-.5f,.5f)});
                else
                {
                    for(int p=0;p<sprite.GetPhysicsShapeCount();p++)
                    {var shape=new List<Vector2>();sprite.GetPhysicsShape(p,shape);contours.Add(shape);}
                }
                Matrix4x4 transform=map.GetTransformMatrix(cell);
                Vector3 center=map.GetCellCenterLocal(cell);
                foreach(var points in contours)
                {
                    var contour=points.Select(p=>(Vector2)(center+transform.MultiplyPoint3x4(p))).ToList();
                    CleanContour(contour);
                    if(contour.Count<3)continue;
                    if(Area(contour)<0)contour.Reverse();
                    int start=vertices.Count;
                    foreach(var p in contour){vertices.Add(p);uv.Add(p/4f);colors.Add(new Color(1,1,1,0));}
                    foreach(int i in Triangulate(contour))indices.Add(start+i);
                    // Only outline exposed upward-facing edges; geometry and collision stay identical.
                    for(int i=0;i<contour.Count;i++)
                    {
                        Vector2 a=contour[i],b=contour[(i+1)%contour.Count];
                        Vector2 d=(b-a).normalized;Vector2 normal=new Vector2(d.y,-d.x);
                        if(normal.y<0.45f)continue;
                        Vector3Int neighbor=map.LocalToCell((a+b)*0.5f+normal*0.03f);
                        if(neighbor!=cell && map.HasTile(neighbor))continue;
                        int edge=vertices.Count;
                        foreach(var p in new[]{a,b,b-normal*0.07f,a-normal*0.07f}){vertices.Add(new Vector3(p.x,p.y,-0.003f));uv.Add(p/4f);}
                        colors.Add(Color.white);colors.Add(Color.white);colors.Add(new Color(1,1,1,0));colors.Add(new Color(1,1,1,0));
                        indices.AddRange(new[]{edge,edge+1,edge+2,edge,edge+2,edge+3});
                    }
                }
            }
            if(vertices.Count==0)continue;
            var mesh=new Mesh{name=prefix+"_"+map.name,indexFormat=IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
            string meshPath=Art+"/Meshes/"+prefix+"_"+map.name+".asset";
            ReplaceAsset(mesh,meshPath);meshCount++;
            var skin=new GameObject("Painted surface");skin.transform.SetParent(map.transform,false);
            skin.AddComponent<MeshFilter>().sharedMesh=mesh;
            var meshRenderer=skin.AddComponent<MeshRenderer>();meshRenderer.sharedMaterial=material;
            meshRenderer.sortingLayerID=renderer.sortingLayerID;meshRenderer.sortingOrder=renderer.sortingOrder;
            meshRenderer.shadowCastingMode=ShadowCastingMode.Off;meshRenderer.receiveShadows=false;
            renderer.enabled=false;
            if(map.name.StartsWith("Gnd")) {
                int planted=0;
                foreach(Vector3Int cell in map.cellBounds.allPositionsWithin) {
                    if(!map.HasTile(cell) || map.HasTile(cell+Vector3Int.up) || Mathf.Abs(cell.x)%4!=1)continue;
                    if(map.GetColliderType(cell)==Tile.ColliderType.None)continue;
                    var plant=new GameObject("Luminous fern");plant.transform.SetParent(map.transform,false);
                    plant.transform.localPosition=map.GetCellCenterLocal(cell)+new Vector3(0,SurfaceHeight(map,cell),-.015f);
                    float size=.65f+(Mathf.Abs(cell.x*13)%5)*.07f;
                    plant.transform.localScale=new Vector3((planted++%2==0?1:-1)*size,size,1);
                    plant.AddComponent<MeshFilter>().sharedMesh=floraMesh;
                    var fern=plant.AddComponent<MeshRenderer>();fern.sharedMaterial=floraMaterial;
                    fern.sortingLayerID=renderer.sortingLayerID;fern.sortingOrder=renderer.sortingOrder+1;
                    fern.shadowCastingMode=ShadowCastingMode.Off;
                }
            }
        }
    }
    static float SurfaceHeight(Tilemap map,Vector3Int cell)
    {
        if(map.GetColliderType(cell)==Tile.ColliderType.Grid)return .5f;
        var sprite=map.GetSprite(cell);float top=-.5f;
        if(sprite==null)return top;
        for(int p=0;p<sprite.GetPhysicsShapeCount();p++) {
            var outline=new List<Vector2>();sprite.GetPhysicsShape(p,outline);
            for(int i=0;i<outline.Count;i++) {
                Vector2 a=outline[i],b=outline[(i+1)%outline.Count];
                if((a.x<=0 && b.x>=0)||(b.x<=0 && a.x>=0)) {
                    if(Mathf.Abs(b.x-a.x)<.0001f)top=Mathf.Max(top,a.y,b.y);
                    else top=Mathf.Max(top,Mathf.Lerp(a.y,b.y,-a.x/(b.x-a.x)));
                }
            }
        }
        return top;
    }
    static void BuildFlora()
    {
        floraMaterial=new Material(Shader.Find("DimensionShifter/PaintedFlora"));
        ReplaceAsset(floraMaterial,Art+"/LuminousFern.mat");
        var v=new List<Vector3>();var c=new List<Color>();var tri=new List<int>();
        Action<Vector2,Vector2,float,Color,Color> ribbon=(a,b,width,baseColor,tipColor)=> {
            Vector2 side=new Vector2(-(b-a).y,(b-a).x).normalized*width;
            int index=v.Count;v.Add(a-side);v.Add(a+side);v.Add(b);c.Add(baseColor);c.Add(baseColor);c.Add(tipColor);
            tri.AddRange(new[]{index,index+1,index+2});
        };
        Color stem=new Color(.08f,.28f,.28f), leaf=new Color(.13f,.46f,.44f), tip=new Color(.51f,.86f,.78f);
        for(int branch=-1;branch<=1;branch++) {
            Vector2 origin=Vector2.zero;float height=branch==0?.78f:.55f;
            for(int step=0;step<8;step++) {
                float t=(step+1)/8f;Vector2 next=new Vector2(branch*.24f*t*t,height*t);
                ribbon(origin,next,.013f*(1-t*.6f),stem,leaf);
                if(step>1 && step<7) {
                    float length=.22f*(1-t*.65f);
                    foreach(int side in new[]{-1,1}) {
                        Vector2 end=next+new Vector2(side*length,length*.7f);
                        ribbon(next,end,.037f*(1-t*.4f),leaf,tip);
                    }
                }
                origin=next;
            }
            // Soft translucent halo plus a pale seed at each branch tip.
            for(int ring=3;ring>=1;ring--) {
                int centre=v.Count;v.Add(origin);c.Add(new Color(.45f,.88f,.78f,ring==1?.9f:.045f));
                float radius=ring==1?.022f:.045f*ring;
                for(int i=0;i<=16;i++) {
                    float angle=i*Mathf.PI*2/16;v.Add(origin+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius);
                    c.Add(new Color(.5f,.9f,.8f,ring==1?.7f:0f));
                    if(i>0)tri.AddRange(new[]{centre,centre+i,centre+i+1});
                }
            }
        }
        floraMesh=new Mesh{name="Luminous fern"};floraMesh.SetVertices(v);floraMesh.SetColors(c);floraMesh.SetTriangles(tri,0);floraMesh.RecalculateBounds();
        ReplaceAsset(floraMesh,Art+"/LuminousFern.asset");
    }
    static void CleanContour(List<Vector2> p)
    {
        bool changed=true;
        while(changed && p.Count>=3)
        {
            changed=false;
            for(int i=p.Count-1;i>=0;i--)
            {
                Vector2 prev=p[(i+p.Count-1)%p.Count], next=p[(i+1)%p.Count];
                if((p[i]-prev).sqrMagnitude<0.0000001f || Mathf.Abs(Cross(p[i]-prev,next-p[i]))<0.000001f)
                {p.RemoveAt(i);changed=true;break;}
            }
        }
    }
    static float Area(List<Vector2> p)
    {float a=0;for(int i=0;i<p.Count;i++){var q=p[(i+1)%p.Count];a+=p[i].x*q.y-q.x*p[i].y;}return a*.5f;}
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static List<int> Triangulate(List<Vector2> p)
    {
        var remaining=Enumerable.Range(0,p.Count).ToList();var triangles=new List<int>();int guard=p.Count*p.Count;
        while(remaining.Count>2 && guard-->0)
        {
            bool clipped=false;
            for(int i=0;i<remaining.Count;i++)
            {
                int a=remaining[(i+remaining.Count-1)%remaining.Count],b=remaining[i],c=remaining[(i+1)%remaining.Count];
                if(Cross(p[b]-p[a],p[c]-p[b])<=0.000001f)continue;
                bool inside=false;
                foreach(int k in remaining)
                {
                    if(k==a||k==b||k==c)continue;
                    if(Cross(p[b]-p[a],p[k]-p[a])>=-0.000001f && Cross(p[c]-p[b],p[k]-p[b])>=-0.000001f && Cross(p[a]-p[c],p[k]-p[c])>=-0.000001f){inside=true;break;}
                }
                if(inside)continue;
                triangles.AddRange(new[]{a,b,c});remaining.RemoveAt(i);clipped=true;break;
            }
            if(!clipped)break;
        }
        if(remaining.Count>2)throw new Exception("Could not triangulate a terrain contour: "+string.Join(";",p));
        return triangles;
    }

    public static void RenderPreview()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity",OpenSceneMode.Single);
            var generator=UnityEngine.Object.FindFirstObjectByType<LevelGenerator>();
            // Editor-only preview instances; the saved scene continues to generate chunks at runtime.
            Vector3 next=generator.startSpawnPoint.position;
            var sources=new[]{generator.startChunks[0],generator.midChunks[0].chunkPrefab,generator.midChunks[1].chunkPrefab};
            foreach(var prefab in sources)
            {var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);instance.transform.position=next;next=instance.transform.Find("EndPosition").position;}
            foreach(var mapCollider in UnityEngine.Object.FindObjectsByType<TilemapCollider2D>(FindObjectsSortMode.None))mapCollider.ProcessTilemapChanges();
            Physics2D.SyncTransforms();
            var previewPlayer=GameObject.FindGameObjectWithTag("Player");
            if(previewPlayer!=null) {
                var ground=Physics2D.Raycast(previewPlayer.transform.position,Vector2.down,30f,(1<<8)|(1<<9));
                if(ground.collider!=null) {
                    var point=previewPlayer.transform.position;
                    point.y=ground.point.y+previewPlayer.GetComponent<CapsuleCollider2D>().size.y*.5f+.005f;
                    previewPlayer.transform.position=point;
                }
            }
            Camera camera=Camera.main;
            File.WriteAllLines("preview-sprites.txt",UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Select(sr=>$"{sr.name}: sprite={sr.sprite?.name}, material={sr.sharedMaterial?.name}, shader={sr.sharedMaterial?.shader?.name}, position={sr.transform.position}, size={sr.bounds.size}, enabled={sr.enabled}, active={sr.gameObject.activeInHierarchy}"));
            camera.transform.position=new Vector3(generator.startSpawnPoint.position.x+10f,generator.startSpawnPoint.position.y+3f,camera.transform.position.z);
            camera.orthographic=true;camera.orthographicSize=5.4f;camera.aspect=16f/9f;
            foreach(var layer in UnityEngine.Object.FindObjectsByType<Parallax>(FindObjectsSortMode.None)) {
                typeof(Parallax).GetMethod("Start",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(layer,null);
                typeof(Parallax).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(layer,null);
            }
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
            // Manual editor render requests do not populate sprite texture bindings
            // until a player loop has rendered. Bind them explicitly for this preview.
            foreach(var sr in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if(sr.sprite!=null) {
                    var previewMaterial=new Material(sr.sharedMaterial);
                    previewMaterial.mainTexture=sr.sprite.texture;
                    sr.sharedMaterial=previewMaterial;
                }
            var rt=new RenderTexture(1600,900,24);rt.Create();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
            File.WriteAllBytes("painted-preview.png",image.EncodeToPNG());
            RenderTexture.active=null;rt.Release();
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
