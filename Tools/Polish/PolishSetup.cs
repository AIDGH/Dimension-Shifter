using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.Tilemaps;using Object=UnityEngine.Object;
public static class PolishSetup
{
 const string Enc="Assets/Prefabs/Encounters/";const string Art="Assets/Art/Polish/";
 static Material sprites,rift,background;static Sprite shrine;static readonly List<string> report=new List<string>();
 static GameObject Child(string name,Transform parent,Vector3 p){var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=p;return g;}
 static void Asset(Object value,string path){var old=AssetDatabase.LoadMainAssetAtPath(path);if(old!=null){EditorUtility.CopySerialized(value,old);EditorUtility.SetDirty(old);Object.DestroyImmediate(value);}else AssetDatabase.CreateAsset(value,path);}
 static Tile Tile(int id)=>AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.FindAssets("t:Tile",new[]{"Assets/Tiles"}).Select(AssetDatabase.GUIDToAssetPath).First(p=>p.EndsWith("/Snowy Tundra 16x16 Tileset_"+id+".asset")));
 static GameObject Instance(string name,Transform parent,Vector3 p){var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Enc+name+".prefab"));g.transform.SetParent(parent,false);g.transform.localPosition=p;return g;}
 public static void Run(){try{
  sprites=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Painted/PaintedSprites.mat");
  Asset(new Material(Shader.Find("DimensionShifter/LivingRift")),Art+"Rift.mat");rift=AssetDatabase.LoadAssetAtPath<Material>(Art+"Rift.mat");
  Asset(new Material(Shader.Find("DimensionShifter/SoftBackground")),Art+"SoftBackground.mat");background=AssetDatabase.LoadAssetAtPath<Material>(Art+"SoftBackground.mat");
  var importer=(TextureImporter)AssetImporter.GetAtPath(Art+"CheckpointShrine.png");importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=570;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
  var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,.025f);importer.SetTextureSettings(settings);importer.SaveAndReimport();shrine=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"CheckpointShrine.png");
  // Shared enemy prefabs retain their health and attacks; only collider/feedback tuning changes.
  var slime=PrefabUtility.LoadPrefabContents(Enc+"PouncingSlime.prefab");FixSlime(slime);PrefabUtility.SaveAsPrefabAsset(slime,Enc+"PouncingSlime.prefab");PrefabUtility.UnloadPrefabContents(slime);
  var mushroom=PrefabUtility.LoadPrefabContents(Enc+"SporeKeeper.prefab");mushroom.GetComponent<EnemyHealth>().rewardScore=150;mushroom.GetComponent<EnemyHealth>().rewardXP=35;mushroom.GetComponent<EnemyFlipbook>().target.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Combat/EnemyFlash.mat");PrefabUtility.SaveAsPrefabAsset(mushroom,Enc+"SporeKeeper.prefab");PrefabUtility.UnloadPrefabContents(mushroom);
  foreach(var path in Directory.GetFiles(Enc,"*.prefab").Where(p=>Path.GetFileName(p).StartsWith("0")||Path.GetFileName(p).StartsWith("TrailEnd")))
  {
   var root=PrefabUtility.LoadPrefabContents(path);
   foreach(var marker in root.GetComponentsInChildren<TrailMarker>())if(marker.kind!=TrailMarker.MarkerKind.Hint)Shrine(marker);
   foreach(var member in root.GetComponentsInChildren<DimensionMember>())if(member.name.Contains("seal"))Rift(member);
   foreach(var enemy in root.GetComponentsInChildren<EnemyHealth>().ToArray())BindEnemy(enemy,enemy.GetComponent<EnemyAttackAI>()?.kind==EnemyAttackAI.AttackKind.Spore?1:0);
   if(root.name.StartsWith("07_") && root.transform.Find("Bonus echo keeper")==null) {var extra=Instance("SporeKeeper",root.transform,new Vector3(17,.05f));BindEnemy(extra.GetComponent<EnemyHealth>(),1);extra.transform.parent.name="Bonus echo keeper";Pickup(root.transform,new Vector3(14.8f,3.1f),-1,50,20);}
   if(root.name.StartsWith("08_") && root.transform.Find("Bonus frost slime")==null) {var extra=Instance("PouncingSlime",root.transform,new Vector3(11,.55f));BindEnemy(extra.GetComponent<EnemyHealth>(),0);extra.transform.parent.name="Bonus frost slime";Pickup(root.transform,new Vector3(7.8f,2.8f),0,60,25);Pickup(root.transform,new Vector3(17,2.8f),1,75,30);}
   if(root.name.StartsWith("09_")) {Pickup(root.transform,new Vector3(12.2f,2.4f),1,75,30);Pickup(root.transform,new Vector3(20,2.4f),0,75,30);}
   PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
  }
  BuildVertical(false);BuildVertical(true);
  foreach(var path in new[]{"Assets/Scenes/SampleScene.unity","Assets/Scenes/PaintedDimension.unity"})
  {
   var scene=EditorSceneManager.OpenScene(path);bool painted=path.Contains("Painted");var player=Object.FindAnyObjectByType<PlayerHealth>();
   Add<PlayerRewards>(player.gameObject);var hud=Add<GameHUD>(player.gameObject);hud.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Examples & Extras/Fonts/Roboto-Bold.ttf");Add<WorldMusic>(player.gameObject);
   player.GetComponent<DimensionController>().cooldown=2f;
   Add<AtmosphereBackdrop>(Camera.main.gameObject);
   foreach(var layer in Object.FindObjectsByType<Parallax>())layer.GetComponent<SpriteRenderer>().sharedMaterial=background;
   foreach(var enemy in Object.FindObjectsByType<EnemyHealth>()){FixSlime(enemy.gameObject);BindEnemy(enemy,0);}
   var gen=Object.FindAnyObjectByType<LevelGenerator>();string suffix=painted?"_Painted":"";
   gen.encounterChunks=new[]{"07_LeapGarden","10_CrystalAscent","08_SporePass","09_RiftCrossing","11_HollowDescent"}.Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>(Enc+n+suffix+".prefab")).ToArray();gen.cameraFollow.maxX=260;
   EditorSceneManager.SaveScene(scene);
  }
  foreach(var path in Directory.GetFiles("Assets/Resources/Music","*.wav")){var audio=(AudioImporter)AssetImporter.GetAtPath(path);var sample=audio.defaultSampleSettings;sample.loadType=AudioClipLoadType.Streaming;sample.compressionFormat=AudioCompressionFormat.Vorbis;sample.quality=.7f;audio.defaultSampleSettings=sample;audio.SaveAndReimport();}
  RouteRefinementSetup.Apply();
  AssetDatabase.SaveAssets();File.WriteAllLines("polish-setup-results.txt",report);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static T Add<T>(GameObject g)where T:Component{var c=g.GetComponent<T>();return c!=null?c:g.AddComponent<T>();}
 static void BindEnemy(EnemyHealth enemy,int dimension)
 {
  if(enemy.GetComponentInParent<DimensionActor>()!=null)return;
  var t=enemy.transform;var parent=t.parent;var p=t.localPosition;var anchor=Child(dimension==0?"Frost inhabitants":"Echo inhabitants",parent,p);t.SetParent(anchor.transform,false);t.localPosition=Vector3.zero;
  var phase=anchor.AddComponent<DimensionActor>();phase.dimension=dimension;phase.content=enemy.gameObject;
 }
 static void FixSlime(GameObject enemy)
 {
  var sr=enemy.GetComponent<SpriteRenderer>();if(sr==null||sr.sprite==null)return;
  var sprite=sr.sprite;var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));bool readable=importer.isReadable;importer.isReadable=true;importer.SaveAndReimport();
  var rect=sprite.rect;var pixels=sprite.texture.GetPixels32();int width=sprite.texture.width;int bottom=(int)rect.yMax;
  for(int y=(int)rect.y;y<rect.yMax;y++)for(int x=(int)rect.x;x<rect.xMax;x++)if(pixels[y*width+x].a>80){bottom=Mathf.Min(bottom,y);break;}
  float foot=(bottom-rect.y-sprite.pivot.y)/sprite.pixelsPerUnit;
  // Remove the oversized secondary collision shape, retaining a grounded body box.
  foreach(var polygon in enemy.GetComponents<PolygonCollider2D>())Object.DestroyImmediate(polygon);
  var box=enemy.GetComponent<BoxCollider2D>();if(box==null)box=enemy.AddComponent<BoxCollider2D>();box.enabled=true;box.isTrigger=false;if(box!=null){float top=box.offset.y+box.size.y*.5f;box.size=new Vector2(box.size.x,top-foot);box.offset=new Vector2(box.offset.x,(top+foot)*.5f);}
  importer.isReadable=readable;importer.SaveAndReimport();report.Add("Slime opaque foot="+foot+"; collider bottom aligned, "+enemy.name);
 }
 static void Shrine(TrailMarker marker)
 {
  var previous=marker.transform.Find("Blue sanctuary ring");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
  if(marker.beacon!=null)Object.DestroyImmediate(marker.beacon.gameObject);
  var visual=Child("Blue sanctuary ring",marker.transform,new Vector3(0,-.9f,0));var sr=visual.AddComponent<SpriteRenderer>();sr.sprite=shrine;sr.sharedMaterial=sprites;sr.sortingOrder=15;
  if(marker.kind==TrailMarker.MarkerKind.Exit)sr.color=new Color(.8f,.7f,1);
  Add<ShrineVisual>(marker.gameObject).shrine=sr;marker.beacon=null;
 }
 static void Rift(DimensionMember member)
 {
  foreach(Transform child in member.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
  var go=Child("Living energy seam",member.transform,Vector3.zero);var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-.45f,0),new Vector3(.45f,0),new Vector3(.45f,7),new Vector3(-.45f,7)};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};
  string path=Art+"RiftQuad.asset";Asset(mesh,path);go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=rift;renderer.sortingOrder=15;
 }
 static void Pickup(Transform parent,Vector3 pos,int dimension,int points,int xp)
 {
  if(parent.GetComponentsInChildren<ExperiencePickup>(true).Any(p=>Vector3.Distance(p.transform.localPosition,pos)<.01f))return;
  var g=Child("Echo relic",parent,pos);var c=g.AddComponent<CircleCollider2D>();c.isTrigger=true;c.radius=.35f;var item=g.AddComponent<ExperiencePickup>();item.dimension=dimension;item.points=points;item.experience=xp;
  var visual=Child("Floating gem",g.transform,Vector3.zero);item.visual=visual.transform;
  var line=visual.AddComponent<LineRenderer>();line.sharedMaterial=sprites;line.useWorldSpace=false;line.loop=true;line.positionCount=4;line.SetPositions(new[]{new Vector3(0,.28f),new Vector3(.18f,0),new Vector3(0,-.28f),new Vector3(-.18f,0)});line.startWidth=line.endWidth=.055f;line.startColor=line.endColor=dimension==0?new Color(.4f,.95f,1):dimension==1?new Color(.9f,.6f,1):new Color(1,.8f,.4f);line.sortingOrder=20;
 }
 static void BuildVertical(bool down)
 {
  string name=down?"11_HollowDescent":"10_CrystalAscent";var root=new GameObject(name);root.AddComponent<Grid>();Child("EndPosition",root.transform,new Vector3(18,down?-6:6));
  var terrain=Child("Tower terrain",root.transform,Vector3.zero);terrain.layer=8;var map=terrain.AddComponent<Tilemap>();terrain.AddComponent<TilemapRenderer>().sharedMaterial=sprites;
  for(int x=0;x<18;x++){int step=Mathf.Min(3,x/4);int top=(down?-1:1)*step*2;
   for(int y=top-4;y<top;y++){var cell=new Vector3Int(x,y,0);map.SetTile(cell,Tile(y==top-1?49:84));map.SetColliderType(cell,UnityEngine.Tilemaps.Tile.ColliderType.None);}
   var box=terrain.AddComponent<BoxCollider2D>();box.size=new Vector2(1,4);box.offset=new Vector2(x+.5f,top-2);
  }
  // Optional high shelf rewards exploration without blocking the main staircase.
  int shelfY=down?2:8;var shelf=Child("Relic alcove",root.transform,Vector3.zero);shelf.layer=8;var shelves=shelf.AddComponent<Tilemap>();shelf.AddComponent<TilemapRenderer>().sharedMaterial=sprites;
  for(int x=7;x<10;x++){var c=new Vector3Int(x,shelfY,0);shelves.SetTile(c,Tile(49));shelves.SetColliderType(c,UnityEngine.Tilemaps.Tile.ColliderType.None);}
  var shape=shelf.AddComponent<BoxCollider2D>();shape.size=new Vector2(3,1);shape.offset=new Vector2(8.5f,shelfY+.5f);Pickup(root.transform,new Vector3(8.5f,shelfY+1.75f),down?1:0,150,50);
  for(int i=0;i<3;i++)Pickup(root.transform,new Vector3(5+i*4,(down?-1:1)*(i+1)*2+.8f),-1,25,10);
  var markerGo=Child("Checkpoint",root.transform,new Vector3(2,.9f));var trigger=markerGo.AddComponent<BoxCollider2D>();trigger.isTrigger=true;trigger.size=new Vector2(1.5f,2);var marker=markerGo.AddComponent<TrailMarker>();marker.kind=TrailMarker.MarkerKind.Checkpoint;marker.message=down?"Hollow descent":"Crystal ascent";marker.spawnPoint=Child("Safe respawn",markerGo.transform,new Vector3(-.8f,-.15f)).transform;Shrine(marker);
  PrefabUtility.SaveAsPrefabAsset(root,Enc+name+".prefab");PaintedVariantBuilder.PaintExpansion(root,name);PrefabUtility.SaveAsPrefabAsset(root,Enc+name+"_Painted.prefab");Object.DestroyImmediate(root);report.Add("Built "+name+" with 6 units of vertical travel and an optional relic shelf.");
 }
}
