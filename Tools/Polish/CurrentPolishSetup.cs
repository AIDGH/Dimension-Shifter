using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
public static class CurrentPolishSetup
{
 public static void Run()
 {
  try {
   AssetDatabase.Refresh();
   var shader=Shader.Find("DimensionShifter/WardenLance");if(shader==null)throw new Exception("Missing lance shader");
   string materialPath="Assets/Art/Polish/WardenLance.mat";
   var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
   if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,materialPath);}else mat.shader=shader;
   mat.SetColor("_Tint",new Color(.35f,.85f,1));EditorUtility.SetDirty(mat);
   foreach(var path in new[]{"Assets/Prefabs/Encounters/16_WardenArena.prefab","Assets/Prefabs/Encounters/16_WardenArena_Painted.prefab"}){
    var root=PrefabUtility.LoadPrefabContents(path);foreach(var boss in root.GetComponentsInChildren<RiftWarden>(true))boss.beamMaterial=mat;
    PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
   }
   foreach(var path in Directory.GetFiles("Assets/Hollow/map_images","*.png",SearchOption.AllDirectories)){
    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
    if(importer.filterMode==FilterMode.Bilinear)continue;
    importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
   }
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/HollowGreenpath.unity");
   var player=Object.FindAnyObjectByType<PlayerMovement>();player.spriteFacesRight=false;player.GetComponent<SpriteRenderer>().flipX=true;
   Camera.main.orthographicSize=6.5f;
   foreach(var boss in Object.FindObjectsByType<RiftWarden>(FindObjectsInactive.Include,FindObjectsSortMode.None))boss.beamMaterial=mat;
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   PlayerSettings.bundleVersion="0.2-hollow";
   string report="Knight artwork faces left; logical direction preserved. Camera 6.5; bilinear map textures. Dedicated lance shader.\n";
   foreach(var path in new[]{"Assets/Prefabs/PaintedChunks/End_Chunk_01_Painted.prefab","Assets/Prefabs/Encounters/TrailEnd_Painted.prefab"}){
    var root=PrefabUtility.LoadPrefabContents(path);report+="\n"+path+"\n";
    foreach(var map in root.GetComponentsInChildren<Tilemap>(true)){
     var mr=map.GetComponent<TilemapRenderer>();if(mr==null||!mr.enabled)continue;
     var names=new System.Collections.Generic.List<string>();foreach(var c in map.cellBounds.allPositionsWithin)if(map.GetSprite(c)!=null && map.GetColor(c).a>.01f)names.Add(map.GetSprite(c).name+"@"+c);
     report+=map.name+": "+string.Join(",",names)+"\n";
    }PrefabUtility.UnloadPrefabContents(root);
   }
   AssetDatabase.SaveAssets();File.WriteAllText("current-polish-setup.txt",report);EditorApplication.Exit(0);
  }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
 }
}
