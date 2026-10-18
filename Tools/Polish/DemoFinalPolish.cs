using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class DemoFinalPolish
{
    const string Enc="Assets/Prefabs/Encounters/";
    static Sprite rocks,trunk,plant;static Material material;
    static readonly Dictionary<Sprite,Bounds> opaqueBounds=new Dictionary<Sprite,Bounds>();
    static readonly HashSet<int> Woods=new HashSet<int>{124,125,143,144,162,163};
    static readonly HashSet<int> Rocks=new HashSet<int>{40,42,165,166};
    static int sceneryCount,trapCount;
    public static void Run()
    {
        try
        {
            AssetDatabase.Refresh();material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Painted/PaintedSprites.mat");
            rocks=Import("MossRock");trunk=Import("AncientStump");plant=Import("GlowPlant");
            foreach(var path in Directory.GetFiles("Assets/Resources/HollowSFX","*.wav"))
            {var i=(AudioImporter)AssetImporter.GetAtPath(path);i.forceToMono=true;var settings=i.defaultSampleSettings;settings.preloadAudioData=true;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.85f;i.defaultSampleSettings=settings;i.SaveAndReimport();}
            foreach(var path in Directory.GetFiles(Enc,"*.prefab").Concat(Directory.GetFiles("Assets/Prefabs/PaintedChunks","*.prefab")))
            {
                var root=PrefabUtility.LoadPrefabContents(path);bool changed=false;
                foreach(var boss in root.GetComponentsInChildren<RiftWarden>(true))
                {boss.GetComponent<EnemyHealth>().maxHealth=48;boss.slimePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Enc+"PouncingSlime.prefab");boss.mushroomPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Enc+"SporeKeeper.prefab");boss.beamMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Polish/Rift.mat");changed=true;}
                if(path.Contains("_Painted")){Dress(root);changed=true;}
                if(Path.GetFileName(path).StartsWith("07_")||Path.GetFileName(path).StartsWith("08_")||Path.GetFileName(path).StartsWith("11_"))
                {
                    float[] positions=Path.GetFileName(path).StartsWith("11_")?new[]{19f,31f}:new[]{15f};
                    foreach(float x in positions)if(root.transform.Find("Restored ice "+x)==null)
                    {
                        float? y=Ground(root,x);if(!y.HasValue)continue;
                        var trap=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Traps/IceTrap.prefab"));trap.transform.SetParent(root.transform,false);trap.name="Restored ice "+x;trap.transform.localPosition=new Vector3(x,y.Value+.5f);trapCount++;changed=true;
                    }
                }
                foreach(var marker in root.GetComponentsInChildren<TrailMarker>(true))
                {
                    if(marker.kind==TrailMarker.MarkerKind.Hint&&marker.message.Contains("claim the spirit")){marker.message=marker.message.Replace("claim the spirit at the altar with E","walk into the spirit to collect it");changed=true;}
                }
                if(changed)PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
            }
            foreach(var path in new[]{"Assets/Scenes/PaintedDimension.unity","Assets/Scenes/SampleScene.unity"})
            {
                var scene=EditorSceneManager.OpenScene(path);var player=Object.FindAnyObjectByType<PlayerHealth>();player.GetComponent<SpriteRenderer>().sortingOrder=50;
                // Keep accelerated and immediate modes; faster ramp only affects the optional accelerated mode.
                player.GetComponent<PlayerMovement>().acceleration=35;
                if(path.Contains("Painted"))foreach(var root in scene.GetRootGameObjects())Dress(root);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();File.WriteAllText("demo-final-polish-setup.txt","Scenery groups: "+sceneryCount+"\nRestored ice traps (both styles): "+trapCount+"\nBoss health: 48\n");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static Sprite Import(string name)
    {
        string path="Assets/Art/Scenery/"+name+".png";var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=100;i.alphaIsTransparency=true;i.mipmapEnabled=false;i.filterMode=FilterMode.Bilinear;i.textureCompression=TextureImporterCompression.Uncompressed;i.isReadable=true;i.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();int x0=texture.width,y0=texture.height,x1=0,y1=0;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>64){x0=Mathf.Min(x0,x);y0=Mathf.Min(y0,y);x1=Mathf.Max(x1,x);y1=Mathf.Max(y1,y);}
        i.isReadable=false;i.SaveAndReimport();var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        opaqueBounds[sprite]=new Bounds(new Vector3((x0+x1+1)*.5f-sprite.pivot.x,(y0+y1+1)*.5f-sprite.pivot.y)/sprite.pixelsPerUnit,new Vector3(x1+1-x0,y1+1-y0)/sprite.pixelsPerUnit);return sprite;
    }
    static int Id(Sprite sprite){if(sprite==null||!sprite.name.StartsWith("Snowy"))return -1;return int.TryParse(sprite.name.Substring(sprite.name.LastIndexOf('_')+1),out int id)?id:-1;}
    static void Dress(GameObject root)
    {
        foreach(var map in root.GetComponentsInChildren<Tilemap>(true))
        {
            var renderer=map.GetComponent<TilemapRenderer>();if(renderer==null||!renderer.enabled)continue;
            var previous=map.transform.Find("Painted scenery");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
            var selected=new HashSet<Vector3Int>();foreach(var c in map.cellBounds.allPositionsWithin)if(Woods.Contains(Id(map.GetSprite(c)))||Rocks.Contains(Id(map.GetSprite(c))))selected.Add(c);
            if(selected.Count==0)continue;
            var group=new GameObject("Painted scenery");group.transform.SetParent(map.transform,false);
            while(selected.Count>0)
            {
                var first=selected.First();bool wood=Woods.Contains(Id(map.GetSprite(first)));var cells=new List<Vector3Int>();var queue=new Queue<Vector3Int>();queue.Enqueue(first);selected.Remove(first);
                while(queue.Count>0)
                {var c=queue.Dequeue();cells.Add(c);foreach(var d in new[]{Vector3Int.left,Vector3Int.right,Vector3Int.up,Vector3Int.down}){var n=c+d;if(selected.Contains(n)&&(!wood||n.x==first.x)&&Woods.Contains(Id(map.GetSprite(n)))==wood){selected.Remove(n);queue.Enqueue(n);}}}
                Vector3 min=Vector3.one*float.PositiveInfinity,max=Vector3.one*float.NegativeInfinity;
                foreach(var cell in cells)
                {
                    var spriteBounds=map.GetSprite(cell).bounds;var matrix=map.GetTransformMatrix(cell);var center=map.GetCellCenterLocal(cell);
                    for(int a=0;a<2;a++)for(int b=0;b<2;b++)
                    {var corner=center+matrix.MultiplyPoint3x4(new Vector3(a==0?spriteBounds.min.x:spriteBounds.max.x,b==0?spriteBounds.min.y:spriteBounds.max.y,0));min=Vector3.Min(min,corner);max=Vector3.Max(max,corner);}
                    var source=map.GetTile<Tile>(cell);if(source!=null)
                    {
                        string dir="Assets/Art/Scenery/HiddenTiles";Directory.CreateDirectory(dir);string path=dir+"/"+source.name.Replace("Hidden_","")+".asset";
                        var hidden=AssetDatabase.LoadAssetAtPath<Tile>(path);
                        if(hidden==null){hidden=Object.Instantiate(source);hidden.name="Hidden_"+source.name;hidden.color=Color.clear;hidden.flags=TileFlags.LockColor;AssetDatabase.CreateAsset(hidden,path);}
                        map.SetTile(cell,hidden);map.SetTransformMatrix(cell,matrix);
                    }
                }
                File.AppendAllText("scenery-groups.txt",root.name+"/"+map.name+" wood="+wood+" cells="+string.Join(";",cells.Select(c=>c.ToString()+":"+Id(map.GetSprite(c))))+" bounds="+min+".."+max+"\n");
                var sprite=wood?trunk:rocks;var g=new GameObject(wood?"Ancient root column":"Moss-covered stones");g.transform.SetParent(group.transform,false);g.transform.localPosition=new Vector3((min.x+max.x)*.5f,(min.y+max.y)*.5f,.2f);
                var sr=g.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;sr.sortingOrder=0;sr.color=wood?Color.white:new Color(1.5f,1.7f,1.9f,1);
                var visible=opaqueBounds[sprite];g.transform.localScale=new Vector3((max.x-min.x)/visible.size.x,(max.y-min.y)/visible.size.y,1);g.transform.localPosition-=Vector3.Scale(visible.center,g.transform.localScale);
                if(wood&&max.y-min.y>2)
                {var sprout=new GameObject("Root growth");sprout.transform.SetParent(group.transform,false);sprout.transform.localPosition=new Vector3(min.x-.18f,min.y+1,.4f);var leaves=sprout.AddComponent<SpriteRenderer>();leaves.sprite=plant;leaves.sharedMaterial=material;leaves.sortingOrder=-2;leaves.color=new Color(1.4f,1.5f,1.7f);sprout.transform.localScale=Vector3.one*.4f;}
                sceneryCount++;
            }
        }
    }
    static float? Ground(GameObject root,float x)
    {
        foreach(var map in root.GetComponentsInChildren<Tilemap>(true).Where(m=>!m.name.Contains("Original props")))
        for(int y=3;y>=-7;y--){var c=map.WorldToCell(root.transform.position+new Vector3(x,y+.1f));if(PaintedVariantBuilder.IsTerrain(map.GetSprite(c)))return map.CellToWorld(c).y+1-root.transform.position.y;}
        return null;
    }
}
