using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using Object=UnityEngine.Object;
public static class DeadlineSetup
{
 const string Enc="Assets/Prefabs/Encounters/";
 static Material painted;
 public static void Main(){Run(false);}public static void Hollow(){Run(true);}
 static void Run(bool hollow){try{
  AssetDatabase.Refresh();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
  painted=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Painted/PaintedSprites.mat");
  FixHorn();ImportStory();
  if(!hollow){Trial();MoreEnemies();Canopy();FinalTree();}
  var scene=EditorSceneManager.OpenScene(hollow?"Assets/Scenes/HollowGreenpath.unity":"Assets/Scenes/PaintedDimension.unity");
  var player=Object.FindAnyObjectByType<PlayerMovement>();var skin=player.GetComponent<CharacterAppearance>()??player.gameObject.AddComponent<CharacterAppearance>();
  skin.redController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Red/Player.controller");
  skin.knightController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Hollow/Generated/Knight.controller.overrideController");
  skin.redIdle=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Red/Player_Idle.anim").GetObjectReferenceValue();
  skin.knightIdle=AssetDatabase.LoadAllAssetsAtPath("Assets/Hollow/player/Idle.png").OfType<Sprite>().First();skin.useKnight=true;
  if(skin.redController==null||skin.knightController==null||skin.redIdle==null)throw new Exception("Missing skin references");
  player.GetComponent<Animator>().runtimeAnimatorController=skin.knightController;player.GetComponent<SpriteRenderer>().sprite=skin.knightIdle;player.GetComponent<SpriteRenderer>().flipX=true;player.spriteFacesRight=false;player.alignToSlope=false;
  player.useAcceleration=false;player.acceleration=85;player.groundDeceleration=85;
  var attack=player.GetComponent<PlayerAttack>();attack.normalAttackDuration=.38f;attack.normalAttackRate=2.6f;attack.activeFrom=.04f;attack.activeUntil=.26f;
  foreach(var path in new[]{"Assets/Animations/Red/Player_Attack.anim","Assets/Hollow/Generated/Player_Attack.anim"}){
    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(clip==null)continue;
    foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip)){var keys=AnimationUtility.GetObjectReferenceCurve(clip,binding);float last=keys.Max(k=>k.time);if(last>.38f){float factor=.35f/last;for(int i=0;i<keys.Length;i++)keys[i].time*=factor;AnimationUtility.SetObjectReferenceCurve(clip,binding,keys);}}
  }
  player.GetComponent<DemoSession>().RequiresTreeKey=!hollow;
  if(!hollow){var gen=Object.FindAnyObjectByType<LevelGenerator>();var list=gen.encounterChunks.ToList();var canopy=AssetDatabase.LoadAssetAtPath<GameObject>(Enc+"17_RiftCanopy_Painted.prefab");if(!list.Contains(canopy)){list.Insert(4,canopy);gen.encounterChunks=list.ToArray();gen.cameraFollow.maxX+=32;}gen.cameraFollow.airborneVerticalDeadzone=2;}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  PlayerSettings.bundleVersion=hollow?"0.3-hollow-preview":"1.1.0-demo";AssetDatabase.SaveAssets();
  File.WriteAllText("deadline-setup.txt","SUCCESS: skins, input feel, sprite scale, story; "+(hollow?"Hollow scene":"compact mixed waves, canopy route, keyed tree exit")+"\n");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);File.WriteAllText("deadline-setup.txt",e.ToString());EditorApplication.Exit(1);}}
 static void FixHorn(){
  var idle=(TextureImporter)AssetImporter.GetAtPath("Assets/Hollow/enemies/husk_hornhead/Idle.png");float ppu=idle.spritePixelsPerUnit;
  foreach(var name in new[]{"Walk","Charge"}){var imp=(TextureImporter)AssetImporter.GetAtPath("Assets/Hollow/enemies/husk_hornhead/"+name+".png");float old=imp.spritePixelsPerUnit;if(Mathf.Approximately(old,ppu))continue;
   var fac=new SpriteDataProviderFactories();fac.Init();var provider=fac.GetSpriteEditorDataProviderFromObject(imp);provider.InitSpriteEditorDataProvider();var rects=provider.GetSpriteRects();foreach(var r in rects){var pivot=r.pivot;pivot.y+=(ppu-old)*.495f/r.rect.height;r.pivot=pivot;}provider.SetSpriteRects(rects);provider.Apply();imp.spritePixelsPerUnit=ppu;imp.SaveAndReimport();
  }
 }
 static void ImportStory(){foreach(var path in Directory.GetFiles("Assets/Resources/Story","*.png")){var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.maxTextureSize=2048;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;imp.SaveAndReimport();}}
 static void Trial(){foreach(var suffix in new[]{"","_Painted"}){string path=Enc+"15_ProvingGrounds"+suffix+".prefab";var r=PrefabUtility.LoadPrefabContents(path);var trial=r.GetComponentInChildren<TrialArena>();trial.width=30;trial.mossflyPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Enc+"FrostMossfly.prefab");trial.hornheadPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Enc+"EchoHornhead.prefab");var gate=r.GetComponentsInChildren<QuestGate>().First(x=>x.requirement==QuestGate.Requirement.Trial);gate.transform.localPosition=new Vector3(36,-.5f,0);PrefabUtility.SaveAsPrefabAsset(r,path);PrefabUtility.UnloadPrefabContents(r);}}
 static void MoreEnemies(){foreach(var pair in new[]{("07_LeapGarden_Painted",true),("09_RiftCrossing_Painted",true),("14_RelayTrial_Painted",false)}){var path=Enc+pair.Item1+".prefab";var r=PrefabUtility.LoadPrefabContents(path);Physics2D.SyncTransforms();var phys=r.scene.GetPhysicsScene2D();float width=r.transform.Find("EndPosition").localPosition.x;
  for(float x=width*.65f;x<width-6;x+=2){var hit=phys.Raycast(new Vector2(x,35),Vector2.down,55,1<<8);if(hit.collider==null||hit.normal.y<.95f)continue;var p=new Vector3(x,hit.point.y+(pair.Item2?2.8f:.52f));if(r.GetComponentsInChildren<EnemyHealth>(true).Any(e=>Vector2.Distance(e.transform.position,p)<5))continue;Spawn(r.transform,p,pair.Item2);break;}
  PrefabUtility.SaveAsPrefabAsset(r,path);PrefabUtility.UnloadPrefabContents(r);
 }}
 static void Spawn(Transform parent,Vector3 p,bool fly){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Enc+(fly?"FrostMossfly":"EchoHornhead")+".prefab");var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);g.transform.localPosition=p;}
 static void Canopy(){
  string path=Enc+"17_RiftCanopy_Painted.prefab";if(File.Exists(path))return;
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Enc+"15_ProvingGrounds_Painted.prefab");var srcMap=source.GetComponentsInChildren<Tilemap>().First(m=>m.GetTile(new Vector3Int(0,-1,0))!=null);
  var top=srcMap.GetTile(new Vector3Int(0,-1,0));var fill=srcMap.GetTile(new Vector3Int(0,-2,0));
  var root=new GameObject("17_RiftCanopy_Painted");root.AddComponent<Grid>();Child(root.transform,"EndPosition",new Vector3(32,0));
  var terrain=Child(root.transform,"Canopy stone platforms",Vector3.zero);terrain.layer=8;var map=terrain.AddComponent<Tilemap>();terrain.AddComponent<TilemapRenderer>().sharedMaterial=srcMap.GetComponent<TilemapRenderer>().sharedMaterial;
  Action<int,int,int,int> block=(x,y,w,h)=>{for(int i=x;i<x+w;i++)for(int j=y;j<y+h;j++){map.SetTile(new Vector3Int(i,j,0),j==y+h-1?top:fill);map.SetColliderType(new Vector3Int(i,j,0),Tile.ColliderType.None);}var c=terrain.AddComponent<BoxCollider2D>();c.size=new Vector2(w,h);c.offset=new Vector2(x+w*.5f,y+h*.5f);};
  block(0,-4,32,4);block(4,0,3,2);block(8,0,3,4);block(12,0,6,8);block(20,0,3,5);block(25,0,3,2);
  var marker=AssetDatabase.LoadAssetAtPath<GameObject>(Enc+"07_LeapGarden_Painted.prefab").GetComponentsInChildren<TrailMarker>(true).First(m=>m.kind==TrailMarker.MarkerKind.Checkpoint);
  var cp=Object.Instantiate(marker.gameObject,root.transform);cp.transform.localPosition=new Vector3(1.8f,.9f);cp.GetComponent<TrailMarker>().message="Rift canopy";
  var hint=Child(root.transform,"Canopy trail note",new Vector3(3,1));var col=hint.AddComponent<BoxCollider2D>();col.isTrigger=true;col.size=new Vector2(2,3);hint.AddComponent<TrailMarker>().message="The trail climbs through the canopy. SPACE twice to reach the crown, then descend to the far side.";
  Spawn(root.transform,new Vector3(15,10.5f),true);Spawn(root.transform,new Vector3(15,8.52f),false);
  PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);
 }
 static GameObject Child(Transform p,string name,Vector3 pos){var g=new GameObject(name);g.transform.SetParent(p,false);g.transform.localPosition=pos;return g;}
 static void FinalTree(){string path=Enc+"TrailEnd_Painted.prefab";var r=PrefabUtility.LoadPrefabContents(path);if(r.GetComponentInChildren<FinalTreeGate>()!=null){PrefabUtility.UnloadPrefabContents(r);return;}
  var tree=r.GetComponentsInChildren<SpriteRenderer>(true).First(x=>x.name=="Sanctuary canopy");Physics2D.SyncTransforms();float x=tree.transform.position.x;var hit=r.scene.GetPhysicsScene2D().Raycast(new Vector2(x-1,tree.transform.position.y+2),Vector2.down,20,1<<8);float y=hit.collider!=null?hit.point.y:tree.transform.position.y;
  var g=Child(r.transform,"Ancient tree · Warden sigil",new Vector3(x-.7f,y+.1f));var gate=g.AddComponent<FinalTreeGate>();
  var door=Child(g.transform,"Sealed tree door",Vector3.zero);var sr=door.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Demo/SanctumDoorV2.png");sr.sharedMaterial=painted;sr.sortingOrder=20;gate.door=sr;
  var wall=Child(g.transform,"Root seal · no bypass",Vector3.zero);wall.layer=9;var box=wall.AddComponent<BoxCollider2D>();box.size=new Vector2(.7f,70);box.offset=new Vector2(0,20);gate.barrier=box;
  var exit=r.GetComponentsInChildren<TrailMarker>(true).First(m=>m.kind==TrailMarker.MarkerKind.Exit);gate.finalRift=exit.gameObject;
  foreach(var visual in exit.GetComponentsInChildren<SpriteRenderer>(true))visual.enabled=false;foreach(var old in exit.GetComponentsInChildren<ShrineVisual>(true))old.enabled=false;
  var portal=Child(exit.transform,"Golden home rift",new Vector3(0,1));portal.AddComponent<FinalRiftVisual>().material=painted;
  PrefabUtility.SaveAsPrefabAsset(r,path);PrefabUtility.UnloadPrefabContents(r);
 }
 static Sprite GetObjectReferenceValue(this AnimationClip clip){foreach(var b in AnimationUtility.GetObjectReferenceCurveBindings(clip)){var keys=AnimationUtility.GetObjectReferenceCurve(clip,b);if(keys.Length>0&&keys[0].value is Sprite s)return s;}return null;}
}
