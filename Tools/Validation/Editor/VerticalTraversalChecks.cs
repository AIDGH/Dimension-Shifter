using System;using System.IO;using System.Reflection;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
// Exercise the shipped player collider and movement settings against both styles of new terrain.
public static class VerticalTraversalChecks
{
 static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static PlayerMovement motor;static Rigidbody2D body;
 static void Set(string field,object value)=>typeof(PlayerMovement).GetField(field,Private).SetValue(motor,value);
 static void Call(string method)=>typeof(PlayerMovement).GetMethod(method,Private).Invoke(motor,null);
 static void Step(){Physics2D.SyncTransforms();Call("FixedUpdate");Physics2D.Simulate(Time.fixedDeltaTime);}
 public static void Run(){var report=new List<string>();try{
  Physics2D.simulationMode=SimulationMode2D.Script;
  foreach(string suffix in new[]{"","_Painted"})foreach(bool down in new[]{false,true})foreach(bool acceleration in new[]{false,true})
  {
   EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");motor=Object.FindAnyObjectByType<PlayerMovement>();body=motor.GetComponent<Rigidbody2D>();motor.useAcceleration=acceleration;Call("Awake");
   string name=(down?"11_HollowDescent":"10_CrystalAscent")+suffix;var chunk=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Encounters/"+name+".prefab"));chunk.transform.position=new Vector3(1000,0);
   for(int step=1;step<=3;step++)
   {
    float edge=1000+step*4;float from=(down?-1:1)*(step-1)*2;float target=(down?-1:1)*step*2;
    body.position=new Vector2(edge-1.2f,from+.7f);body.linearVelocity=Vector2.zero;body.rotation=0;Set("moveInput",0f);for(int i=0;i<35;i++)Step();
    if(!motor.isGrounded)throw new Exception(name+" start not grounded "+step);
    if(!down){Set("jumpBufferRemaining",.12f);Set("bufferedJumpReleased",false);}
    for(int i=0;i<160;i++){Set("moveInput",body.position.x<edge+1?1f:0f);Step();}
    if(body.position.x<edge+.5f||!motor.isGrounded||Mathf.Abs(motor.GetComponent<Collider2D>().bounds.min.y-target)>.06f)throw new Exception(name+" step "+step+" cannot traverse: "+body.position+" grounded="+motor.isGrounded);
    report.Add("PASS "+name+" step "+step+" "+(down?"descent":"jump ascent")+" acceleration="+acceleration);
   }
  }
  File.WriteAllLines("vertical-traversal-results.txt",report);EditorApplication.Exit(0);
 }catch(Exception e){report.Add("FAIL "+e);File.WriteAllLines("vertical-traversal-results.txt",report);Debug.LogException(e);EditorApplication.Exit(1);}}
}
