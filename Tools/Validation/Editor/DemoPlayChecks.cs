using System;using System.IO;using System.Linq;using System.Collections;using System.Reflection;using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class DemoPlayChecks
{
 const string Key="DimensionShifter.DemoChecks";static bool started;static string report="";
 static DemoPlayChecks(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Boot;}
 public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");Physics2D.simulationMode=SimulationMode2D.FixedUpdate;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 static void Boot(){EditorApplication.QueuePlayerLoopUpdate();if(!EditorApplication.isPlaying||started||Time.frameCount<3)return;started=true;Application.logMessageReceived+=Log;new GameObject("Demo regression").AddComponent<DemoCheckRunner>().StartCoroutine("Check");}
 static void Log(string msg,string stack,LogType type){if((type==LogType.Error||type==LogType.Exception)&&!stack.Contains("UnityEditor.Search"))Finish(1,msg+"\n"+stack);}
 public static void Need(bool ok,string text){if(!ok)throw new Exception(text);report+="PASS "+text+"\n";File.WriteAllText("demo-play-results.txt",report);}
 public static void Finish(int code,string msg=""){SessionState.SetBool(Key,false);EditorApplication.update-=Boot;Application.logMessageReceived-=Log;File.WriteAllText("demo-play-results.txt",report+msg);Time.timeScale=1;EditorApplication.Exit(code);}
}
public class DemoCheckRunner:MonoBehaviour
{
 PlayerHealth hp;Rigidbody2D body;DemoSession session;DimensionController dimension;Camera cam;Canvas canvas;
 static void Need(bool ok,string msg)=>DemoPlayChecks.Need(ok,msg);
 GameObject Spawn(string name,float x)=>Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Encounters/"+name+".prefab"),new Vector3(x,0),Quaternion.identity);
 void Place(float x,float y){body.position=new Vector2(x,y);hp.transform.position=new Vector3(x,y);body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
 void Freeze(){hp.GetComponent<PlayerMovement>().enabled=false;body.constraints=RigidbodyConstraints2D.FreezeAll;body.gravityScale=0;}
 void Vulnerable(){typeof(PlayerHealth).GetField("invincibleRemaining",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(hp,0f);}
 void Protected(){typeof(PlayerHealth).GetField("invincibleRemaining",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(hp,1000f);}
 IEnumerator Check()
 {
  hp=Object.FindAnyObjectByType<PlayerHealth>();session=hp.GetComponent<DemoSession>();body=hp.GetComponent<Rigidbody2D>();dimension=hp.GetComponent<DimensionController>();cam=Camera.main;canvas=Object.FindObjectsByType<Canvas>().First(c=>c.name=="Dimension HUD");
  yield return new WaitForSecondsRealtime(.2f);Need(!session.Started&&Time.timeScale==0&&session.Elapsed==0,"Start menu holds gameplay and timer before Begin");Capture("demo-start-menu.png");
  Need(Object.FindObjectsByType<EnemyPatrol>().Length==0,"Standalone falling intro slime is absent");
  var begin=canvas.GetComponentsInChildren<Button>().First(b=>b.name=="Begin journey");var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};ExecuteEvents.Execute(begin.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return null;
  Need(session.Started&&Time.timeScale==1&&hp.GetComponent<PlayerMovement>().enabled,"Begin button starts actual gameplay");
  var generator=Object.FindAnyObjectByType<LevelGenerator>();Need(generator.authoredOnly&&generator.encounterChunks.Length==10,"Demo uses ten mandatory authored encounters");generator.enabled=false;Freeze();cam.GetComponent<CameraFollow>().enabled=false;cam.GetComponent<CameraFollow>().maxX=3000;
  hp.GetComponent<RunProgress>().Complete();Need(!hp.GetComponent<RunProgress>().Completed,"Demo cannot complete before Warden defeat");
  var sanctuary=Spawn("13_KeySanctum_Painted",1000);yield return null;
  var items=sanctuary.GetComponentsInChildren<QuestInteractable>();var key=items.First(q=>q.kind==QuestInteractable.Kind.EchoKey);var door=items.First(q=>q.kind==QuestInteractable.Kind.BlueDoor);var spirit=items.First(q=>q.kind==QuestInteractable.Kind.Spirit);var portal=items.First(q=>q.kind==QuestInteractable.Kind.ReturnPortal);
  Need(!hp.GetComponent<SpiritWeapon>().Fire(),"Spirit casting is locked before discovery");
  dimension.SetDimension(0);Place(1031,1);Need(!door.Interact(session),"Blue door rejects entry without key");
  dimension.SetDimension(0);Place(key.transform.position.x,key.transform.position.y);yield return null;Need(!session.HasEchoKey,"Key cannot be collected in Frost");
  dimension.SetDimension(1);yield return null;Need(session.HasEchoKey,"Echo key is collected in violet world");Place(door.transform.position.x,door.transform.position.y+.6f);Need(!door.Interact(session),"Key alone does not open door in wrong dimension");
  dimension.SetDimension(0);Need(door.Interact(session),"Echo key opens the door in Frost");Need(Vector2.Distance(body.position,door.destination.position)<.01f,"Door teleports into elevated sanctuary");
  cam.transform.position=new Vector3(1035,20,-44.25f);yield return new WaitForSeconds(.3f);Capture("demo-hidden-sanctuary.png");
  Place(portal.transform.position.x,portal.transform.position.y+.6f);Need(!portal.Interact(session),"Return portal waits for the sanctuary reward");Place(spirit.transform.position.x,spirit.transform.position.y);Need(spirit.Interact(session)&&session.HasSpirit,"Altar permanently unlocks spirit bolt");Place(portal.transform.position.x,portal.transform.position.y+.6f);Need(portal.Interact(session),"Return portal restores player to trail");yield return null;Need(sanctuary.GetComponentInChildren<QuestGate>().Open,"Spirit discovery opens mandatory route gate");
  // Physical stomp: actual collider falls onto mushroom, inflicts exactly one hit, and rebounds.
  var floor=new GameObject("Stomp test floor");floor.layer=8;floor.transform.position=new Vector3(1130,-.5f);var ground=floor.AddComponent<BoxCollider2D>();ground.size=new Vector2(12,1);
  var mushroom=Spawn("SporeKeeper",1130);mushroom.GetComponent<EnemyAttackAI>().enabled=false;yield return new WaitForSeconds(.2f);
  var enemy=mushroom.GetComponent<EnemyHealth>();dimension.SetDimension(1);body.constraints=RigidbodyConstraints2D.FreezeRotation;body.gravityScale=4;Place(1130,3.8f);Vulnerable();float healthBefore=hp.currentHealth;int before=hp.GetComponent<PlayerEnemyContact>().StompCount;
  for(int i=0;i<100&&hp.GetComponent<PlayerEnemyContact>().StompCount==before;i++)yield return new WaitForFixedUpdate();
  Need(hp.GetComponent<PlayerEnemyContact>().StompCount==before+1&&enemy.CurrentHealth==enemy.maxHealth-1,"Landing on mushroom deals exactly one damage");Need(hp.currentHealth==healthBefore&&body.linearVelocity.y>0,"Successful stomp bounces without contact damage");Freeze();Place(1126,1);
  var shielded=Spawn("SporeKeeper",1186);shielded.GetComponent<EnemyAttackAI>().enabled=false;shielded.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
  var wall=new GameObject("Bolt blocking wall");wall.layer=8;wall.transform.position=new Vector3(1183,1);wall.AddComponent<BoxCollider2D>().size=new Vector2(.5f,4);Place(1180,.6f);hp.transform.localScale=new Vector3(Mathf.Abs(hp.transform.localScale.x),hp.transform.localScale.y,1);hp.GetComponent<SpriteRenderer>().flipX=false;Physics2D.SyncTransforms();
  Need(hp.GetComponent<SpiritWeapon>().Fire(),"Spirit bolt can be fired toward an enemy");yield return new WaitForSeconds(.7f);Need(shielded.GetComponent<EnemyHealth>().CurrentHealth==4,"Terrain shields enemies from spirit projectiles");Destroy(wall);yield return new WaitForSeconds(.75f);hp.GetComponent<SpiritWeapon>().Fire();yield return new WaitForSeconds(.7f);Need(shielded.GetComponent<EnemyHealth>().CurrentHealth==2,"Unblocked spirit bolt deals two enemy damage");Destroy(shielded);yield return new WaitForSeconds(.75f);
  var relayRoom=Spawn("14_RelayTrial_Painted",1200);yield return null;var relays=relayRoom.GetComponentsInChildren<QuestInteractable>().Where(q=>q.kind==QuestInteractable.Kind.Relay).ToArray();
  foreach(var relay in relays)
  {
   dimension.SetDimension(1-relay.dimension);Need(!relay.ReceiveBolt(1-relay.dimension),"Wrong-world bolt cannot activate "+relay.name);
   dimension.SetDimension(relay.dimension);Place(relay.transform.position.x-3,relay.transform.position.y);body.position+=(Vector2)(hp.transform.position-hp.GetComponent<Collider2D>().bounds.center);hp.transform.position=body.position;Physics2D.SyncTransforms();
   hp.GetComponent<SpriteRenderer>().flipX=false;var scale=hp.transform.localScale;scale.x=Mathf.Abs(scale.x);hp.transform.localScale=scale;
   Need(hp.GetComponent<SpiritWeapon>().Fire(),"Unlocked C cast emits a projectile");yield return new WaitForSeconds(.45f);Need((session.Relays&(1<<relay.dimension))!=0,"Physical spirit projectile awakens "+relay.name);int score=session.GetComponent<PlayerRewards>().Score;Need(!relay.ReceiveBolt(relay.dimension)&&session.GetComponent<PlayerRewards>().Score==score,"Activated seal cannot award duplicate rewards");yield return new WaitForSeconds(1.1f);
  }
  yield return null;Need(session.Relays==3&&relayRoom.GetComponentInChildren<QuestGate>().Open,"Both seals open the relay gate");
  hp.GetComponent<RunProgress>().SetCheckpoint(new Vector3(1126,1),"Regression checkpoint");Vulnerable();hp.TryTakeDamage(10000);yield return null;float stopped=session.Elapsed;yield return new WaitForSeconds(.2f);Need(session.Elapsed==stopped,"Run timer pauses while dead");hp.GetComponent<RunProgress>().Respawn();Freeze();Need(session.HasEchoKey&&session.HasSpirit&&session.Relays==3,"Checkpoint return preserves quest discoveries");
  var trialRoot=Spawn("15_ProvingGrounds_Painted",1500);Place(1512,1);Protected();yield return new WaitForSeconds(1.3f);var trial=trialRoot.GetComponentInChildren<TrialArena>();Need(trial.Active&&trial.Wave==1&&trial.entrance.enabled,"Proving grounds locks entrance and starts first wave");
  hp.GetComponent<RunProgress>().SetCheckpoint(new Vector3(1502,.7f),"Trial threshold");Vulnerable();hp.TryTakeDamage(10000);yield return new WaitForSeconds(.2f);Need(!trial.Active&&trial.Wave==0&&!trial.entrance.enabled,"Dying resets the trial and releases its entrance");hp.GetComponent<RunProgress>().Respawn();Freeze();Place(1512,1);Protected();yield return new WaitForSeconds(1.3f);
  for(int wave=1;wave<=3;wave++)
  {
   for(int world=0;world<2;world++){dimension.SetDimension(world);yield return null;foreach(var e in Object.FindObjectsByType<EnemyHealth>().Where(e=>e.transform.parent!=null&&e.transform.parent.name.StartsWith("Trial ")).ToArray())e.TakeDamage(1000,body.position);yield return new WaitForSeconds(.4f);}
   yield return new WaitForSeconds(1.8f);
  }
  Need(session.TrialCleared&&!trial.entrance.enabled,"Three waves across both worlds unlock the trial");
  var bossRoot=Spawn("16_WardenArena_Painted",1700);Place(1711,1);Protected();var boss=bossRoot.GetComponentInChildren<RiftWarden>();var bossHealth=boss.GetComponent<EnemyHealth>();yield return new WaitForSeconds(.1f);
  Need(boss.Engaged&&boss.entrance.enabled,"Warden engages and seals arena entrance");Need(!bossHealth.TakeDamage(1,body.position),"Armored Warden rejects premature hits");float oldX=boss.transform.position.x;
  for(int i=0;i<1800&&boss.State!=RiftWarden.Phase.Exposed;i++)yield return new WaitForFixedUpdate();
  Need((boss.MeleeCount>0||boss.BeamCount>0)&&boss.transform.position.x<oldX,"Warden pursues and completes an attack cycle");
  dimension.SetDimension(1-boss.CoreDimension);Need(!bossHealth.TakeDamage(1,body.position),"Wrong dimension cannot damage exposed core");dimension.SetDimension(boss.CoreDimension);Need(bossHealth.TakeDamage(40,body.position),"Matching world damages exposed core");Need(!bossHealth.TakeDamage(1,body.position),"Boss hit cooldown prevents duplicate same-frame damage");Need(boss.Enraged,"Below half health activates enraged phase");
  cam.transform.position=new Vector3(1719,2.3f,-44.25f);yield return new WaitForSeconds(.25f);Capture("demo-warden.png");
  int shots=boss.ShotCount;typeof(RiftWarden).GetMethod("FireFan",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(boss,null);Need(boss.ShotCount>=shots+5,"Enraged Warden fires a five-bolt fan");
  hp.GetComponent<RunProgress>().SetCheckpoint(new Vector3(1702,.7f),"Warden threshold");Vulnerable();hp.TryTakeDamage(10000);yield return new WaitForSeconds(.2f);Need(boss.State==RiftWarden.Phase.Dormant&&bossHealth.CurrentHealth==bossHealth.maxHealth&&!boss.entrance.enabled,"Death resets boss health and releases the arena for retry");hp.GetComponent<RunProgress>().Respawn();Freeze();Place(1711,1);Protected();
  for(int i=0;i<1800&&boss.State!=RiftWarden.Phase.Exposed;i++)yield return new WaitForFixedUpdate();dimension.SetDimension(boss.CoreDimension);Need(bossHealth.TakeDamage(1000,body.position),"Exposed Warden can be defeated");yield return new WaitForSeconds(.15f);
  Need(session.BossDefeated&&bossRoot.GetComponentInChildren<QuestGate>().Open,"Boss defeat opens final route gate");hp.GetComponent<RunProgress>().Complete();yield return new WaitForSeconds(.5f);Need(hp.GetComponent<RunProgress>().Completed&&hp.GetComponent<GameHUD>().OutcomeVisible,"Final outcome displays only after Warden defeat");Capture("demo-complete.png");Need(session.Elapsed>1,"Journey clock counts active gameplay");
  DontDestroyOnLoad(gameObject);var restart=canvas.GetComponentsInChildren<Button>().First(b=>b.name=="Return to sanctuary");ExecuteEvents.Execute(restart.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return new WaitForSecondsRealtime(.4f);var fresh=DemoSession.Instance;Need(fresh!=null&&!fresh.Started&&!fresh.HasEchoKey&&!fresh.HasSpirit&&fresh.Elapsed==0,"Journey Again returns to start menu with fresh quest state and timer");DemoPlayChecks.Finish(0);
 }
 void Capture(string path)
 {
  var rt=new RenderTexture(1600,900,24);rt.Create();cam.aspect=1600f/900;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;cam.targetTexture=rt;canvas.GetComponent<CanvasScaler>().SendMessage("Handle");Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;rt.Release();Destroy(tex);Destroy(rt);canvas.renderMode=RenderMode.ScreenSpaceOverlay;
 }
}
