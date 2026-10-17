using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

// Called at the end of PolishSetup; can also refine the already-built polish project.
public static class RouteRefinementSetup
{
    const string Enc = "Assets/Prefabs/Encounters/";
    const string Art = "Assets/Art/Polish/";
    static Material sprites, rift, sanctuary;
    static Mesh strip;
    static GameObject checkpointTemplate, crystalsTemplate;
    static GameObject[] hazardTemplates;
    static Tile Tile(int id) => AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.FindAssets("t:Tile", new[]{"Assets/Tiles"}).Select(AssetDatabase.GUIDToAssetPath).First(p=>p.EndsWith("/Snowy Tundra 16x16 Tileset_"+id+".asset")));
    static GameObject Child(string name, Transform parent, Vector3 position)
    { var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;return g; }
    static void Asset(Object value,string path)
    { var old=AssetDatabase.LoadMainAssetAtPath(path);if(old!=null){EditorUtility.CopySerialized(value,old);EditorUtility.SetDirty(old);Object.DestroyImmediate(value);}else AssetDatabase.CreateAsset(value,path); }
    public static void Run(){try{Apply();AssetDatabase.SaveAssets();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void Apply()
    {
        sprites=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Painted/PaintedSprites.mat");
        rift=AssetDatabase.LoadAssetAtPath<Material>(Art+"Rift.mat");rift.SetColor("_Tint",Color.white);EditorUtility.SetDirty(rift);
        Asset(new Material(Shader.Find("DimensionShifter/Sanctuary")),Art+"Sanctuary.mat");sanctuary=AssetDatabase.LoadAssetAtPath<Material>(Art+"Sanctuary.mat");
        var mesh=new Mesh {name="Unit rift strip"};mesh.vertices=new[]{new Vector3(-.45f,0),new Vector3(.45f,0),new Vector3(.45f,1),new Vector3(-.45f,1)};
        mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();Asset(mesh,Art+"RiftStrip.asset");strip=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"RiftStrip.asset");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Enc+"07_LeapGarden.prefab");
        checkpointTemplate=Object.Instantiate(source.GetComponentsInChildren<TrailMarker>().First(m=>m.kind==TrailMarker.MarkerKind.Checkpoint).gameObject);
        checkpointTemplate.SetActive(false);
        var previous=AssetDatabase.LoadAssetAtPath<GameObject>(Enc+"09_RiftCrossing.prefab");
        hazardTemplates=previous.GetComponentsInChildren<TimedHazard>(true).Select(h=>Object.Instantiate(h.gameObject)).ToArray();
        foreach(var hazard in hazardTemplates)hazard.SetActive(false);
        var crystals=previous.transform.Find("Garden crystals");if(crystals!=null){crystalsTemplate=Object.Instantiate(crystals.gameObject);crystalsTemplate.SetActive(false);}
        BuildCrossing();BuildVault();Object.DestroyImmediate(checkpointTemplate);
        foreach(var hazard in hazardTemplates)Object.DestroyImmediate(hazard);
        if(crystalsTemplate!=null)Object.DestroyImmediate(crystalsTemplate);
        foreach(bool down in new[]{false,true})RefineUnderside(down);
        foreach(string path in Directory.GetFiles(Enc,"*.prefab"))
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            foreach(var shrine in root.GetComponentsInChildren<ShrineVisual>(true))shrine.shrine.sharedMaterial=sanctuary;
            PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
        foreach(string path in new[]{"Assets/Scenes/SampleScene.unity","Assets/Scenes/PaintedDimension.unity"})
        {
            var scene=EditorSceneManager.OpenScene(path);
            foreach(var slime in Object.FindObjectsByType<EnemyPatrol>())
            {
                var box=slime.GetComponent<BoxCollider2D>();
                if(box==null)throw new Exception("Intro slime is missing its body collider");
                box.enabled=true;box.isTrigger=false;
                var body=slime.GetComponent<Rigidbody2D>();body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            }
            var gen=Object.FindAnyObjectByType<LevelGenerator>();string suffix=path.Contains("Painted")?"_Painted":"";
            gen.encounterChunks=new[]{"07_LeapGarden","10_CrystalAscent","08_SporePass","09_RiftCrossing","12_RiftVault","11_HollowDescent"}.Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>(Enc+n+suffix+".prefab")).ToArray();
            gen.cameraFollow.maxX=320;EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();File.WriteAllText("route-refinement-setup.txt","Enabled the intro slime body; assigned dimension-aware shrines; built wider crossing and sealed relic vault; sloped both vertical undersides; six authored encounters in both scenes.\n");
    }
    static GameObject Root(string name,int width)
    {var root=new GameObject(name);root.AddComponent<Grid>();Child("EndPosition",root.transform,new Vector3(width,0));return root;}
    static Tilemap Terrain(Transform parent,string name)
    {var go=Child(name,parent,Vector3.zero);go.layer=8;var map=go.AddComponent<Tilemap>();go.AddComponent<TilemapRenderer>().sharedMaterial=sprites;return map;}
    static void Block(Tilemap map,int x,int y,int w,int h)
    {
        for(int xx=x;xx<x+w;xx++)for(int yy=y;yy<y+h;yy++){var cell=new Vector3Int(xx,yy,0);map.SetTile(cell,Tile(yy==y+h-1?49:84));map.SetColliderType(cell,UnityEngine.Tilemaps.Tile.ColliderType.None);}
        var box=map.gameObject.AddComponent<BoxCollider2D>();box.size=new Vector2(w,h);box.offset=new Vector2(x+w*.5f,y+h*.5f);
    }
    static void Checkpoint(Transform parent,string label)
    {var g=Object.Instantiate(checkpointTemplate,parent);g.name="Checkpoint";g.transform.localPosition=new Vector3(2,.9f);g.SetActive(true);g.GetComponent<TrailMarker>().message=label;}
    static void Hint(Transform parent,float x,string text)
    {var g=Child("Route hint",parent,new Vector3(x,.8f));var box=g.AddComponent<BoxCollider2D>();box.isTrigger=true;box.size=new Vector2(1.4f,2);var m=g.AddComponent<TrailMarker>();m.kind=TrailMarker.MarkerKind.Hint;m.message=text;}
    static void Relic(Transform parent,Vector3 p,int points,int xp,int dimension=-1)
    {
        var g=Child("Sealed relic",parent,p);var col=g.AddComponent<CircleCollider2D>();col.radius=.35f;col.isTrigger=true;
        var item=g.AddComponent<ExperiencePickup>();item.points=points;item.experience=xp;item.dimension=dimension;item.visual=Child("Relic light",g.transform,Vector3.zero).transform;
        var line=item.visual.gameObject.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.sharedMaterial=sprites;line.positionCount=4;
        line.SetPositions(new[]{new Vector3(0,.3f),new Vector3(.2f,0),new Vector3(0,-.3f),new Vector3(-.2f,0)});line.startWidth=line.endWidth=.06f;line.startColor=line.endColor=new Color(1,.8f,.4f);line.sortingOrder=20;
    }
    static void Barrier(Transform parent,string name,int dimension,params Vector2[] points)
    {
        var g=Child(name,parent,points[0]);g.layer=9;
        for(int i=1;i<points.Length;i++)
        {
            var a=points[i-1];var d=points[i]-a;float length=d.magnitude;
            var segment=Child("Energy segment",g.transform,a-points[0]);segment.layer=9;segment.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg-90);
            var box=segment.AddComponent<BoxCollider2D>();box.size=new Vector2(.65f,length);box.offset=new Vector2(0,length*.5f);
            var visual=Child("Living energy seam",segment.transform,Vector3.zero);visual.transform.localScale=new Vector3(1,length,1);
            visual.AddComponent<MeshFilter>().sharedMesh=strip;var renderer=visual.AddComponent<MeshRenderer>();renderer.sharedMaterial=rift;renderer.sortingOrder=15;
        }
        g.AddComponent<DimensionMember>().dimension=dimension;
    }
    static void SaveBoth(GameObject root)
    {string name=root.name;PrefabUtility.SaveAsPrefabAsset(root,Enc+name+".prefab");PaintedVariantBuilder.PaintExpansion(root,name);PrefabUtility.SaveAsPrefabAsset(root,Enc+name+"_Painted.prefab");Object.DestroyImmediate(root);}
    static void BuildCrossing()
    {
        var root=Root("09_RiftCrossing",42);var ground=Terrain(root.transform,"Rift terrain");Block(ground,0,-3,42,3);Checkpoint(root.transform,"Rift crossing");
        Barrier(root.transform,"Frost seal",0,new Vector2(8,-.5f),new Vector2(8,40));
        Barrier(root.transform,"Echo seal",1,new Vector2(32,-.5f),new Vector2(32,40));
        for(int i=0;i<hazardTemplates.Length;i++){var h=Object.Instantiate(hazardTemplates[i],root.transform);h.name="Pulse thorns";h.transform.localPosition=new Vector3(i==0?11.3f:35.3f,0);h.SetActive(true);}
        if(crystalsTemplate!=null){var props=Object.Instantiate(crystalsTemplate,root.transform);props.name="Garden crystals";props.transform.localPosition=Vector3.zero;props.SetActive(true);}
        // A horizontal membrane is a platform in Echo, and can be crossed in Frost.
        Block(ground,14,0,3,2);Block(ground,26,0,3,2);
        Barrier(root.transform,"Horizontal Echo seal",1,new Vector2(18,3.5f),new Vector2(24,3.5f));
        Relic(root.transform,new Vector3(21,4.4f),150,50,1);
        Relic(root.transform,new Vector3(21,1.2f),75,25,0);
        Hint(root.transform,4,"Q changes which seals are solid. Tall walls cannot be jumped over; wait for SHIFT READY before the next seal.");
        Hint(root.transform,15,"The horizontal membrane is a bridge in Echo. In Frost you can pass through it to reach the relic below.");
        SaveBoth(root);
    }
    static void BuildVault()
    {
        var root=Root("12_RiftVault",36);var ground=Terrain(root.transform,"Vault terrain");Block(ground,0,-3,36,3);Checkpoint(root.transform,"Sealed vault");
        Barrier(root.transform,"Frost seal",0,new Vector2(6,-.5f),new Vector2(6,40));
        // Ground closes the bottom of this three-sided seal. Its treasure requires a shift.
        Barrier(root.transform,"Echo vault seal",1,new Vector2(17,0),new Vector2(17,4.5f),new Vector2(23,4.5f),new Vector2(23,0));
        Relic(root.transform,new Vector3(20,1.3f),200,60);
        Block(ground,28,0,3,2);Relic(root.transform,new Vector3(29.5f,2.9f),75,25);
        Hint(root.transform,3,"Cross the cyan seal in Echo. Farther ahead, shift to Frost to enter the sealed relic chamber.");
        Hint(root.transform,14,"Violet seals enclose the treasure on three sides. In Frost the shell becomes a ghost, including its roof.");
        SaveBoth(root);
    }
    static void RefineUnderside(bool down)
    {
        string name=down?"11_HollowDescent":"10_CrystalAscent";var root=PrefabUtility.LoadPrefabContents(Enc+name+".prefab");
        var terrain=root.transform.Find("Tower terrain").gameObject;var map=terrain.GetComponent<Tilemap>();map.ClearAllTiles();
        foreach(var col in terrain.GetComponents<Collider2D>())Object.DestroyImmediate(col);
        for(int x=0;x<18;x++)
        {
            int top=(down?-1:1)*Mathf.Min(3,x/4)*2;
            float left=-4+(down?-1:1)*Mathf.Min(12,x)*.5f;
            float right=-4+(down?-1:1)*Mathf.Min(12,x+1)*.5f;
            int bottom=Mathf.FloorToInt(Mathf.Min(left,right));
            for(int y=bottom;y<top;y++)
            {
                int id=y==top-1?49:84;
                if(y==bottom&&x<12)id=down?(x%2==0?104:105):(x%2==0?107:108);
                var cell=new Vector3Int(x,y,0);map.SetTile(cell,Tile(id));map.SetColliderType(cell,UnityEngine.Tilemaps.Tile.ColliderType.None);
            }
            var shape=terrain.AddComponent<PolygonCollider2D>();shape.points=new[]{new Vector2(x,left),new Vector2(x+1,right),new Vector2(x+1,top),new Vector2(x,top)};
        }
        PrefabUtility.SaveAsPrefabAsset(root,Enc+name+".prefab");
        PaintedVariantBuilder.PaintExpansion(root,name);PrefabUtility.SaveAsPrefabAsset(root,Enc+name+"_Painted.prefab");PrefabUtility.UnloadPrefabContents(root);
    }
}
