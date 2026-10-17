using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
public static class DemoLayoutChecks
{
 static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
 static List<string> report=new List<string>();
 static void Need(bool ok,string msg){if(!ok)throw new Exception(msg);report.Add("PASS "+msg);}
 static void Call(object o,string method)=>o.GetType().GetMethod(method,Hidden).Invoke(o,null);
 static void Set(object o,string field,object value)=>o.GetType().GetField(field,Hidden).SetValue(o,value);
 public static void Run(){try{
  foreach(var scene in new[]{"Assets/Scenes/SampleScene.unity","Assets/Scenes/PaintedDimension.unity"})
  {
   EditorSceneManager.OpenScene(scene);Need(Object.FindObjectsByType<EnemyPatrol>().Length==0,scene+" has no standalone intro slime");var gen=Object.FindAnyObjectByType<LevelGenerator>();Need(gen.authoredOnly&&gen.encounterChunks.Length==10,scene+" contains all ten mandatory encounters");Call(gen,"Start");
   for(int i=0;i<20;i++){var end=(Transform)typeof(LevelGenerator).GetField("lastEndPosition",Hidden).GetValue(gen);gen.player.position=end.position;Call(gen,"Update");}
   foreach(var prefab in gen.encounterChunks)Need(Object.FindObjectsByType<Transform>().Any(t=>t.name==prefab.name+"(Clone)"),scene+" reaches "+prefab.name);
   var finish=Object.FindObjectsByType<TrailMarker>().FirstOrDefault(m=>m.kind==TrailMarker.MarkerKind.Exit);Need(finish!=null,scene+" reaches final sanctuary");Need(finish.transform.position.x<gen.cameraFollow.maxX+Camera.main.orthographicSize*Camera.main.aspect,scene+" final sanctuary remains inside camera coverage");
   foreach(var ai in Object.FindObjectsByType<EnemyAttackAI>(FindObjectsInactive.Include))Need(ai.warning!=null&&AssetDatabase.GetAssetPath(ai.warning.sprite).EndsWith("Warning.png"),ai.name+" has a real warning symbol");
  }
  foreach(string suffix in new[]{"","_Painted"})foreach(string name in new[]{"13_KeySanctum","14_RelayTrial","15_ProvingGrounds","16_WardenArena"})
  {
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Encounters/"+name+suffix+".prefab"));Physics2D.SyncTransforms();int width=(int)root.transform.Find("EndPosition").localPosition.x;
   for(float x=.25f;x<width;x+=.5f)Need(Physics2D.Raycast(new Vector2(x,.01f),Vector2.down,1,1<<8).collider!=null,name+suffix+" continuous main floor x="+x);
   foreach(var gate in root.GetComponentsInChildren<QuestGate>())Need(gate.barrier.bounds.max.y>40,name+suffix+" mandatory gate cannot be jumped over");
  }
  // Actual movement on each required staircase, including the long second relay gap.
  foreach(string suffix in new[]{"","_Painted"})foreach(bool acceleration in new[]{false,true})foreach(string name in new[]{"13_KeySanctum","14_RelayTrial"})
  {
   EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");Physics2D.simulationMode=SimulationMode2D.Script;
   var motor=Object.FindAnyObjectByType<PlayerMovement>();var body=motor.GetComponent<Rigidbody2D>();motor.useAcceleration=acceleration;Call(motor,"Awake");
   var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Encounters/"+name+suffix+".prefab"));root.transform.position=new Vector3(1000,0);
   foreach(var member in root.GetComponentsInChildren<DimensionMember>())foreach(var col in member.GetComponentsInChildren<Collider2D>())col.isTrigger=true;
   foreach(var enemy in root.GetComponentsInChildren<EnemyHealth>())enemy.gameObject.SetActive(false);
   var steps=name=="13_KeySanctum"?new[]{new Vector4(5.8f,0,8.2f,2),new Vector4(9,2,13,4),new Vector4(14,4,18,6)}:new[]{new Vector4(5.8f,0,8.2f,2),new Vector4(9,2,14,4),new Vector4(15,4,19,6),new Vector4(47.8f,0,50,3),new Vector4(52,3,56,5)};
   foreach(var step in steps)
   {
    body.position=new Vector2(1000+step.x,step.y+.7f);body.linearVelocity=Vector2.zero;Set(motor,"moveInput",0f);
    for(int i=0;i<35;i++){Physics2D.SyncTransforms();Call(motor,"FixedUpdate");Physics2D.Simulate(Time.fixedDeltaTime);}
    Need(motor.isGrounded,name+" jump starts supported");Set(motor,"jumpBufferRemaining",.12f);Set(motor,"bufferedJumpReleased",false);
    for(int i=0;i<155;i++){Set(motor,"moveInput",body.position.x<1000+step.z?1f:0f);if(i==19){Set(motor,"jumpBufferRemaining",.12f);Set(motor,"bufferedJumpReleased",false);}Physics2D.SyncTransforms();Call(motor,"FixedUpdate");Physics2D.Simulate(Time.fixedDeltaTime);}
    Need(body.position.x>1000+step.z-.4f&&motor.isGrounded&&Mathf.Abs(motor.GetComponent<Collider2D>().bounds.min.y-step.w)<.07f,name+suffix+" jump to "+step.z+" acceleration="+acceleration+" reachable with shipped double jump (actual "+body.position+", grounded="+motor.isGrounded+")");
   }
  }
  File.WriteAllLines("demo-layout-results.txt",report);EditorApplication.Exit(0);
 }catch(Exception e){report.Add("FAIL "+e);File.WriteAllLines("demo-layout-results.txt",report);Debug.LogException(e);EditorApplication.Exit(1);}}
}
