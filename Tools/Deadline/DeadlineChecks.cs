using System;using System.IO;using System.Linq;using System.Collections;using System.Reflection;using UnityEngine;using UnityEngine.UI;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
[InitializeOnLoad]public static class DeadlineChecks
{
 static bool boot;static string report="";static double began;
 static DeadlineChecks(){if(SessionState.GetBool("DeadlineChecks",false))EditorApplication.update+=Boot;}
 public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");SessionState.SetBool("DeadlineChecks",true);Physics2D.simulationMode=SimulationMode2D.FixedUpdate;EditorApplication.isPlaying=true;}
 static void Boot(){EditorApplication.QueuePlayerLoopUpdate();if(!EditorApplication.isPlaying||boot||Time.frameCount<4)return;boot=true;began=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;new GameObject("Deadline regression checks").AddComponent<DeadlineRunner>().StartCoroutine("Run");}
 static void Log(string m,string st,LogType t){if((t==LogType.Error||t==LogType.Exception)&&!st.Contains("UnityEditor.Search"))Done(1,m+st);}
 public static void Need(bool b,string m){if(!b)throw new Exception(m);report+="PASS "+m+"\n";File.WriteAllText("deadline-checks.txt",report);}
 public static void Done(int code,string m=""){SessionState.SetBool("DeadlineChecks",false);File.WriteAllText("deadline-checks.txt",report+m);EditorApplication.Exit(code);}
}
public class DeadlineRunner:MonoBehaviour
{
 DemoSession s;PlayerMovement motor;Rigidbody2D rb;PlayerHealth hp;RunProgress run;
 static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Need(bool b,string m)=>DeadlineChecks.Need(b,m);
 void Set(object obj,string field,object value){obj.GetType().GetField(field,Flags).SetValue(obj,value);}
 void Call(object obj,string method,params object[] values){obj.GetType().GetMethod(method,Flags).Invoke(obj,values);}
 void Place(Vector2 p){rb.position=p;rb.linearVelocity=Vector2.zero;s.transform.position=p;Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToPlayer();}
 IEnumerator Run(){
 s=DemoSession.Instance;motor=s.GetComponent<PlayerMovement>();rb=s.GetComponent<Rigidbody2D>();hp=s.GetComponent<PlayerHealth>();run=s.GetComponent<RunProgress>();
 yield return new WaitForSecondsRealtime(.9f);Need(Object.FindAnyObjectByType<StoryIntro>()!=null&&!s.Started,"Skippable prologue exists before gameplay");Capture("deadline-prologue.png");s.GetComponent<StoryIntro>().Dismiss();
 Need(!motor.useAcceleration,"Immediate movement enabled by default; acceleration remains available");var skin=s.GetComponent<CharacterAppearance>();var shape=s.GetComponent<Collider2D>();var scale=s.transform.localScale;var size=((CapsuleCollider2D)shape).size;
 Need(skin.useKnight&&!motor.spriteFacesRight,"Main demo begins with Knight artwork and correct facing");skin.Toggle();Need(!skin.useKnight&&motor.spriteFacesRight&&AssetDatabase.GetAssetPath(s.GetComponent<SpriteRenderer>().sprite).Contains("Red"),"H toggle restores original Red");skin.Toggle();Need(skin.useKnight&&s.transform.localScale==scale&&((CapsuleCollider2D)shape).size==size,"Skin toggle preserves transform and collision size");
 float idle=((TextureImporter)AssetImporter.GetAtPath("Assets/Hollow/enemies/husk_hornhead/Idle.png")).spritePixelsPerUnit;
 Need(Mathf.Approximately(idle,((TextureImporter)AssetImporter.GetAtPath("Assets/Hollow/enemies/husk_hornhead/Charge.png")).spritePixelsPerUnit),"Hornhead dash and idle use the same pixel scale");
 s.Begin();yield return new WaitForSeconds(.5f);var gen=Object.FindAnyObjectByType<LevelGenerator>();Need(gen.LoadAuthoredRoute(),"Complete authored route loads");yield return null;yield return new WaitForSeconds(.3f);
 var extra=Object.FindObjectsByType<HollowEnemy>(FindObjectsInactive.Include);File.WriteAllText("deadline-enemy-inventory.txt",string.Join("\n",extra.Select(e=>e.transform.root.name+" / "+e.transform.parent.name)));Need(extra.Length>=6&&extra.Select(e=>e.transform.root.name).Distinct().Count()>=4,"New enemies populate at least four route sections ("+extra.Length+" enemies)");
 var canopy=Object.FindObjectsByType<Grid>().First(g=>g.name.StartsWith("17_RiftCanopy"));Need(canopy!=null,"Climb-and-descent encounter is in the actual generated route");
 // Drive the actual movement controller through the climb using real collision simulation.
 foreach(var e in Object.FindObjectsByType<HollowEnemy>())e.enabled=false;
 motor.enabled=false;Physics2D.simulationMode=SimulationMode2D.Script;
 Place((Vector2)canopy.transform.position+new Vector2(1,.65f));
 for(int i=0;i<20;i++){Set(motor,"moveInput",0f);Call(motor,"FixedUpdate");Physics2D.Simulate(1f/60);}
 foreach(var target in new[]{new Vector2(5.5f,2),new Vector2(9.5f,4),new Vector2(14.5f,8)}){
   Vector2 absolute=(Vector2)canopy.transform.position+target;Set(motor,"jumpBufferRemaining",.12f);bool airJump=false,landed=false;
   for(int i=0;i<180;i++){Set(motor,"moveInput",Mathf.Abs(rb.position.x-absolute.x)<.12f?0f:Mathf.Sign(absolute.x-rb.position.x));if(i>10&&!airJump&&rb.linearVelocity.y<1){Set(motor,"jumpBufferRemaining",.12f);airJump=true;}Call(motor,"FixedUpdate");Physics2D.Simulate(1f/60);if(i>15&&motor.isGrounded&&Mathf.Abs(rb.position.x-absolute.x)<.6f&&Mathf.Abs(shape.bounds.min.y-absolute.y)<.2f){landed=true;break;}}
   Need(landed,"Real movement reaches canopy ledge at height "+target.y);
 }
 Physics2D.simulationMode=SimulationMode2D.FixedUpdate;motor.enabled=true;hp.RestoreHealth();yield return new WaitForSeconds(.2f);Capture("deadline-canopy.png");
 // Compact arena, real wave state machine, and gates after all three waves.
 s.ClaimKey();s.ClaimSpirit();s.ActivateRelay(0);s.ActivateRelay(1);
 var trial=Object.FindAnyObjectByType<TrialArena>();var trialGate=trial.transform.parent.GetComponentsInChildren<QuestGate>().First(g=>g.requirement==QuestGate.Requirement.Trial);
 Need(trial.width==30&&Mathf.Abs(trialGate.transform.position.x-(trial.transform.position.x+30))<.1f,"Right wave boundary is now 30 units from entrance");
 Place((Vector2)trial.transform.position+new Vector2(10,.7f));yield return new WaitForSeconds(1.3f);Need(trial.Wave==1&&trial.Active&&trialGate.barrier.enabled,"Wave one starts with right boundary closed");
 for(int wave=1;wave<=3;wave++){
  Need(trial.Wave==wave,"Wave "+wave+" begins");var list=(System.Collections.Generic.List<GameObject>)typeof(TrialArena).GetField("enemies",Flags).GetValue(trial);
  if(wave>=2)Need(list.Any(g=>g.GetComponentInChildren<HollowEnemy>(true)!=null),"Wave "+wave+" includes new enemy types");
  foreach(var anchor in list.ToArray()){var enemy=anchor.GetComponentInChildren<EnemyHealth>(true);int world=anchor.GetComponent<DimensionActor>().dimension;s.GetComponent<DimensionController>().SetDimension(world);enemy.TakeDamage(999,s.transform.position,0);}
  yield return new WaitForSeconds(1.2f);hp.RestoreHealth();
 }
 Need(s.TrialCleared&&!trialGate.barrier.enabled&&!trialGate.glow.enabled,"All three waves clear and right boundary disappears");
 // End cannot be bypassed even with teleport, missing key, or a phase dash.
 var tree=Object.FindAnyObjectByType<FinalTreeGate>();Need(tree!=null&&tree.barrier.enabled&&!tree.finalRift.activeSelf,"Tree gate blocks the final rift before the Warden");
 Place(tree.transform.position-Vector3.right);Need(!tree.TryOpen(),"Tree refuses interaction without Warden sigil");run.Complete();Need(!run.Completed,"Final completion rejects a pre-boss bypass");
 // Kill exposed boss above ground; retain physics and settle corpse before it fades.
 var boss=Object.FindAnyObjectByType<RiftWarden>();s.GetComponent<DimensionController>().SetDimension(boss.CoreDimension);Place((Vector2)boss.transform.position+new Vector2(-4,.8f));
 Call(boss,"Enter",RiftWarden.Phase.Exposed,10f);boss.transform.position+=Vector3.up*2;boss.GetComponent<Rigidbody2D>().position=boss.transform.position;Physics2D.SyncTransforms();
 var bh=boss.GetComponent<EnemyHealth>();Need(bh.TakeDamage(999,s.transform.position,0),"Exposed Warden accepts a finishing blow");Need(bh.IsDead&&boss.GetComponent<Rigidbody2D>().simulated,"Dead Warden retains ground physics");
 yield return new WaitForSeconds(.8f);Need(s.BossDefeated&&s.HasWardenKey,"Boss defeat awards the Warden sigil");var ground=Physics2D.Raycast(boss.transform.position+Vector3.up*.1f,Vector2.down,1,1<<8);Need(ground.collider!=null&&ground.distance<.2f,"Boss corpse settles onto the arena floor");Capture("deadline-boss-defeat.png");
 Place(tree.transform.position-Vector3.right);Need(tree.TryOpen()&&s.TreeOpened&&!tree.barrier.enabled&&tree.finalRift.activeSelf,"Warden sigil opens tree and reveals distinct home rift");Need(Vector2.Distance(rb.position,tree.farSide.position)<.1f,"Tree doorway moves player safely beyond its roots");yield return new WaitForSeconds(.1f);Capture("deadline-tree-exit.png");
 run.Respawn();Need(s.TreeOpened&&s.HasWardenKey,"Respawn preserves final-tree progress");Place(tree.finalRift.transform.position);yield return new WaitForSeconds(.2f);Need(run.Completed,"Entering unlocked final rift completes the run");
 DeadlineChecks.Done(0);
 }
 void Capture(string name){var cam=Camera.main;cam.GetComponent<CameraFollow>().SnapToPlayer();var canvases=Object.FindObjectsByType<Canvas>();foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=1;}var rt=new RenderTexture(1600,900,24);rt.Create();cam.aspect=1600f/900;cam.targetTexture=rt;Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var t=new Texture2D(1600,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1600,900),0,0);t.Apply();File.WriteAllBytes(name,t.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;rt.Release();Object.Destroy(rt);Object.Destroy(t);foreach(var c in canvases)c.renderMode=RenderMode.ScreenSpaceOverlay;}
}
