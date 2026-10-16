using System;using System.IO;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;
[InitializeOnLoad]
public static class CombatPlayChecks
{
 const string Key="DimensionShifter.CombatPlayChecks";
 static int phase;static float since;static PlayerAttack attack;static EnemyHealth enemy;static string report="";
 static CombatPlayChecks(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Update;}
 public static void Run(){
  EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");
  UnityEngine.Object.FindAnyObjectByType<LevelGenerator>().enabled=false;
  UnityEngine.Object.FindAnyObjectByType<PlayerMovement>().enabled=false;
  UnityEngine.Object.FindAnyObjectByType<EnemyPatrol>().enabled=false;
  UnityEngine.Object.FindAnyObjectByType<CameraFollow>().enabled=false;
  SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
 }
 static void Need(bool value,string message){if(!value)throw new Exception(message);}
 static void Pass(string text){report+="PASS "+text+"\n";}
 static void Update(){if(!EditorApplication.isPlaying)return;try{
  if(phase==0){attack=UnityEngine.Object.FindAnyObjectByType<PlayerAttack>();enemy=UnityEngine.Object.FindAnyObjectByType<EnemyHealth>();Need(attack!=null&&enemy!=null,"Missing actors");
   var rb=attack.GetComponent<Rigidbody2D>();rb.gravityScale=0;rb.constraints=RigidbodyConstraints2D.FreezeAll;rb.position=new Vector2(0,1);attack.transform.position=new Vector3(0,1,0);rb.interpolation=RigidbodyInterpolation2D.None;
   enemy.GetComponent<Rigidbody2D>().gravityScale=0;enemy.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;enemy.transform.position=new Vector3(1,1,0);Physics2D.SyncTransforms();
   var delta=(Vector2)attack.GetComponent<Collider2D>().bounds.center+new Vector2(.95f,-.2f)-(Vector2)enemy.GetComponent<Collider2D>().bounds.center;enemy.transform.position+=(Vector3)delta;Physics2D.SyncTransforms();
   var camera=Camera.main;camera.orthographic=true;camera.orthographicSize=2.5f;camera.transform.position=new Vector3(.5f,1.8f,camera.transform.position.z);
   foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
   Need(enemy.CurrentHealth==3&&!enemy.HealthBarVisible,"Initial health/UI wrong");Pass("real scene starts at 3 HP with health bar hidden");
   attack.StartCoroutine("NormalAttackRoutine");since=Time.realtimeSinceStartup;phase=1;
  }else if(phase==1 && Time.realtimeSinceStartup-since>.2f){Need(enemy.CurrentHealth==2&&enemy.HealthBarVisible,"Normal attack/UI failed");Need(enemy.IsStunned,"Hurt stun absent");Need(enemy.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Slime_Hurt"),"Hurt animation missing");Pass("normal animation strikes once; Hurt state and health bar are visible");Capture("combat-hit.png");phase=2;
  }else if(phase==2 && Time.realtimeSinceStartup-since>2.5f){Need(enemy.CurrentHealth==2,"Repeated damage in same swing");Need(!enemy.HealthBarVisible,"Health bar did not hide");Need(Time.timeScale==1f,"Hit pause did not restore time");Pass("health bar fades away; hit pause restores time; no repeated damage");attack.StartCoroutine("DashAttackRoutine");since=Time.realtimeSinceStartup;phase=3;
  }else if(phase==3 && Time.realtimeSinceStartup-since>.2f){Need(enemy!=null&&enemy.IsDead,"Dash did not deal two damage");Need(!enemy.GetComponent<Rigidbody2D>().simulated,"Dead enemy still simulated");foreach(var c in enemy.GetComponentsInChildren<Collider2D>())Need(!c.enabled,"Ghost collider");Need(enemy.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Slime_Die"),"Death animation absent");Need(!enemy.HealthBarVisible,"Dead health bar visible");Pass("dash kills; Die animation plays; movement, colliders and bar disable immediately");Capture("combat-death.png");phase=4;
  }else if(phase==4 && Time.realtimeSinceStartup-since>1f){Need(enemy==null,"Dead enemy was not removed");Need(GameObject.Find("Slime health")==null,"Orphan health bar");Need(Time.timeScale==1f,"Death pause stuck");Pass("death animation finishes, enemy/bar are removed, game time resumes");Finish(0);}
 }catch(Exception e){report+="FAIL "+e+"\n";Finish(1);}}
 static void Capture(string path){var rt=new RenderTexture(1200,675,24);rt.Create();RenderPipeline.SubmitRenderRequest(Camera.main,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var texture=new Texture2D(1200,675,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1200,675),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());RenderTexture.active=null;rt.Release();UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(rt);}
 static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.update-=Update;File.WriteAllText("combat-play-results.txt",report);EditorApplication.Exit(code);}
}
