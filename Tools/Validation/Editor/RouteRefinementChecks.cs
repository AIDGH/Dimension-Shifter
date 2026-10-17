using System;using System.Collections;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class RouteRefinementChecks
{
 const string Key="DimensionShifter.RouteRefinement";static bool started;static string report="";
 static RouteRefinementChecks(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Boot;}
 public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");Physics2D.simulationMode=SimulationMode2D.FixedUpdate;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 static void Boot(){EditorApplication.QueuePlayerLoopUpdate();if(!EditorApplication.isPlaying||started||Time.frameCount<3)return;started=true;Application.logMessageReceived+=Log;new GameObject("Route refinement checks").AddComponent<RouteRefinementRunner>().StartCoroutine("Check");}
 static void Log(string text,string stack,LogType type){if((type==LogType.Exception||type==LogType.Error)&&!stack.Contains("UnityEditor.Search"))Finish(1,text+"\n"+stack);}
 public static void Need(bool ok,string text){if(!ok)throw new Exception(text);report+="PASS "+text+"\n";File.WriteAllText("route-refinement-results.txt",report);}
 public static void Finish(int code,string message=""){SessionState.SetBool(Key,false);EditorApplication.update-=Boot;Application.logMessageReceived-=Log;File.WriteAllText("route-refinement-results.txt",report+message);EditorApplication.Exit(code);}
}
public class RouteRefinementRunner:MonoBehaviour
{
 PlayerHealth hp;Rigidbody2D body;DimensionController dimension;Camera cameraView;Canvas ui;
 GameObject Spawn(string name,Vector3 p)=>Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Encounters/"+name+".prefab"),p,Quaternion.identity);
 void Place(float x,float y){body.position=new Vector2(x,y);hp.transform.position=new Vector3(x,y);body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
 static void Need(bool ok,string text)=>RouteRefinementChecks.Need(ok,text);
 IEnumerator Check()
 {
  hp=Object.FindAnyObjectByType<PlayerHealth>();body=hp.GetComponent<Rigidbody2D>();dimension=hp.GetComponent<DimensionController>();cameraView=Camera.main;
  hp.GetComponent<PlayerMovement>().enabled=false;body.constraints=RigidbodyConstraints2D.FreezeAll;hp.GetComponent<Collider2D>().enabled=false;
  var slime=Object.FindAnyObjectByType<EnemyPatrol>();var slimeBody=slime.GetComponent<Rigidbody2D>();var slimeShape=slime.GetComponent<Collider2D>();var initialFloor=Physics2D.Raycast(slime.transform.position,Vector2.down,20,1<<8);
  Need(slimeShape.enabled&&!slimeShape.isTrigger,"The actual scene's introductory slime has an enabled solid collider");
  Need(initialFloor.collider!=null,"The generated starting route has terrain below the introductory slime");
  for(int i=0;i<20;i++){yield return new WaitForSeconds(.4f);Need(slimeBody.position.y>initialFloor.point.y-.5f,"Intro slime stays above the map, sample "+i);}
  var foot=Physics2D.Raycast(new Vector2(slimeShape.bounds.center.x,slimeShape.bounds.min.y+.03f),Vector2.down,.2f,1<<8);
  Need(foot.collider!=null,"Intro slime finishes its patrol supported by real scene terrain");
  Object.FindAnyObjectByType<LevelGenerator>().enabled=false;slime.gameObject.SetActive(false);hp.GetComponent<Collider2D>().enabled=true;cameraView.GetComponent<CameraFollow>().enabled=false;
  var crossing=Spawn("09_RiftCrossing_Painted",new Vector3(1000,0));var vault=Spawn("12_RiftVault_Painted",new Vector3(1050,0));yield return null;
  var seals=crossing.GetComponentsInChildren<DimensionMember>();var walls=seals.Where(s=>s.name=="Frost seal"||s.name=="Echo seal").OrderBy(s=>s.transform.position.x).ToArray();
  Need(walls.Length==2&&walls[1].transform.position.x-walls[0].transform.position.x>=20,"Crossing walls leave at least twenty units between switches");
  Need(walls.All(w=>w.GetComponentInChildren<Collider2D>().bounds.max.y>=39),"Vertical seals extend beyond the player's reachable camera view");
  var shell=vault.GetComponentsInChildren<DimensionMember>().First(s=>s.name=="Echo vault seal");Need(shell.GetComponentsInChildren<Collider2D>().Length==3,"Relic chamber has two walls and a horizontal roof");
  dimension.SetDimension(1);Place(1021,5.5f);body.constraints=RigidbodyConstraints2D.FreezeRotation;body.gravityScale=4;yield return new WaitForSeconds(.8f);
  Need(Mathf.Abs(hp.GetComponent<Collider2D>().bounds.min.y-3.825f)<.08f,"Horizontal Echo membrane physically supports the player");
  dimension.SetDimension(0);yield return new WaitForSeconds(.8f);
  Need(hp.GetComponent<Collider2D>().bounds.min.y<.1f,"Changing to Frost lets the player fall through the horizontal membrane");
  body.constraints=RigidbodyConstraints2D.FreezeAll;body.gravityScale=0;
  var shrine=crossing.GetComponentInChildren<ShrineVisual>();dimension.SetDimension(1);yield return null;
  var block=new MaterialPropertyBlock();shrine.shrine.GetPropertyBlock(block);
  Need(shrine.shrine.sharedMaterial.shader.name=="DimensionShifter/Sanctuary"&&block.GetFloat("_EchoAmount")==1,"The shrine enables violet recoloring in Echo");
  dimension.SetDimension(0);yield return null;shrine.shrine.GetPropertyBlock(block);Need(block.GetFloat("_EchoAmount")==0,"The shrine returns to its original blue in Frost");
  foreach(string suffix in new[]{"","_Painted"})foreach(bool down in new[]{false,true})
  {
   var root=Spawn((down?"11_HollowDescent":"10_CrystalAscent")+suffix,new Vector3(1200,0));yield return null;Physics2D.SyncTransforms();
   for(int x=0;x<12;x++)
   {
    float expected=-4+(down?-1:1)*(x+.5f)*.5f;
    var hit=Physics2D.Raycast(new Vector2(1200+x+.5f,expected-1),Vector2.up,1.2f,1<<8);
    Need(hit.collider!=null&&Mathf.Abs(hit.point.y-expected)<.02f,"Continuous sloped underside "+root.name+" x="+x);
   }
   Destroy(root);yield return null;
  }
  ui=Object.FindObjectsByType<Canvas>().First(c=>c.name=="Dimension HUD");
  Place(1004,.65f);cameraView.transform.position=new Vector3(1007,2,-44.25f);dimension.SetDimension(0);yield return new WaitForSeconds(.3f);Capture("refinement-blue-shrine.png");
  dimension.SetDimension(1);yield return new WaitForSeconds(.4f);Capture("refinement-violet-shrine.png");
  cameraView.transform.position=new Vector3(1070,2,-44.25f);Place(1064,.65f);yield return new WaitForSeconds(.3f);Capture("refinement-vault.png");
  var ascent=Spawn("10_CrystalAscent_Painted",new Vector3(1120,0));Place(1126,2.65f);cameraView.transform.position=new Vector3(1128,2,-44.25f);yield return new WaitForSeconds(.3f);Capture("refinement-underside.png");
  hp.GetComponent<RunProgress>().SetCheckpoint(new Vector3(1004,.65f),"Rift crossing");typeof(PlayerHealth).GetField("invincibleRemaining",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(hp,0f);
  hp.TryTakeDamage(10000);yield return new WaitForSeconds(.5f);var hud=hp.GetComponent<GameHUD>();Need(hud.OutcomeVisible,"Death opens the full outcome menu");Capture("refinement-death-menu.png");
  var primary=ui.GetComponentsInChildren<Button>().First(b=>b.name=="Return to sanctuary");
  var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};ExecuteEvents.Execute(primary.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return null;
  Need(!hp.IsDead&&Vector2.Distance(body.position,new Vector2(1004,.65f))<.1f,"Clicking Return to sanctuary restores the player at the checkpoint");
  hp.GetComponent<PlayerMovement>().enabled=false;body.constraints=RigidbodyConstraints2D.FreezeAll;yield return new WaitForSeconds(.5f);Need(!hud.OutcomeVisible,"Outcome menu closes after checkpoint return");
  cameraView.transform.position=new Vector3(1007,2,-44.25f);hp.GetComponent<RunProgress>().Complete();yield return new WaitForSeconds(.5f);Need(hud.OutcomeVisible&&primary.GetComponentInChildren<TMPro.TMP_Text>().text.Contains("JOURNEY AGAIN"),"Victory offers a new journey and the run's results");Capture("refinement-victory-menu.png");
  DontDestroyOnLoad(gameObject);ExecuteEvents.Execute(primary.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return new WaitForSeconds(.6f);
  var fresh=Object.FindAnyObjectByType<PlayerHealth>();Need(fresh!=null&&!fresh.IsDead&&fresh.currentHealth==fresh.maxHealth&&!fresh.GetComponent<RunProgress>().Completed&&fresh.GetComponent<PlayerRewards>().Score==0,"Clicking Journey Again reloads a healthy new run with reset score");
  RouteRefinementChecks.Finish(0);
 }
 void Capture(string path)
 {
  const int width=1600,height=900;cameraView.aspect=(float)width/height;ui.renderMode=RenderMode.ScreenSpaceCamera;ui.worldCamera=cameraView;ui.planeDistance=1;
  var rt=new RenderTexture(width,height,24);rt.Create();cameraView.targetTexture=rt;ui.GetComponent<CanvasScaler>().SendMessage("Handle");Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(cameraView,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;
  var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=null;cameraView.targetTexture=null;rt.Release();Destroy(image);Destroy(rt);ui.renderMode=RenderMode.ScreenSpaceOverlay;
 }
}
