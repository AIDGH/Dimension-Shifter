using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
public static class DeadlineFinish
{
 public static void Main(){Run(false);}public static void Hollow(){Run(true);}
 static void Run(bool hollow){try{
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);AssetDatabase.Refresh();
  string[] paths={"Assets/Prefabs/Encounters/16_WardenArena.prefab","Assets/Prefabs/Encounters/16_WardenArena_Painted.prefab"};
  foreach(var path in paths){var root=PrefabUtility.LoadPrefabContents(path);Align(root.GetComponentInChildren<RiftWarden>());PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);}
  if(!hollow){
   string path="Assets/Prefabs/Encounters/17_RiftCanopy_Painted.prefab";var root=PrefabUtility.LoadPrefabContents(path);
   if(root.transform.Find("Canopy stone platforms/Painted surface")==null)PaintedVariantBuilder.PaintExpansion(root,"17_RiftCanopy");
   PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
   path="Assets/Prefabs/Encounters/TrailEnd_Painted.prefab";root=PrefabUtility.LoadPrefabContents(path);var gate=root.GetComponentInChildren<FinalTreeGate>();var exit=gate.finalRift.transform;
   var target=new GameObject("Beyond the ancient tree");target.transform.SetParent(root.transform);Physics2D.SyncTransforms();float x=exit.position.x-3;var hit=root.scene.GetPhysicsScene2D().Raycast(new Vector2(x,30),Vector2.down,60,1<<8);target.transform.position=new Vector3(x,(hit.collider!=null?hit.point.y:exit.position.y)+.65f);gate.farSide=target.transform;
   PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
  }else{var scene=EditorSceneManager.OpenScene("Assets/Scenes/HollowGreenpath.unity");foreach(var boss in Object.FindObjectsByType<RiftWarden>())Align(boss);EditorSceneManager.SaveScene(scene);}
  AssetDatabase.SaveAssets();File.WriteAllText("deadline-finish.txt","Opaque feet aligned on all boss frames; painted canopy and safe tree passage configured.\n");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void Align(RiftWarden boss){var sprites=boss.idle.Concat(boss.run).Concat(boss.attack).Concat(boss.stunned).Concat(boss.death).Distinct().ToArray();boss.groundedSprites=sprites;boss.spriteBottoms=new float[sprites.Length];
  foreach(string path in sprites.Select(s=>AssetDatabase.GetAssetPath(s)).Distinct()){var imp=(TextureImporter)AssetImporter.GetAtPath(path);bool readable=imp.isReadable;imp.isReadable=true;imp.SaveAndReimport();var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var px=tex.GetPixels32();
   for(int n=0;n<sprites.Length;n++){var sp=sprites[n];if(AssetDatabase.GetAssetPath(sp)!=path)continue;var r=sp.rect;int low=(int)r.height;for(int y=0;y<(int)r.height;y++)for(int x=0;x<(int)r.width;x++)if(px[((int)r.y+y)*tex.width+(int)r.x+x].a>80)low=Math.Min(low,y);boss.spriteBottoms[n]=(low-sp.pivot.y)/sp.pixelsPerUnit;}
   imp.isReadable=readable;imp.SaveAndReimport();
  }
 }
}
