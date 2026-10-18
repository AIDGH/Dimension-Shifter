using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class CurrentPolishChecks {
 static bool boot;static string report="";static double started;
 static CurrentPolishChecks(){if(SessionState.GetBool("CurrentPolishChecks",false))EditorApplication.update+=Boot;}
 public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/HollowGreenpath.unity");Physics2D.simulationMode=SimulationMode2D.FixedUpdate;SessionState.SetBool("CurrentPolishChecks",true);EditorApplication.isPlaying=true;}
 static void Boot(){EditorApplication.QueuePlayerLoopUpdate();if(!EditorApplication.isPlaying)return;if(boot){if(EditorApplication.timeSinceStartup-started>150)Done(1,"Timeout");return;}if(Time.frameCount<3)return;boot=true;started=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;new GameObject("Current polish checks").AddComponent<CurrentPolishRunner>().StartCoroutine("Run");}
 static void Log(string m,string s,LogType t){if((t==LogType.Error||t==LogType.Exception)&&!s.Contains("UnityEditor.Search"))Done(1,m+"\n"+s);}
 public static void Need(bool b,string m){if(!b)throw new Exception(m);report+="PASS "+m+"\n";File.WriteAllText("current-polish-results.txt",report);}
 public static void Done(int code,string m=""){SessionState.SetBool("CurrentPolishChecks",false);File.WriteAllText("current-polish-results.txt",report+m);EditorApplication.Exit(code);}
}
public class CurrentPolishRunner:MonoBehaviour {
 static void Need(bool b,string m)=>CurrentPolishChecks.Need(b,m);
 static void Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
 static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
 GameObject Floor(Vector2 center,Vector2 size){var g=new GameObject("Test floor");g.layer=8;g.transform.position=center;g.AddComponent<BoxCollider2D>().size=size;return g;}
 EnemyHealth Target(Vector2 position){var g=new GameObject("Blade target");g.SetActive(false);g.layer=10;g.transform.position=position;var rb=g.AddComponent<Rigidbody2D>();rb.constraints=RigidbodyConstraints2D.FreezeAll;g.AddComponent<BoxCollider2D>().size=new Vector2(.3f,.5f);var h=g.AddComponent<EnemyHealth>();h.maxHealth=20;g.SetActive(true);return h;}
 IEnumerator Run(){
  var s=DemoSession.Instance;s.Begin();yield return new WaitForSeconds(1);
  var pm=s.GetComponent<PlayerMovement>();float originalMoveSpeed=pm.moveSpeed;var rb=s.GetComponent<Rigidbody2D>();var attack=s.GetComponent<PlayerAttack>();var visual=s.GetComponent<SpriteRenderer>();var rewards=s.GetComponent<PlayerRewards>();
  Need(!pm.spriteFacesRight&&visual.flipX,"Knight left-facing source is mirrored visually without reversing controls");
  Need(Mathf.Approximately(Camera.main.orthographicSize,6.5f),"Camera shows wider environment without scaling terrain or colliders");
  foreach(var tex in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader.name=="DimensionShifter/HollowMap").Select(r=>r.sharedMaterial.mainTexture).Distinct())Need(tex.filterMode==FilterMode.Bilinear,"Map texture uses smooth sampling: "+tex.name);
  Capture("polish-hollow-entry.png");
  pm.enabled=false;s.GetComponent<RunProgress>().enabled=false;rb.constraints=RigidbodyConstraints2D.FreezeAll;rb.position=new Vector2(1000,1000);s.transform.position=rb.position;
  Need(attack.EffectiveNormalDamage==1&&attack.EffectiveDashDamage==2,"Level 1 preserves original blade and dash strength");
  rewards.EnemyDefeated(100,100);Need(rewards.Level==2&&attack.EffectiveNormalDamage==1&&Mathf.Abs(attack.EffectiveDashCooldown-2.85f)<.001f,"Level 2 reduces dash cooldown without doubling early damage");
  rewards.EnemyDefeated(100,100);Need(rewards.Level==3&&attack.EffectiveNormalDamage==2,"Level 3 grants first blade damage milestone");
  foreach(float facing in new[]{1f,-1f}){
   s.transform.localScale=new Vector3(Mathf.Abs(s.transform.localScale.x)*facing,s.transform.localScale.y,s.transform.localScale.z);Physics2D.SyncTransforms();
   var forward=Target(rb.position+Vector2.right*facing*.95f);var behind=Target(rb.position-Vector2.right*facing*.95f);Physics2D.SyncTransforms();Call(attack,"BeginAttack");Call(attack,"ResolveHits");
   Need(forward.CurrentHealth==18&&behind.CurrentHealth==20,"Leveled blade hits only intended side: "+facing);Call(attack,"FinishAttack");Destroy(forward.gameObject);Destroy(behind.gameObject);yield return null;
  }
  s.ClaimSpirit();var weapon=s.GetComponent<SpiritWeapon>();
  foreach(float facing in new[]{1f,-1f}){
   s.transform.localScale=new Vector3(Mathf.Abs(s.transform.localScale.x)*facing,s.transform.localScale.y,s.transform.localScale.z);Set(weapon,"readyAt",0f);Need(weapon.Fire(),"Cast can fire: "+facing);var bolt=Object.FindAnyObjectByType<SpiritBolt>();float x=bolt.transform.position.x;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();Need((bolt.transform.position.x-x)*facing>0&&bolt.damage==3,"Cast follows logical facing and receives level bonus: "+facing);Destroy(bolt.gameObject);yield return null;
   rb.constraints=RigidbodyConstraints2D.FreezeRotation;rb.gravityScale=0;var start=rb.position;
   var routine=(IEnumerator)typeof(PlayerAttack).GetMethod("DashAttackRoutine",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(attack,null);StartCoroutine(routine);yield return new WaitForSeconds(.15f);Need((rb.position.x-start.x)*facing>1,"Dash moves in intended direction: "+facing);yield return new WaitForSeconds(.4f);rb.constraints=RigidbodyConstraints2D.FreezeAll;
  }
  rewards.EnemyDefeated(0,200);Need(rewards.Level==5&&rewards.DamageBonus==2,"Level 5 grants second damage milestone");rewards.EnemyDefeated(0,200);Need(rewards.Level==7&&rewards.DamageBonus==3,"Level 7 grants third damage milestone");rewards.EnemyDefeated(0,2000);Need(rewards.DamageBonus==3&&Mathf.Abs(attack.EffectiveDashCooldown-2.4f)<.001f,"Damage and cooldown improvements remain capped at high XP");Need(pm.moveSpeed==originalMoveSpeed,"Level growth does not alter challenge movement speed");
  Floor(new Vector2(2000,999.5f),new Vector2(18,1));
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Encounters/PouncingSlime.prefab");var slime=Instantiate(prefab,new Vector3(2000,1001),Quaternion.identity);var ai=slime.GetComponent<EnemyAttackAI>();var health=slime.GetComponent<EnemyHealth>();var anim=slime.GetComponent<Animator>();yield return new WaitForSeconds(1.1f);
  Need(anim.GetCurrentAnimatorStateInfo(0).IsName("Slime_Idle"),"Waiting pouncing slime uses idle animation");health.TakeDamage(1,slime.transform.position,0);yield return new WaitForSeconds(.5f);Need(anim.GetCurrentAnimatorStateInfo(0).IsName("Slime_Idle"),"Hurt recovery no longer leaves stationary slime running");slime.SetActive(false);slime.SetActive(true);yield return new WaitForSeconds(.15f);Need(anim.GetCurrentAnimatorStateInfo(0).IsName("Slime_Idle"),"World reactivation restores stationary attack slime idle");
  Destroy(ai);yield return null;var patrol=slime.AddComponent<EnemyPatrol>();patrol.walkableLayers=1<<8;patrol.movingRight=true;patrol.speed=2;var badSensor=new GameObject("Legacy sensor behind body");badSensor.transform.SetParent(slime.transform,false);badSensor.transform.localPosition=new Vector3(-100,0);patrol.ledgeDetector=badSensor.transform;float beforeX=slime.transform.position.x;yield return new WaitForSeconds(.8f);Need(slime.transform.position.x>beforeX+1,"Patrol moves despite legacy/mirrored misplaced point sensor");Need(anim.GetCurrentAnimatorStateInfo(0).IsName("Slime_Move"),"Actually moving slime uses movement animation");
  Floor(new Vector2(slime.transform.position.x+1.5f,1001),new Vector2(.5f,4));yield return new WaitForSeconds(1.5f);Need(!patrol.movingRight,"Patrol turns at a real wall instead of running in place");Destroy(slime);yield return null;
  // Check saved end prefabs retain terrain while replacing only old tree pixels.
  var end=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Encounters/TrailEnd_Painted.prefab"),new Vector3(3000,0),Quaternion.identity);
  Need(end.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.name=="Sanctuary canopy")==1,"Final chunk has one complete painted sanctuary tree");
  var tree=end.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Sanctuary canopy");var cam=Camera.main;cam.GetComponent<HollowRoomCamera>().enabled=false;cam.transform.position=tree.bounds.center+new Vector3(0,0,-10);cam.orthographicSize=6;Capture("polish-sanctuary-tree.png");
  var boss=Object.FindAnyObjectByType<RiftWarden>();boss.enabled=false;Call(boss,"Enter",RiftWarden.Phase.Chase,100f);var beam=new GameObject("Preview lance").AddComponent<WardenBeam>();beam.Launch(new Vector2(4000,20),Vector2.right,boss.beamMaterial,boss);Need(boss.beamMaterial.shader.name=="DimensionShifter/WardenLance","Boss uses dedicated tapered cyan-white lightning material");cam.transform.position=new Vector3(4000,20,-10);cam.orthographicSize=1.6f;Capture("polish-warden-lance.png");
  CurrentPolishChecks.Done(0);
 }
 void Capture(string name){var cam=Camera.main;var rc=cam.GetComponent<HollowRoomCamera>();if(rc!=null&&rc.enabled){rc.Snap();rc.SendMessage("LateUpdate");}var canvas=Object.FindObjectsByType<Canvas>().First(c=>c.name=="Dimension HUD");bool show=name.Contains("entry");canvas.enabled=show;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;var rt=new RenderTexture(1600,900,24);rt.Create();cam.aspect=1600f/900;cam.targetTexture=rt;canvas.GetComponent<CanvasScaler>().SendMessage("Handle");Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var t=new Texture2D(1600,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1600,900),0,0);t.Apply();File.WriteAllBytes(name,t.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;rt.Release();Object.Destroy(rt);Object.Destroy(t);canvas.enabled=true;canvas.renderMode=RenderMode.ScreenSpaceOverlay;}
}
