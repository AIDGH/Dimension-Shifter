using System;using System.Collections;using System.IO;using System.Linq;using UnityEngine;using UnityEngine.UI;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class PolishPlayChecks
{
 const string Key="DimensionShifter.PolishChecks";static bool started;static string report="";
 static PolishPlayChecks(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Boot;}
 public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");Object.FindAnyObjectByType<LevelGenerator>().enabled=false;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 static void Boot(){EditorApplication.QueuePlayerLoopUpdate();if(!EditorApplication.isPlaying||started||Time.frameCount<3)return;started=true;Physics2D.simulationMode=SimulationMode2D.FixedUpdate;Application.logMessageReceived+=Log;var runner=new GameObject("Polish checks").AddComponent<PolishCheckRunner>();runner.StartCoroutine(runner.Check());}
 static void Log(string s,string trace,LogType type){if((type==LogType.Error||type==LogType.Exception)&&!trace.Contains("UnityEditor.Search"))Finish(1,s+"\n"+trace);}
 public static void Need(bool ok,string message){if(!ok)throw new Exception(message);report+="PASS "+message+"\n";File.WriteAllText("polish-play-results.txt",report);}
 public static void Finish(int code,string text=""){SessionState.SetBool(Key,false);EditorApplication.update-=Boot;Application.logMessageReceived-=Log;File.WriteAllText("polish-play-results.txt",report+text);EditorApplication.Exit(code);}
}
public class PolishCheckRunner:MonoBehaviour
{
 PlayerHealth hp;Rigidbody2D rb;DimensionController dimension;
 GameObject Spawn(string name,Vector3 pos)=>Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Encounters/"+name+".prefab"),pos,Quaternion.identity);
 void Place(float x,float y){rb.position=new Vector2(x,y);hp.transform.position=new Vector3(x,y);rb.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
 static void Need(bool ok,string text)=>PolishPlayChecks.Need(ok,text);
 public IEnumerator Check()
 {
  hp=Object.FindAnyObjectByType<PlayerHealth>();rb=hp.GetComponent<Rigidbody2D>();dimension=hp.GetComponent<DimensionController>();var rewards=hp.GetComponent<PlayerRewards>();hp.GetComponent<PlayerMovement>().enabled=false;Camera.main.GetComponent<CameraFollow>().enabled=false;rb.constraints=RigidbodyConstraints2D.FreezeAll;rb.gravityScale=0;
  var garden=Spawn("07_LeapGarden_Painted",new Vector3(1000,0));Place(1003,.65f);yield return new WaitForSeconds(.3f);
  var actors=garden.GetComponentsInChildren<DimensionActor>();Need(actors.Any(a=>a.dimension==0)&&actors.Any(a=>a.dimension==1),"Both dimensions have distinct enemy populations");
  Need(actors.Where(a=>a.dimension==0).All(a=>a.content.activeSelf)&&actors.Where(a=>a.dimension==1).All(a=>!a.content.activeSelf),"Only Frost inhabitants are active initially");
  var slime=actors.First(a=>a.dimension==0).content.GetComponent<EnemyHealth>();slime.TakeDamage(1,hp.transform.position,0);int remaining=slime.CurrentHealth;
  var shot=Spawn("SporeProjectile",new Vector3(1018,1));shot.GetComponent<EnemyProjectile>().Launch(Vector2.left,null);
  Need(dimension.TryShift(),"Shift starts in open space");Need(!dimension.TryShift()&&dimension.CooldownRemaining>1.8f,"Two-second cooldown prevents immediate repeated shifts");yield return null;
  Need(shot==null&&actors.Where(a=>a.dimension==0).All(a=>!a.content.activeSelf)&&actors.Where(a=>a.dimension==1).All(a=>a.content.activeSelf),"Shift hides old enemies and removes their projectiles");
  yield return new WaitForSeconds(2.1f);Need(dimension.TryShift(),"Shift becomes available after its cooldown");Need(slime.CurrentHealth==remaining,"An enemy keeps its damage when its dimension returns");
  int score=rewards.Score;slime.TakeDamage(100,hp.transform.position);Need(rewards.Score==score+100&&rewards.Defeated==1,"Defeating an enemy grants one score and XP reward");slime.TakeDamage(100,hp.transform.position);Need(rewards.Score==score+100,"A dying enemy cannot award duplicate points");
  var pickup=garden.GetComponentInChildren<ExperiencePickup>();int xp=rewards.Experience;Need(pickup.TryCollect(rewards)&&rewards.Experience==xp+pickup.experience,"Relic pickup grants XP");Need(!pickup.TryCollect(rewards),"A collected relic cannot be farmed again after respawn");
  var checkpoint=garden.GetComponentsInChildren<TrailMarker>().First(m=>m.kind==TrailMarker.MarkerKind.Checkpoint);Need(checkpoint.GetComponent<ShrineVisual>()!=null&&checkpoint.GetComponent<ShrineVisual>().shrine.sprite.name=="CheckpointShrine","Checkpoint uses the generated blue ring, with no plant beacon");
  checkpoint.GetComponent<ShrineVisual>().Activate();Need(checkpoint.GetComponent<ShrineVisual>().Activated,"Checkpoint activation produces persistent lit state");
  var ui=Object.FindObjectsByType<Canvas>().First(c=>c.name=="Dimension HUD");Need(ui.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("RESONANCE")),"HUD displays vitality, resonance and cooldowns");
  var music=hp.GetComponent<WorldMusic>().GetComponents<AudioSource>().Where(a=>a.loop).ToArray();Need(music.Length==2&&music.All(a=>a.clip!=null&&Mathf.Abs(a.clip.length-32)<.05f),"Both dimensions load matching 32-second looping music");
  var rift=Spawn("09_RiftCrossing_Painted",new Vector3(1040,0));yield return null;
  foreach(var gate in rift.GetComponentsInChildren<DimensionMember>().Where(m=>m.name.Contains("seal")))Need(gate.GetComponentsInChildren<SpriteRenderer>().Length==0&&gate.GetComponentsInChildren<MeshRenderer>().Any(r=>r.sharedMaterial.shader.name=="DimensionShifter/LivingRift"),"Barrier uses a flowing energy seam without plant sprites");
  var ascent=Spawn("10_CrystalAscent_Painted",new Vector3(1080,0));var descent=Spawn("11_HollowDescent_Painted",new Vector3(1120,6));yield return null;Physics2D.SyncTransforms();
  Need(ascent.transform.Find("EndPosition").localPosition.y==6&&descent.transform.Find("EndPosition").localPosition.y==-6,"Vertical chunks rise and descend six units");
  foreach(var root in new[]{ascent,descent})for(int i=0;i<18;i++){float expected=root.transform.position.y+(root==ascent?1:-1)*Mathf.Min(3,i/4)*2;var hit=Physics2D.Raycast(new Vector2(root.transform.position.x+i+.5f,expected+.05f),Vector2.down,1,1<<8);Need(hit.collider!=null&&Mathf.Abs(hit.point.y-expected)<.02f,"Vertical floor sample "+root.name+" x="+i);}
  // Preview real Play Mode objects and the same HUD at two aspect ratios.
  var camera=Camera.main;camera.orthographic=true;camera.orthographicSize=5.4f;camera.transform.position=new Vector3(1008,2,camera.transform.position.z);Place(1004,.65f);yield return new WaitForSeconds(.2f);
  File.WriteAllText("polish-render-debug.txt","Camera "+camera.transform.position+" player "+hp.transform.position+" viewport "+camera.WorldToViewportPoint(hp.transform.position)+"\n"+string.Join("\n",garden.GetComponentsInChildren<Renderer>().Select(r=>r.name+" enabled="+r.enabled+" bounds="+r.bounds+" order="+r.sortingOrder)));
  Capture("polish-frost.png",1600,900,ui);dimension.SetDimension(1);hp.GetComponent<RunProgress>().ShowMessage("ECHO  •  New inhabitants awaken beyond the veil");yield return new WaitForSeconds(.4f);Capture("polish-echo.png",1600,900,ui);
  camera.transform.position=new Vector3(1048,2,camera.transform.position.z);Place(1044,.65f);yield return new WaitForSeconds(.2f);Capture("polish-rift.png",1280,800,ui);
  camera.transform.position=new Vector3(1089,6,camera.transform.position.z);Place(1092,6.65f);yield return new WaitForSeconds(.2f);Capture("polish-ascent.png",1600,900,ui);
  camera.transform.position=new Vector3(1129,-2,camera.transform.position.z);Place(1132,.65f);yield return new WaitForSeconds(.2f);Capture("polish-descent.png",1600,900,ui);
  Need(camera.clearFlags==CameraClearFlags.SolidColor&&camera.backgroundColor.b<.12f,"Deep background covers the viewport after upward and downward camera travel");
  PolishPlayChecks.Finish(0);
 }
 void Capture(string path,int width,int height,Canvas ui)
 {
  var camera=Camera.main;camera.aspect=(float)width/height;ui.renderMode=RenderMode.ScreenSpaceCamera;ui.worldCamera=camera;ui.planeDistance=1;
  var rt=new RenderTexture(width,height,24);rt.Create();camera.targetTexture=rt;ui.GetComponent<CanvasScaler>().SendMessage("Handle");Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;
  var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;rt.Release();Destroy(image);Destroy(rt);ui.renderMode=RenderMode.ScreenSpaceOverlay;
 }
}
