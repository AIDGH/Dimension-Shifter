using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class DemoSetup
{
    const string Art="Assets/Art/Demo/",Enc="Assets/Prefabs/Encounters/";
    static Material material,rift;static Sprite warning,rune,key,door;static GameObject checkpoint;
    static GameObject Child(string name,Transform parent,Vector3 p){var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=p;return g;}
    static Tile Tile(int id)=>AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.FindAssets("t:Tile",new[]{"Assets/Tiles"}).Select(AssetDatabase.GUIDToAssetPath).First(p=>p.EndsWith("/Snowy Tundra 16x16 Tileset_"+id+".asset")));
    static Sprite[] Frames(string file)=>AssetDatabase.LoadAllAssetsAtPath(Art+file+".png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
    static SpriteRenderer Visual(string name,Transform parent,Vector3 p,Sprite sprite,float scale=1)
    {var g=Child(name,parent,p);g.transform.localScale=Vector3.one*scale;var sr=g.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;sr.sortingOrder=20;return sr;}
    static void Import(string name,int columns,float ppu,bool feet=false)
    {
        string path=Art+name+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=columns==1?SpriteImportMode.Single:SpriteImportMode.Multiple;importer.spritePixelsPerUnit=ppu;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=16384;importer.isReadable=true;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);int w=texture.width/columns,h=texture.height;float bottom=0;
        if(feet){var pixels=texture.GetPixels32();int min=h;for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(pixels[y*texture.width+x].a>50){min=Mathf.Min(min,y);break;}bottom=(float)min/h;}
        if(columns>1)
        {
            var factories=new SpriteDataProviderFactories();factories.Init();var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();var old=provider.GetSpriteRects();
            var slices=new SpriteRect[columns];for(int i=0;i<columns;i++){string label=name+"_"+i.ToString("D2");var previous=old.FirstOrDefault(r=>r.name==label);slices[i]=new SpriteRect{name=label,rect=new Rect(i*w,0,w,h),alignment=SpriteAlignment.Custom,pivot=new Vector2(.5f,feet?bottom:.5f),spriteID=previous!=null?previous.spriteID:GUID.Generate()};}
            provider.SetSpriteRects(slices);provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(slices.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));provider.Apply();
        }
        else {var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,feet?bottom:.5f);importer.SetTextureSettings(settings);}
        // Slice at original resolution, then cap the runtime texture for 8192-limit GPUs.
        importer.maxTextureSize=8192;importer.SaveAndReimport();
    }
    static void Symbols()
    {
        foreach(bool mark in new[]{true,false})
        {int w=64,h=96;var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);var pixels=new Color[w*h];
         for(int y=0;y<h;y++)for(int x=0;x<w;x++){bool ink=mark?((Mathf.Abs(x-31.5f)<6&&y>36&&y<84)||new Vector2(x-31.5f,y-20).magnitude<7):Mathf.Abs(Mathf.Abs(x-31.5f)/25+Mathf.Abs(y-47.5f)/37-1)<.14f;pixels[y*w+x]=ink?Color.white:Color.clear;}
         tex.SetPixels(pixels);tex.Apply();File.WriteAllBytes(Art+(mark?"Warning":"Rune")+".png",tex.EncodeToPNG());Object.DestroyImmediate(tex);}
        AssetDatabase.Refresh();Import("Warning",1,95);Import("Rune",1,64);warning=Frames("Warning")[0];rune=Frames("Rune")[0];
    }
    public static void Run()
    {
        try
        {
            material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Painted/PaintedSprites.mat");rift=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Polish/Rift.mat");Symbols();
            foreach(var n in new[]{"Idle","Run","Stunned"})Import("Keeper"+n,5,200,true);Import("KeeperAttack",9,200,true);Import("KeeperDeath",13,200,true);Import("SpiritBolt",4,260);Import("EchoKey",1,170);Import("SanctumDoor",1,260,true);key=Frames("EchoKey")[0];door=Frames("SanctumDoor")[0];
            // All warning renderers, including embedded instances in existing authored chunks.
            foreach(string path in Directory.GetFiles(Enc,"*.prefab"))
            {
                var root=PrefabUtility.LoadPrefabContents(path);
                if(root.GetComponent<EnemyProjectile>()!=null){var pellet=root.GetComponentInChildren<SpriteRenderer>();pellet.sprite=rune;pellet.transform.localScale=Vector3.one*.3f;}
                foreach(var ai in root.GetComponentsInChildren<EnemyAttackAI>(true))if(ai.warning!=null){ai.warning.sprite=warning;ai.warning.transform.localScale=Vector3.one*.8f;ai.warning.sharedMaterial=material;}
                PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
            }
            var cp=AssetDatabase.LoadAssetAtPath<GameObject>(Enc+"07_LeapGarden.prefab").GetComponentsInChildren<TrailMarker>().First(m=>m.kind==TrailMarker.MarkerKind.Checkpoint);
            checkpoint=Object.Instantiate(cp.gameObject);checkpoint.SetActive(false);
            BuildKeySanctum();BuildRelays();BuildTrial();BuildBoss();Object.DestroyImmediate(checkpoint);
            foreach(string scenePath in new[]{"Assets/Scenes/SampleScene.unity","Assets/Scenes/PaintedDimension.unity"})
            {
                var scene=EditorSceneManager.OpenScene(scenePath);bool painted=scenePath.Contains("Painted");string suffix=painted?"_Painted":"";
                foreach(var enemy in Object.FindObjectsByType<EnemyPatrol>())Object.DestroyImmediate(enemy.gameObject);
                var player=Object.FindAnyObjectByType<PlayerHealth>().gameObject;if(player.GetComponent<DemoSession>()==null)player.AddComponent<DemoSession>();var weapon=player.GetComponent<SpiritWeapon>();if(weapon==null)weapon=player.AddComponent<SpiritWeapon>();weapon.frames=Frames("SpiritBolt");
                var gen=Object.FindAnyObjectByType<LevelGenerator>();gen.authoredOnly=true;
                gen.encounterChunks=new[]{"07_LeapGarden","10_CrystalAscent","13_KeySanctum","11_HollowDescent","08_SporePass","09_RiftCrossing","14_RelayTrial","12_RiftVault","15_ProvingGrounds","16_WardenArena"}.Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>(Enc+n+suffix+".prefab")).ToArray();
                float length=gen.startChunks[0].transform.Find("EndPosition").localPosition.x+gen.encounterChunks.Sum(p=>p.transform.Find("EndPosition").localPosition.x)+gen.endChunks[0].transform.Find("EndPosition").localPosition.x;
                gen.cameraFollow.maxX=gen.startSpawnPoint.position.x+length-10;
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();File.WriteAllText("demo-setup-results.txt","Built key sanctuary, two-world relays, three-wave trial, Warden arena, start menu and run timer. Removed the standalone opening slime from both scenes.\n");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static GameObject Root(string name,int width){var root=new GameObject(name);root.AddComponent<Grid>();Child("EndPosition",root.transform,new Vector3(width,0));return root;}
    static Tilemap Map(Transform parent){var g=Child("Demo terrain",parent,Vector3.zero);g.layer=8;var map=g.AddComponent<Tilemap>();g.AddComponent<TilemapRenderer>().sharedMaterial=material;return map;}
    static void Block(Tilemap map,int x,int y,int w,int h)
    {for(int xx=x;xx<x+w;xx++)for(int yy=y;yy<y+h;yy++){var p=new Vector3Int(xx,yy,0);map.SetTile(p,Tile(yy==y+h-1?49:84));map.SetColliderType(p,UnityEngine.Tilemaps.Tile.ColliderType.None);}var c=map.gameObject.AddComponent<BoxCollider2D>();c.size=new Vector2(w,h);c.offset=new Vector2(x+w*.5f,y+h*.5f);}
    static void Checkpoint(Transform parent,string label){var g=Object.Instantiate(checkpoint,parent);g.name="Checkpoint";g.transform.localPosition=new Vector3(2,.9f);g.GetComponent<TrailMarker>().message=label;g.SetActive(true);}
    static void Hint(Transform parent,float x,string text){var g=Child("Trail hint",parent,new Vector3(x,1));var c=g.AddComponent<BoxCollider2D>();c.isTrigger=true;c.size=new Vector2(1.5f,2);var m=g.AddComponent<TrailMarker>();m.message=text;}
    static void SaveBoth(GameObject root){string name=root.name;PrefabUtility.SaveAsPrefabAsset(root,Enc+name+".prefab");PaintedVariantBuilder.PaintExpansion(root,name);PrefabUtility.SaveAsPrefabAsset(root,Enc+name+"_Painted.prefab");Object.DestroyImmediate(root);}
    static QuestInteractable Item(Transform root,string name,Vector3 p,QuestInteractable.Kind kind,int world,Sprite icon)
    {
        var g=Child(name,root,p);var q=g.AddComponent<QuestInteractable>();q.kind=kind;q.dimension=world;q.visual=Visual(name+" light",g.transform,Vector3.zero,icon);q.visual.color=world==0?new Color(.5f,1,1):world==1?new Color(1,.6f,1):new Color(1,.85f,.55f);
        if(kind==QuestInteractable.Kind.Relay){g.layer=11;var c=g.AddComponent<CircleCollider2D>();c.isTrigger=true;c.radius=.55f;}
        return q;
    }
    static BoxCollider2D Seal(Transform root,string name,float x,float y,float height,out MeshRenderer renderer)
    {
        var g=Child(name,root,new Vector3(x,y));g.layer=9;var box=g.AddComponent<BoxCollider2D>();box.size=new Vector2(.65f,height);box.offset=new Vector2(0,height*.5f);
        var v=Child("Energy",g.transform,Vector3.zero);v.transform.localScale=new Vector3(1,height,1);v.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Polish/RiftStrip.asset");renderer=v.AddComponent<MeshRenderer>();renderer.sharedMaterial=rift;renderer.sortingOrder=15;return box;
    }
    static void Gate(Transform root,float x,QuestGate.Requirement requirement){var shape=Seal(root,requirement+" lock",x,-.5f,48,out var glow);var gate=shape.gameObject.AddComponent<QuestGate>();gate.requirement=requirement;gate.barrier=shape;gate.glow=glow;}
    static void PhaseSeal(Transform root,float x,int world){var shape=Seal(root,"Dimension seal",x,-.5f,42,out var glow);shape.gameObject.AddComponent<DimensionMember>().dimension=world;}
    static void Enemy(Transform root,float x,float y,int world)
    {var anchor=Child("Dimension defender",root,new Vector3(x,y));var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Enc+(world==0?"PouncingSlime":"SporeKeeper")+".prefab");var actor=Object.Instantiate(prefab,anchor.transform);actor.transform.localPosition=world==0?Vector3.up*.48f:Vector3.zero;var phase=anchor.AddComponent<DimensionActor>();phase.dimension=world;phase.content=actor;}
    static void BuildKeySanctum()
    {
        var root=Root("13_KeySanctum",64);var map=Map(root.transform);Block(map,0,-4,64,4);Checkpoint(root.transform,"Key sanctuary");
        Block(map,7,0,3,2);Block(map,12,0,3,4);Block(map,17,0,4,6);PhaseSeal(root.transform,10,0);
        Item(root.transform,"Echo key",new Vector3(18.5f,6.9f),QuestInteractable.Kind.EchoKey,1,key);Enemy(root.transform,22,0,1);
        Block(map,25,17,20,1);Block(map,24,17,1,9);Block(map,45,17,1,9);Block(map,24,25,22,1);
        var entry=Item(root.transform,"Blue key door",new Vector3(31,.1f),QuestInteractable.Kind.BlueDoor,0,door);entry.destination=Child("Sanctum arrival",root.transform,new Vector3(28,18.7f)).transform;
        Block(map,38,18,4,1);
        Item(root.transform,"Spirit altar",new Vector3(40,19.8f),QuestInteractable.Kind.Spirit,-1,Frames("SpiritBolt")[1]);
        var back=Item(root.transform,"Return portal",new Vector3(27,18),QuestInteractable.Kind.ReturnPortal,-1,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Polish/CheckpointShrine.png"));back.destination=Child("Trail return",root.transform,new Vector3(48,.7f)).transform;
        Gate(root.transform,55,QuestGate.Requirement.Spirit);Hint(root.transform,4,"Climb the upper steps in ECHO to claim the key. The blue doorway below opens with E in FROST.");Hint(root.transform,50,"C casts your spirit. It can strike enemies and awaken seals from a distance.");SaveBoth(root);
    }
    static void BuildRelays()
    {
        var root=Root("14_RelayTrial",84);var map=Map(root.transform);Block(map,0,-4,84,4);Checkpoint(root.transform,"Twin seals");
        Block(map,7,0,3,2);Block(map,13,0,3,4);Block(map,18,0,4,6);PhaseSeal(root.transform,11,0);
        Item(root.transform,"Violet seal",new Vector3(29,6.7f),QuestInteractable.Kind.Relay,1,rune);Enemy(root.transform,20,6,1);
        Block(map,34,0,3,2);PhaseSeal(root.transform,40,1);Block(map,49,0,4,3);Block(map,55,0,4,5);
        Item(root.transform,"Blue seal",new Vector3(65,5.7f),QuestInteractable.Kind.Relay,0,rune);Enemy(root.transform,50,3,0);Enemy(root.transform,70,0,1);
        Gate(root.transform,78,QuestGate.Requirement.Relays);Hint(root.transform,4,"Awaken BOTH seals with C. Your bolt carries the color of your current world. Climb to line up the shot.");Hint(root.transform,44,"The second seal answers in FROST. Both seals stay awakened after death.");SaveBoth(root);
    }
    static void BuildTrial()
    {
        var root=Root("15_ProvingGrounds",84);var map=Map(root.transform);Block(map,0,-4,84,4);Checkpoint(root.transform,"Proving grounds");
        Block(map,24,0,3,2);Block(map,50,0,3,2);Block(map,36,4,4,1);
        var arena=Child("Three wave trial",root.transform,new Vector3(6,0)).AddComponent<TrialArena>();arena.width=70;
        arena.slimePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Enc+"PouncingSlime.prefab");arena.mushroomPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Enc+"SporeKeeper.prefab");
        arena.spawnPoints=new[]{new Vector3(10,0),new Vector3(25,0),new Vector3(39,0),new Vector3(54,0),new Vector3(61,0)}.Select((p,i)=>Child("Wave spawn "+i,arena.transform,p).transform).ToArray();
        arena.entrance=Seal(root.transform,"Trial entrance",6,-.5f,48,out var entryGlow);var visibility=arena.entrance.gameObject.AddComponent<GateVisibility>();visibility.shape=arena.entrance;visibility.visual=entryGlow;
        Gate(root.transform,77,QuestGate.Requirement.Trial);Hint(root.transform,4,"Three waves guard the path. Defeat inhabitants of BOTH dimensions. C reaches distant foes; landing on mushrooms deals one damage.");SaveBoth(root);
    }
    static void BuildBoss()
    {
        var root=Root("16_WardenArena",52);var map=Map(root.transform);Block(map,0,-4,52,4);Checkpoint(root.transform,"Warden's threshold");Block(map,17,4,3,1);Block(map,35,4,3,1);
        var go=Child("Rift Warden",root.transform,new Vector3(33,.05f));go.layer=10;go.tag="Enemy";var body=go.AddComponent<Rigidbody2D>();body.gravityScale=3;body.mass=8;body.constraints=RigidbodyConstraints2D.FreezeRotation;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
        var shape=go.AddComponent<BoxCollider2D>();shape.size=new Vector2(1.65f,2.25f);shape.offset=new Vector2(0,1.125f);
        var hp=go.AddComponent<EnemyHealth>();hp.maxHealth=64;hp.rewardScore=1800;hp.rewardXP=100;hp.deathDuration=1.6f;
        var boss=go.AddComponent<RiftWarden>();boss.idle=Frames("KeeperIdle");boss.run=Frames("KeeperRun");boss.attack=Frames("KeeperAttack");boss.stunned=Frames("KeeperStunned");boss.death=Frames("KeeperDeath");boss.visual=Visual("Warden armor",go.transform,Vector3.zero,boss.idle[0]);
        boss.warning=Visual("Attack warning",go.transform,new Vector3(0,3.3f),warning,1.1f);boss.warning.enabled=false;
        boss.projectilePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Enc+"SporeProjectile.prefab").GetComponent<EnemyProjectile>();
        boss.arenaLeft=Child("Arena left",root.transform,new Vector3(8,0)).transform;boss.arenaRight=Child("Arena right",root.transform,new Vector3(45,0)).transform;
        boss.entrance=Seal(root.transform,"Arena entrance",8,-.5f,48,out var entryGlow);var visibility=boss.entrance.gameObject.AddComponent<GateVisibility>();visibility.shape=boss.entrance;visibility.visual=entryGlow;
        Gate(root.transform,46,QuestGate.Requirement.Boss);Hint(root.transform,4,"The Warden pursues, slams, and fires a fan of bolts. During recovery its core glows BLUE or VIOLET: attack in that world. Below half health it enrages.");SaveBoth(root);
    }
}
