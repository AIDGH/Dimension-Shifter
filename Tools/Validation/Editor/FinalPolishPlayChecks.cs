using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class FinalPolishPlayChecks
{
    const string Key="FinalPolish.Checks";static bool booted;static string report="";
    static FinalPolishPlayChecks(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Boot;}
    public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");Physics2D.simulationMode=SimulationMode2D.FixedUpdate;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    public static void Visual(){EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");SessionState.SetBool(Key,true);SessionState.SetBool("FinalPolish.Visual",true);EditorApplication.isPlaying=true;}
    static void Boot(){EditorApplication.QueuePlayerLoopUpdate();if(!EditorApplication.isPlaying||booted||Time.frameCount<3)return;booted=true;Application.logMessageReceived+=Log;new GameObject("Final polish tests").AddComponent<FinalPolishRunner>().StartCoroutine(SessionState.GetBool("FinalPolish.Visual",false)?"Visual":"Check");}
    static void Log(string m,string stack,LogType type){if((type==LogType.Exception||type==LogType.Error)&&!stack.Contains("UnityEditor.Search"))Done(1,m+"\n"+stack);}
    public static void Need(bool ok,string msg){if(!ok)throw new Exception(msg);report+="PASS "+msg+"\n";File.WriteAllText("final-polish-checks.txt",report);}
    public static void Done(int code,string msg=""){SessionState.SetBool(Key,false);Application.logMessageReceived-=Log;File.WriteAllText("final-polish-checks.txt",report+msg);Time.timeScale=1;EditorApplication.Exit(code);}
}
public class FinalPolishRunner:MonoBehaviour
{
    static BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,Hidden).SetValue(o,value);
    static void Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Hidden).Invoke(o,args);
    static void Need(bool ok,string msg)=>FinalPolishPlayChecks.Need(ok,msg);
    DemoSession s;PlayerHealth hp;Rigidbody2D body;PlayerMovement move;Camera cam;Canvas canvas;
    void Place(float x,float y){body.position=new Vector2(x,y);s.transform.position=body.position;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
    void Freeze(){move.enabled=false;body.constraints=RigidbodyConstraints2D.FreezeAll;}
    GameObject Spawn(string name,float x)=>Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Encounters/"+name+".prefab"),new Vector3(x,0),Quaternion.identity);
    void Safe(){Set(hp,"invincibleRemaining",1000f);}
    IEnumerator Visual()
    {
        s=DemoSession.Instance;s.Begin();cam=Camera.main;canvas=Object.FindObjectsByType<Canvas>().First(c=>c.name=="Dimension HUD");yield return new WaitForSeconds(.6f);Capture("polish-scenery.png");
        SessionState.SetBool("FinalPolish.Visual",false);SessionState.SetBool("FinalPolish.Checks",false);EditorApplication.Exit(0);
    }
    IEnumerator Check()
    {
        s=DemoSession.Instance;hp=s.GetComponent<PlayerHealth>();body=s.GetComponent<Rigidbody2D>();move=s.GetComponent<PlayerMovement>();cam=Camera.main;canvas=Object.FindObjectsByType<Canvas>().First(c=>c.name=="Dimension HUD");s.Begin();yield return new WaitForSeconds(.6f);
        Need(Application.targetFrameRate==120&&QualitySettings.vSyncCount==0&&Mathf.Abs(Time.fixedDeltaTime-.01f)<.001f,"Responsive presentation configured for 120 FPS and 100 Hz physics");
        Need(s.GetComponent<SpriteRenderer>().sortingOrder==50,"Player renders in front of door and checkpoint artwork");Capture("polish-scenery.png");
        Object.FindAnyObjectByType<LevelGenerator>().enabled=false;Freeze();cam.GetComponent<CameraFollow>().enabled=false;
        var sanctum=Spawn("13_KeySanctum_Painted",1000);yield return null;var items=sanctum.GetComponentsInChildren<QuestInteractable>();var door=items.First(q=>q.kind==QuestInteractable.Kind.BlueDoor);var spirit=items.First(q=>q.kind==QuestInteractable.Kind.Spirit);
        s.ClaimKey();s.GetComponent<DimensionController>().SetDimension(0);Place(1030,.7f);cam.transform.position=new Vector3(1031,2.2f,-44.25f);yield return new WaitForSeconds(.5f);
        Need(canvas.transform.Find("Nearby interaction").GetComponent<CanvasGroup>().alpha==1,"Nearby door instruction appears by the doorway");Need(canvas.transform.Find("Nearby interaction").GetComponentInChildren<TMPro.TMP_Text>().text.Contains("[ E ]"),"Door prompt explicitly shows E");Capture("polish-door-prompt.png");
        Place(spirit.transform.position.x,spirit.transform.position.y);yield return null;Need(s.HasSpirit,"Walking into Spirit Bolt collects it without E");
        var ground=new GameObject("Stomp floor");ground.layer=8;ground.transform.position=new Vector3(1130,-.5f);ground.AddComponent<BoxCollider2D>().size=new Vector2(15,1);
        var mushroom=Spawn("SporeKeeper",1130);mushroom.GetComponent<EnemyAttackAI>().enabled=false;mushroom.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
        var enemy=mushroom.GetComponent<EnemyHealth>();var contact=s.GetComponent<PlayerEnemyContact>();int count=contact.StompCount;int health=enemy.CurrentHealth;
        move.enabled=true;body.constraints=RigidbodyConstraints2D.FreezeRotation;body.gravityScale=4;Place(1130,3.5f);Safe();
        for(int i=0;i<180&&contact.StompCount==count;i++)yield return new WaitForFixedUpdate();
        Need(contact.StompCount==count+1&&enemy.CurrentHealth==health-1,"Real landing with movement enabled causes one mushroom damage");
        yield return new WaitForFixedUpdate();Need(body.linearVelocity.y>7&&!move.isGrounded,"Movement controller preserves the upward mushroom bounce");
        Freeze();float y=mushroom.GetComponent<Collider2D>().bounds.max.y+s.GetComponent<Collider2D>().bounds.extents.y;Place(1130,y);yield return new WaitForSeconds(.6f);
        for(int i=0;i<6;i++)contact.TryContact(enemy);Need(enemy.CurrentHealth==health-1,"Standing/repeated contact cannot farm mushroom damage");
        move.enabled=true;body.constraints=RigidbodyConstraints2D.FreezeRotation;Place(1130,3.5f);body.linearVelocity=Vector2.down*2;
        for(int i=0;i<180&&contact.StompCount<count+2;i++)yield return new WaitForFixedUpdate();Need(contact.StompCount==count+2&&enemy.CurrentHealth==health-2,"A new jump and landing can stomp the mushroom again");Freeze();Destroy(mushroom);
        var arena=Spawn("16_WardenArena_Painted",1400);Place(1411,.7f);Safe();yield return new WaitForSeconds(.1f);var boss=arena.GetComponentInChildren<RiftWarden>();var bossHealth=boss.GetComponent<EnemyHealth>();
        Need(bossHealth.maxHealth==48,"Boss health reduced from 64 to 48");
        Call(boss,"Enter",RiftWarden.Phase.Chase,10f);Set(hp,"invincibleRemaining",0f);float hpBefore=hp.currentHealth;
        contact.TryContact(bossHealth);Need(hp.currentHealth==hpBefore,"Inactive boss body does not deal phantom contact damage");
        Call(boss,"Enter",RiftWarden.Phase.Charge,.65f);Place(boss.transform.position.x+2.7f,1);yield return new WaitForFixedUpdate();Need(hp.currentHealth==hpBefore,"Old invisible forward melee box no longer damages distant player");Safe();Place(1411,.7f);
        Set(boss,"attackChoice",1);Set(boss,"lockedDirection",-1f);Call(boss,"Enter",RiftWarden.Phase.Windup,.01f);yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();Need(boss.JumpCount>0&&boss.GetComponent<Rigidbody2D>().linearVelocity.y>5,"Jump attack really launches the boss");
        var bossBody=boss.GetComponent<Rigidbody2D>();bossBody.position+=Vector2.up*8;bossBody.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();Set(boss,"stateTime",.5f);Call(boss,"FixedUpdate");Need(boss.State==RiftWarden.Phase.Leap,"Jump apex is not mistaken for landing");
        Call(boss,"Enter",RiftWarden.Phase.BeamWindup,2f);int beams=boss.BeamCount;yield return new WaitForSeconds(1.5f);Need(boss.BeamCount==beams&&boss.State==RiftWarden.Phase.BeamWindup,"Energy lance warns before firing");
        cam.transform.position=new Vector3((boss.transform.position.x+1411)*.5f,2.3f,-44.25f);Capture("polish-boss-warning.png");yield return new WaitForSeconds(.6f);Need(boss.BeamCount==beams+1,"Energy lance fires after two-second windup");
        var seen=new System.Collections.Generic.List<int>();UnityEngine.Random.InitState(842);
        for(int i=0;i<24;i++){Call(boss,"Enter",RiftWarden.Phase.Volley,0f);yield return new WaitForFixedUpdate();seen.Add(boss.CoreDimension);}
        Need(seen.Contains(0)&&seen.Contains(1)&&seen.Skip(1).Where((v,i)=>v==seen[i]).Any(),"Random core decisions include both dimensions and consecutive repeats");
        Need(boss.SummonCount==2,"Boss summons a bounded pair of helpers from both worlds");
        var helpers=Object.FindObjectsByType<DimensionActor>(FindObjectsInactive.Include).Where(a=>a.name.StartsWith("Warden helper")).ToArray();Need(helpers.Length==2&&helpers.Select(a=>a.dimension).Distinct().Count()==2,"Summoned helpers have separate dimension membership");
        Call(boss,"ResetEncounter");yield return null;Need(Object.FindObjectsByType<WardenBeam>().Length==0&&!Object.FindObjectsByType<DimensionActor>(FindObjectsInactive.Include).Any(a=>a.name.StartsWith("Warden helper")),"Boss reset cleans beams and both-world reinforcements");
        var trialRoot=Spawn("15_ProvingGrounds_Painted",1600);Place(1612,1);yield return new WaitForSeconds(1.3f);var trial=trialRoot.GetComponentInChildren<TrialArena>();var wave=Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include).Where(e=>e.transform.parent!=null&&e.transform.parent.name.StartsWith("Trial ")).ToArray();
        Need(trial.Wave==1&&wave.Length==2&&wave.All(e=>Mathf.Abs(e.transform.position.x-body.position.x)<9),"First wave is smaller and spawns within reach");
        Need(Resources.LoadAll<AudioClip>("HollowSFX").Length>=16,"Curated supplied sound effects are imported");
        FinalPolishPlayChecks.Done(0);
    }
    void Capture(string name)
    {
        var rt=new RenderTexture(1600,900,24);rt.Create();cam.aspect=1600f/900;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;cam.targetTexture=rt;canvas.GetComponent<CanvasScaler>().SendMessage("Handle");Canvas.ForceUpdateCanvases();
        RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(name,tex.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;rt.Release();Destroy(rt);Destroy(tex);canvas.renderMode=RenderMode.ScreenSpaceOverlay;
    }
}
