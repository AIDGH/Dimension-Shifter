using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public static class ExpansionSetup
{
    const string Folder="Assets/Prefabs/Encounters";
    static Material spriteMaterial;
    static Tile Tile(int id) => AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.FindAssets("t:Tile",new[]{"Assets/Tiles"}).Select(AssetDatabase.GUIDToAssetPath).First(p=>p.EndsWith("/Snowy Tundra 16x16 Tileset_"+id+".asset")));
    static GameObject Child(string name,Transform parent,Vector3 position)
    {var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;return g;}
    static SpriteRenderer Sprite(string name,Transform parent,Vector3 position,Sprite sprite,Vector3 scale,Color color)
    {var g=Child(name,parent,position);g.transform.localScale=scale;var sr=g.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=spriteMaterial;sr.sortingOrder=15;sr.color=color;return sr;}
    static GameObject Save(GameObject g,string name){var p=PrefabUtility.SaveAsPrefabAsset(g,Folder+"/"+name+".prefab");Object.DestroyImmediate(g);return p;}
    public static void Run()
    {
        try
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            spriteMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Painted/PaintedSprites.mat");
            EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");
            var source=Object.FindFirstObjectByType<EnemyHealth>();
            var leap=Object.Instantiate(source.gameObject);leap.name="Pouncing slime";leap.transform.SetParent(null);leap.transform.position=Vector3.zero;leap.transform.localScale=Vector3.one;
            Object.DestroyImmediate(leap.GetComponent<EnemyHealth>());
            Object.DestroyImmediate(leap.GetComponent<EnemyPatrol>());
            foreach(var c in leap.GetComponents<Collider2D>())Object.DestroyImmediate(c);
            var box=leap.AddComponent<BoxCollider2D>();box.size=new Vector2(1.875f,1f);box.offset=Vector2.zero;
            leap.AddComponent<EnemyHealth>();
            var rb=leap.GetComponent<Rigidbody2D>();rb.gravityScale=2;rb.constraints=RigidbodyConstraints2D.FreezeRotation;
            var ai=leap.AddComponent<EnemyAttackAI>();ai.warning=Sprite("Jump warning",leap.transform,new Vector3(0,1.3f),Tile(52).sprite,new Vector3(.5f,1.8f,1),new Color(1,.65f,.2f));ai.warning.enabled=false;
            var leaper=Save(leap,"PouncingSlime");
            var shot=new GameObject("Spore");Sprite("Spore crystal",shot.transform,Vector3.zero,Tile(52).sprite,Vector3.one*.26f,new Color(1,.55f,.85f));shot.AddComponent<EnemyProjectile>();
            var projectile=Save(shot,"SporeProjectile").GetComponent<EnemyProjectile>();
            var mushroom=new GameObject("Spore keeper");mushroom.layer=10;mushroom.tag="Enemy";
            var mbody=mushroom.AddComponent<Rigidbody2D>();mbody.gravityScale=2;mbody.constraints=RigidbodyConstraints2D.FreezeRotation;mbody.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            var mbox=mushroom.AddComponent<BoxCollider2D>();mbox.size=new Vector2(1.05f,.95f);mbox.offset=new Vector2(0,.48f);
            string sheet="Assets/Sprites/Enemies/Haunted Forest Enemy Asset Pack/Mushroom/Mushroom Idle/No Shadow/Mushroom Idle No Shadow Spritesheet.png";
            var frames=AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().Where(s=>s.rect.width>100).OrderByDescending(s=>Mathf.Round(s.rect.y/400)).ThenBy(s=>s.rect.x).ToArray();
            var visual=Sprite("Mushroom",mushroom.transform,Vector3.up*.6f,frames[0],Vector3.one*.45f,Color.white);
            mushroom.AddComponent<EnemyHealth>().maxHealth=4;
            var flip=mushroom.AddComponent<EnemyFlipbook>();flip.idleFrames=frames;flip.target=visual;
            var shooter=mushroom.AddComponent<EnemyAttackAI>();shooter.kind=EnemyAttackAI.AttackKind.Spore;shooter.range=8;shooter.windup=.8f;shooter.recovery=2.4f;shooter.projectilePrefab=projectile;
            shooter.muzzle=Child("Muzzle",mushroom.transform,Vector3.up*.65f).transform;
            shooter.warning=Sprite("Spore warning",mushroom.transform,Vector3.up*1.6f,Tile(52).sprite,new Vector3(.5f,1.8f,1),new Color(1,.65f,.2f));shooter.warning.enabled=false;
            var mushroomPrefab=Save(mushroom,"SporeKeeper");
            var originals=new GameObject[3];var painted=new GameObject[3];
            string[] names={"07_LeapGarden","08_SporePass","09_RiftCrossing"};
            for(int i=0;i<3;i++)
            {
                var root=new GameObject(names[i]);root.AddComponent<Grid>();Child("EndPosition",root.transform,Vector3.right*24);
                var ground=Child("Terrain",root.transform,Vector3.zero);ground.layer=8;var map=ground.AddComponent<Tilemap>();var renderer=ground.AddComponent<TilemapRenderer>();renderer.sharedMaterial=spriteMaterial;
                for(int x=0;x<24;x++)for(int y=-3;y<0;y++)map.SetTile(new Vector3Int(x,y,0),Tile(y==-1?49:84));
                var floor=ground.AddComponent<BoxCollider2D>();floor.size=new Vector2(24,3);floor.offset=new Vector2(12,-1.5f);
                var props=Child("Garden crystals",root.transform,Vector3.zero);var propmap=props.AddComponent<Tilemap>();props.AddComponent<TilemapRenderer>().sharedMaterial=spriteMaterial;
                foreach(int x in new[]{1,5,19,22})propmap.SetTile(new Vector3Int(x,0,0),Tile(52));
                Marker(root.transform,new Vector3(2,.9f),TrailMarker.MarkerKind.Checkpoint,i==0?"Leap garden":i==1?"Spore pass":"Rift crossing");
                if(i==0)
                {
                    Place(leaper,root.transform,new Vector3(10,.55f));Place(leaper,root.transform,new Vector3(19,.55f));
                    Block(map,ground,13,0,3,1);Block(map,ground,14,1,2,1);
                    Marker(root.transform,new Vector3(5,.8f),TrailMarker.MarkerKind.Hint,"Watch the amber warning. Jump or use B to pass through a leaping slime.");
                }
                else if(i==1)
                {
                    Block(map,ground,7,0,2,2);Block(map,ground,16,0,2,2);
                    Place(mushroomPrefab,root.transform,new Vector3(13,.05f));Place(mushroomPrefab,root.transform,new Vector3(21,.05f));
                    Hazard(root.transform,new Vector3(10,0),0,false);
                    Marker(root.transform,new Vector3(5,.8f),TrailMarker.MarkerKind.Hint,"Spore keepers aim before firing. Use stone cover, jump, or dash through a shot.");
                }
                else
                {
                    Gate(root.transform,new Vector3(8,0),0);Gate(root.transform,new Vector3(16,0),1);
                    Platform(root.transform,new Vector3(11,1.5f),1);Platform(root.transform,new Vector3(19,1.5f),0);
                    Hazard(root.transform,new Vector3(11.3f,0),0,true);Hazard(root.transform,new Vector3(19.3f,0),1,true);
                    Marker(root.transform,new Vector3(4,.8f),TrailMarker.MarkerKind.Hint,"Q shifts Frost / Echo. Ghost barriers have no collision. Cyan and violet platforms belong to different worlds.");
                }
                foreach(var cell in map.cellBounds.allPositionsWithin)if(map.HasTile(cell))map.SetColliderType(cell,UnityEngine.Tilemaps.Tile.ColliderType.None);
                originals[i]=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+names[i]+".prefab");
                PaintedVariantBuilder.PaintExpansion(root,names[i]);
                painted[i]=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+names[i]+"_Painted.prefab");Object.DestroyImmediate(root);
            }
            foreach(string path in new[]{"Assets/Scenes/SampleScene.unity","Assets/Scenes/PaintedDimension.unity"})
            {
                var scene=EditorSceneManager.OpenScene(path);bool isPainted=path.Contains("Painted");
                var player=Object.FindFirstObjectByType<PlayerHealth>();
                if(!player.GetComponent<DimensionController>())player.gameObject.AddComponent<DimensionController>();
                if(!player.GetComponent<RunProgress>())player.gameObject.AddComponent<RunProgress>();
                var generator=Object.FindFirstObjectByType<LevelGenerator>();generator.encounterChunks=isPainted?painted:originals;generator.cameraFollow.maxX=170;
                var endSource=AssetDatabase.LoadAssetAtPath<GameObject>(isPainted?"Assets/Prefabs/PaintedChunks/End_Chunk_01_Painted.prefab":"Assets/Prefabs/Chunks/End/End_Chunk_01.prefab");
                var end=PrefabUtility.InstantiatePrefab(endSource) as GameObject;end.name="Trail exit";
                Marker(end.transform,new Vector3(36,.9f),TrailMarker.MarkerKind.Exit,"Rift sealed",true);
                generator.endChunks=new[]{Save(end,isPainted?"TrailEnd_Painted":"TrailEnd")};
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();File.WriteAllText("expansion-setup.txt","Built three encounter chunks in both styles, leaper and spore keeper prefabs, dimension gates/platforms, timed traps, checkpoints and level exit.\n");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Place(GameObject prefab,Transform parent,Vector3 position){var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);g.transform.SetParent(parent,false);g.transform.localPosition=position;}
    static void Block(Tilemap map,GameObject ground,int x,int y,int w,int h)
    {for(int xx=x;xx<x+w;xx++)for(int yy=y;yy<y+h;yy++)map.SetTile(new Vector3Int(xx,yy,0),Tile(yy==y+h-1?49:84));var c=ground.AddComponent<BoxCollider2D>();c.size=new Vector2(w,h);c.offset=new Vector2(x+w*.5f,y+h*.5f);}
    static void Marker(Transform parent,Vector3 position,TrailMarker.MarkerKind kind,string text,bool ground=false)
    {
        var g=Child(kind.ToString(),parent,position);var c=g.AddComponent<BoxCollider2D>();c.isTrigger=true;c.size=new Vector2(1.4f,2);
        var marker=g.AddComponent<TrailMarker>();marker.kind=kind;marker.message=text;marker.alignToGround=ground;
        marker.spawnPoint=Child("Safe respawn",g.transform,new Vector3(-.8f,-.15f)).transform;
        if(kind!=TrailMarker.MarkerKind.Hint)marker.beacon=Sprite("Beacon",g.transform,Vector3.zero,Tile(52).sprite,new Vector3(.8f,1.6f,1),kind==TrailMarker.MarkerKind.Exit?new Color(1,.8f,.45f):new Color(.4f,1,1));
    }
    static void Gate(Transform parent,Vector3 position,int dimension)
    {
        var g=Child(dimension==0?"Frost seal":"Echo seal",parent,position);g.layer=9;
        var box=g.AddComponent<BoxCollider2D>();box.size=new Vector2(.65f,7);box.offset=new Vector2(0,3.5f);
        var beam=Child("Rift beam",g.transform,Vector3.zero).AddComponent<LineRenderer>();beam.useWorldSpace=false;beam.sharedMaterial=spriteMaterial;beam.positionCount=2;beam.SetPosition(0,new Vector3(0,0,-.02f));beam.SetPosition(1,new Vector3(0,7,-.02f));beam.startWidth=beam.endWidth=.65f;beam.startColor=beam.endColor=dimension==0?new Color(.3f,.9f,1,.45f):new Color(.9f,.4f,1,.45f);beam.sortingOrder=12;
        for(int i=0;i<6;i++)Sprite("Seal crystal",g.transform,new Vector3(0,i*1.1f+.55f),Tile(52).sprite,new Vector3(.8f,1.2f,1),dimension==0?new Color(.4f,1,1):new Color(.85f,.5f,1));
        g.AddComponent<DimensionMember>().dimension=dimension;
    }
    static void Platform(Transform parent,Vector3 position,int dimension)
    {
        var g=Child("Dimension ledge",parent,position);g.layer=8;var box=g.AddComponent<BoxCollider2D>();box.size=new Vector2(3,.4f);box.offset=new Vector2(1.5f,-.2f);
        var tiles=Child(dimension==0?"Frost ledge terrain":"Echo ledge terrain",g.transform,new Vector3(0,-.4f));tiles.transform.localScale=new Vector3(1,.4f,1);var map=tiles.AddComponent<Tilemap>();tiles.AddComponent<TilemapRenderer>().sharedMaterial=spriteMaterial;
        for(int i=0;i<3;i++){var cell=new Vector3Int(i,0,0);map.SetTile(cell,Tile(49));map.SetColliderType(cell,UnityEngine.Tilemaps.Tile.ColliderType.None);}
        g.AddComponent<DimensionMember>().dimension=dimension;
    }
    static void Hazard(Transform parent,Vector3 position,int dimension,bool phased)
    {
        var g=Child("Pulse thorns",parent,position);var c=g.AddComponent<BoxCollider2D>();c.isTrigger=true;c.size=new Vector2(1.4f,.85f);c.offset=new Vector2(0,.4f);
        var h=g.AddComponent<TimedHazard>();h.phaseOffset=dimension*1.4f;
        h.warning=Sprite("Warning bed",g.transform,new Vector3(0,.05f),Tile(52).sprite,new Vector3(1.5f,.15f,1),Color.white);
        h.spikes=Child("Thorns",g.transform,Vector3.zero).transform;
        for(int i=0;i<3;i++)Sprite("Thorn",h.spikes,new Vector3((i-1)*.45f,.4f),Tile(52).sprite,new Vector3(.5f,.85f,1),new Color(1,.4f,.55f));
        if(phased)g.AddComponent<DimensionMember>().dimension=dimension;
    }
}
