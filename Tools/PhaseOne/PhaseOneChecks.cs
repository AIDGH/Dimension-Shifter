using System;using System.Collections;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEngine.UI;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using Object=UnityEngine.Object;
[InitializeOnLoad]public static class PhaseOneChecks
{
 static bool running;static string report="";
 static PhaseOneChecks(){if(SessionState.GetBool("PhaseOneChecks",false))EditorApplication.update+=Tick;}
 public static void Run(){SessionState.SetBool("PhaseOneChecks",true);EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");EditorApplication.isPlaying=true;}
 static void Tick(){EditorApplication.QueuePlayerLoopUpdate();if(!EditorApplication.isPlaying||running||Time.frameCount<4)return;running=true;Application.logMessageReceived+=Log;new GameObject("Phase One checks").AddComponent<PhaseOneRunner>().StartCoroutine("Run");}
 static void Log(string m,string st,LogType t){if((t==LogType.Error||t==LogType.Exception)&&!st.Contains("UnityEditor.Search"))Finish(1,m+st);}
 public static void Need(bool ok,string m){if(!ok)throw new Exception(m);report+="PASS "+m+"\n";File.WriteAllText("phase1-checks.txt",report);}
 public static void Finish(int code,string extra=""){File.WriteAllText("phase1-checks.txt",report+extra);SessionState.SetBool("PhaseOneChecks",false);EditorApplication.Exit(code);}
}
public class PhaseOneRunner:MonoBehaviour
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;DemoSession s;Rigidbody2D rb;PlayerMovement motor;
 void Set(object obj,string name,object v)=>obj.GetType().GetField(name,F).SetValue(obj,v);
 void Call(object obj,string name)=>obj.GetType().GetMethod(name,F).Invoke(obj,null);
 void Place(Vector2 p){rb.position=p;rb.linearVelocity=Vector2.zero;s.transform.position=p;Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToPlayer();}
 static void Need(bool b,string m)=>PhaseOneChecks.Need(b,m);
 IEnumerator Run(){
 s=DemoSession.Instance;rb=s.GetComponent<Rigidbody2D>();motor=s.GetComponent<PlayerMovement>();yield return new WaitForSecondsRealtime(.8f);
 Need(!PlayerSettings.macRetinaSupport&&!PlayerSettings.defaultIsNativeResolution,"Mac build uses a bounded pixel budget rather than native Retina resolution");
 Need(QualitySettings.vSyncCount>=1&&Application.targetFrameRate==-1&&UnityEngine.Rendering.OnDemandRendering.renderFrameInterval==1,"Display-synchronized presentation without software 60 FPS pacing or skipped renders");
 Need(Screen.currentResolution.refreshRateRatio.value < 110 || Screen.currentResolution.refreshRateRatio.value > 121 || QualitySettings.vSyncCount == 1,"Fractional 120 Hz refresh never rounds up to half-rate VSync");
 Need(Time.fixedDeltaTime<=1f/60+.0001f,"Physics responds at 60-120 Hz according to display refresh");
 var intro=s.GetComponent<StoryIntro>();var bg=GameObject.Find("Blue violet forest backdrop").GetComponent<Image>();Need(bg.sprite!=null,"Blue-violet forest covers the story background");
 var pic=GameObject.Find("Story illustration").GetComponent<RectTransform>();var caption=GameObject.Find("Story caption panel").GetComponent<RectTransform>();var words=GameObject.Find("Story paragraph").GetComponent<Text>();
 for(int i=0;i<3;i++){
 Set(intro,"page",i);Call(intro,"Show");yield return new WaitForSecondsRealtime(.8f);Canvas.ForceUpdateCanvases();var a=new Vector3[4];var b=new Vector3[4];pic.GetWorldCorners(a);caption.GetWorldCorners(b);
 Need(a[0].y>b[1].y,"Story page "+(i+1)+" illustration stays above its separate caption");Need(words.preferredHeight<=words.rectTransform.rect.height,"Story page "+(i+1)+" paragraph fits without clipping");Capture("phase1-story-"+(i+1)+".png");}
 intro.Dismiss();s.Begin();yield return null;var gen=Object.FindAnyObjectByType<LevelGenerator>();gen.LoadAuthoredRoute();yield return new WaitForSeconds(.25f);
 var gate=Object.FindAnyObjectByType<FinalTreeGate>();var tree=gate.transform.root.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Sanctuary canopy");
 Need(Mathf.Abs(tree.transform.localPosition.x-31.5f)<.05f,"Ancient tree moved two tiles behind its former position");Need(gate.transform.position.x<tree.transform.position.x-2,"Door is reached before the tree trunk");
 for(float x=28.5f;x<=38;x+=.5f){float wx=gate.transform.root.position.x+x;var h=Physics2D.Raycast(new Vector2(wx,gate.transform.root.position.y+16),Vector2.down,25,1<<8);Need(h.collider!=null&&Mathf.Abs(h.point.y-(gate.transform.root.position.y+3))<.12f,"Ground under final approach is clear of invisible trunk at "+x);}
 float floor=gate.transform.root.position.y+3;Need(Mathf.Abs(gate.door.bounds.min.y-floor)<.08f,"Door base rests on the final ground");
 Place((Vector2)gate.transform.position+new Vector2(-2,.65f));yield return new WaitForSeconds(.5f);Need(!gate.TryOpen()&&gate.barrier.enabled&&!gate.finalRift.activeSelf,"Door remains locked without Warden sigil");Capture("phase1-tree-locked.png");
 s.GetComponent<RunProgress>().Complete();Need(!s.GetComponent<RunProgress>().Completed,"Final rift cannot complete an unopened route");
 s.BossDefeated=true;s.ClaimWardenKey();Need(gate.TryOpen()&&!gate.barrier.enabled&&gate.finalRift.activeSelf,"Sigil opens the grounded door and reveals home rift");
 Need(rb.position.x>tree.transform.position.x&&Mathf.Abs(rb.position.y-(floor+.65f))<.08f,"Passage lands safely beyond roots on the same floor");
 yield return new WaitForSeconds(.3f);Capture("phase1-tree-open.png");
 Need(Physics2D.OverlapBox(rb.position,new Vector2(.45f,.7f),0,1<<8)==null,"Door destination has no solid overlap");
 foreach(var knight in new[]{true,false}){s.GetComponent<CharacterAppearance>().Apply(knight);motor.useAcceleration=false;motor.enabled=false;Physics2D.simulationMode=SimulationMode2D.Script;
 foreach(float step in new[]{1f/60,1f/120}){Time.fixedDeltaTime=step;Place(new Vector2(gate.transform.root.position.x+33.1f,floor+.65f));Set(motor,"moveInput",0f);for(int n=0;n<8;n++){Call(motor,"FixedUpdate");Physics2D.Simulate(step);}Set(motor,"moveInput",1f);Call(motor,"FixedUpdate");Physics2D.Simulate(step);Need(rb.linearVelocity.x>motor.moveSpeed*.9f,(knight?"Knight":"Red")+" begins movement on the next physics tick at "+Mathf.RoundToInt(1/step)+" Hz");Set(motor,"moveInput",0f);Call(motor,"FixedUpdate");Physics2D.Simulate(step);Need(Mathf.Abs(rb.linearVelocity.x)<.05f,(knight?"Knight":"Red")+" stops without acceleration delay at "+Mathf.RoundToInt(1/step)+" Hz");}}
 Physics2D.simulationMode=SimulationMode2D.FixedUpdate;motor.enabled=true;DesktopPresentation.Configure();Place(gate.finalRift.transform.position);yield return new WaitForSeconds(.2f);Need(s.GetComponent<RunProgress>().Completed,"Home rift is reachable and completes after tree unlock");PhaseOneChecks.Finish(0);
 }
 void Capture(string name){var cam=Camera.main;cam.GetComponent<CameraFollow>().SnapToPlayer();var all=Object.FindObjectsByType<Canvas>();foreach(var c in all){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=1;}var rt=new RenderTexture(1600,900,24);rt.Create();cam.aspect=1600f/900;cam.targetTexture=rt;Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var t=new Texture2D(1600,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1600,900),0,0);t.Apply();File.WriteAllBytes(name,t.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;rt.Release();Object.Destroy(rt);Object.Destroy(t);foreach(var c in all)c.renderMode=RenderMode.ScreenSpaceOverlay;}
}
