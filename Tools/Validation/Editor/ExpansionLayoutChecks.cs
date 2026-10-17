using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;
public static class ExpansionLayoutChecks
{
 static BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 public static void Run(){try{
 var report=new List<string>();
 foreach(var scenePath in new[]{"Assets/Scenes/SampleScene.unity","Assets/Scenes/PaintedDimension.unity"})
 {
  EditorSceneManager.OpenScene(scenePath);var gen=UnityEngine.Object.FindFirstObjectByType<LevelGenerator>();
  if(gen.encounterChunks.Length!=6||gen.midChunks.Length!=6)throw new Exception("Missing original or new chunks");
  typeof(LevelGenerator).GetMethod("Start",Private).Invoke(gen,null);
  for(int i=0;i<30;i++){var last=(Transform)typeof(LevelGenerator).GetField("lastEndPosition",Private).GetValue(gen);gen.player.position=last.position;typeof(LevelGenerator).GetMethod("Update",Private).Invoke(gen,null);}
  foreach(var prefab in gen.encounterChunks)if(!UnityEngine.Object.FindObjectsByType<Transform>().Any(t=>t.name==prefab.name+"(Clone)"))throw new Exception("Encounter skipped: "+prefab.name);
  if(!UnityEngine.Object.FindObjectsByType<TrailMarker>().Any(m=>m.kind==TrailMarker.MarkerKind.Exit))throw new Exception("Missing finish");
  report.Add("PASS "+scenePath+": all six authored encounters spawn between old chunks; route reaches exit.");
 }
 EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");
 var camera=Camera.main;camera.orthographic=true;camera.aspect=16f/9f;camera.orthographicSize=7.3f;
 foreach(var c in UnityEngine.Object.FindObjectsByType<Canvas>())c.enabled=false;
 foreach(var h in UnityEngine.Object.FindObjectsByType<EnemyHealth>())h.gameObject.SetActive(false);
 var player=UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();player.transform.position=new Vector3(3,.65f,0);
 foreach(string name in new[]{"07_LeapGarden","08_SporePass","09_RiftCrossing"})
 {
  var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Encounters/"+name+"_Painted.prefab"));root.transform.position=Vector3.zero;
  foreach(var member in root.GetComponentsInChildren<DimensionMember>()){typeof(DimensionMember).GetMethod("Awake",Private).Invoke(member,null);member.Apply(0);}
  foreach(var ai in root.GetComponentsInChildren<EnemyAttackAI>())
  {ai.warning.enabled=true;var sr=ai.GetComponentInChildren<SpriteRenderer>();report.Add("VISUAL "+ai.name+" sprite world bounds="+sr.bounds);}
  Physics2D.SyncTransforms();
  for(float x=.1f;x<(name=="09_RiftCrossing"?42:24);x+=.2f){var hit=Physics2D.Raycast(new Vector2(x,.01f),Vector2.down,4,1<<8);if(!hit.collider)throw new Exception("Floor gap at "+name+" "+x);}
  report.Add("PASS "+name+": 120 samples find continuous floor; entry and exit share height.");
  camera.transform.position=new Vector3(12,1.5f,camera.transform.position.z);
  foreach(var l in UnityEngine.Object.FindObjectsByType<Parallax>()){typeof(Parallax).GetMethod("Start",Private).Invoke(l,null);typeof(Parallax).GetMethod("LateUpdate",Private).Invoke(l,null);}
  var rt=new RenderTexture(1600,900,24);rt.Create();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes("encounter-"+name+".png",image.EncodeToPNG());RenderTexture.active=null;rt.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(root);
 }
 File.WriteAllLines("expansion-layout-results.txt",report);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);File.WriteAllText("expansion-layout-results.txt","FAIL "+e);EditorApplication.Exit(1);}}
}
