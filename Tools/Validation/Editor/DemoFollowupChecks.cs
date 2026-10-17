using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class DemoFollowupChecks
{
    const string Key="Demo.FollowupChecks";static bool booted;static string report="";
    static DemoFollowupChecks(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Boot;}
    public static void Run(){SlimeFootAlignment.Run();EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");Physics2D.simulationMode=SimulationMode2D.FixedUpdate;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    public static void Preview(){EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");SessionState.SetBool(Key,true);SessionState.SetBool("Demo.FollowupPreview",true);EditorApplication.isPlaying=true;}
    static void Boot(){EditorApplication.QueuePlayerLoopUpdate();if(!EditorApplication.isPlaying||booted||Time.frameCount<3)return;booted=true;Application.logMessageReceived+=Log;new GameObject("Followup checks").AddComponent<FollowupRunner>().StartCoroutine(SessionState.GetBool("Demo.FollowupPreview",false)?"Preview":"Check");}
    static void Log(string message,string stack,LogType type){if((type==LogType.Error||type==LogType.Exception)&&!stack.Contains("UnityEditor.Search"))Finish(1,message+"\n"+stack);}
    public static void Need(bool value,string message){if(!value)throw new Exception(message);report+="PASS "+message+"\n";}
    public static void Finish(int code,string message=""){SessionState.SetBool(Key,false);Application.logMessageReceived-=Log;File.WriteAllText("demo-followup-results.txt",report+message);Time.timeScale=1;AudioListener.pause=false;EditorApplication.Exit(code);}
}
public class FollowupRunner:MonoBehaviour
{
    static void Need(bool value,string message)=>DemoFollowupChecks.Need(value,message);
    IEnumerator Preview()
    {
        DemoSession.Instance.Begin();yield return new WaitForSeconds(.2f);DemoSession.Instance.SetPaused(true);yield return null;
        Capture();SessionState.SetBool("Demo.FollowupPreview",false);SessionState.SetBool("Demo.FollowupChecks",false);EditorApplication.Exit(0);
    }
    void Capture()
    {
        var cam=Camera.main;var canvas=Object.FindObjectsByType<Canvas>().First(c=>c.name=="Dimension HUD");
        var rt=new RenderTexture(1600,900,24);rt.Create();cam.aspect=1600f/900;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;cam.targetTexture=rt;
        canvas.GetComponent<CanvasScaler>().SendMessage("Handle");Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
        RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes("demo-pause-preview.png",tex.EncodeToPNG());
        RenderTexture.active=null;cam.targetTexture=null;rt.Release();Destroy(tex);Destroy(rt);canvas.renderMode=RenderMode.ScreenSpaceOverlay;
    }
    IEnumerator Check()
    {
        DontDestroyOnLoad(gameObject);
        var session=DemoSession.Instance;var run=session.GetComponent<RunProgress>();var body=session.GetComponent<Rigidbody2D>();
        session.SetPaused(true);Need(!session.Paused&&!session.Started,"Escape pause is unavailable at title");
        session.Begin();yield return new WaitForSeconds(.2f);session.ClaimSpirit();
        var anim=session.GetComponent<Animator>();var weapon=session.GetComponent<SpiritWeapon>();
        Need(weapon.Fire(),"Unlocked spirit cast fires");
        yield return new WaitForSeconds(.15f);
        Need(!anim.GetCurrentAnimatorStateInfo(0).IsName("Player_Attack")&&!anim.GetNextAnimatorStateInfo(0).IsName("Player_Attack"),"Spirit cast does not enter sword animation");
        var duration=(float)typeof(RunProgress).GetField("messageUntil",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run)-Time.time;
        Need(duration>8,"Long instructions stay visible beyond eight seconds");
        CombatFeedback.Hit(session.transform.position,false);session.SetPaused(true);
        float time=session.Elapsed,cooldown=weapon.CooldownRemaining;var position=body.position;
        yield return new WaitForSecondsRealtime(.3f);
        Need(session.Paused&&Time.timeScale==0&&AudioListener.pause,"Hit-stop cannot release menu pause; audio pauses");
        Need(session.Elapsed==time&&body.position==position&&weapon.CooldownRemaining==cooldown,"Pause freezes timer, body and cooldown");
        Need(!weapon.Fire()&&!session.GetComponent<DimensionController>().TryShift(),"Cast and dimension change rejected while paused");
        var interaction=new GameObject("Nearby altar").AddComponent<QuestInteractable>();interaction.transform.position=session.transform.position;interaction.kind=QuestInteractable.Kind.Spirit;
        Need(!interaction.Interact(session),"Interaction rejected while paused");Destroy(interaction.gameObject);
        var canvas=Object.FindObjectsByType<Canvas>().First(c=>c.name=="Dimension HUD");
        var menu=canvas.transform.Find("Pause menu").GetComponent<CanvasGroup>();
        Need(menu.alpha==1&&menu.blocksRaycasts&&menu.interactable,"Pause card is visible and captures pointer input");
        Capture();yield return new WaitForSecondsRealtime(.2f);
        var resume=menu.GetComponentsInChildren<Button>().First(b=>b.name=="Resume journey");
        ExecuteEvents.Execute(resume.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        yield return new WaitForSeconds(.1f);Need(!session.Paused&&Time.timeScale==1&&!AudioListener.pause&&session.Elapsed>time,"Resume button restores gameplay, audio and timer");
        // Pausing an active dash must preserve its coroutine and restore movement normally afterward.
        var attack=session.GetComponent<PlayerAttack>();attack.StartCoroutine("DashAttackRoutine");yield return new WaitForFixedUpdate();
        session.SetPaused(true);yield return new WaitForSecondsRealtime(.1f);Need(attack.IsDashing,"Pause preserves active dash state");
        session.SetPaused(false);yield return new WaitForSeconds(.6f);Need(!attack.IsDashing&&session.GetComponent<PlayerMovement>().enabled&&body.gravityScale>0,"Resumed dash finishes and restores gravity/movement");
        var floor=new GameObject("Foot test floor");floor.layer=8;floor.transform.position=new Vector3(1000,-.5f);floor.AddComponent<BoxCollider2D>().size=new Vector2(10,1);
        var slime=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Encounters/PouncingSlime.prefab"),new Vector3(1000,2),Quaternion.identity);slime.GetComponent<EnemyAttackAI>().enabled=false;
        yield return new WaitForSeconds(1);
        float foot=slime.transform.TransformPoint(new Vector3(0,SlimeFootAlignment.Foot)).y;
        Need(Mathf.Abs(foot)<.025f,"Settled slime visible feet meet floor within 0.025 units");
        foreach(string path in Directory.GetFiles("Assets/Sprites/Enemies/Slime/Individual Sprites","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);bool readable=importer.isReadable;importer.isReadable=true;importer.SaveAndReimport();
            var sprite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();var pixels=sprite.texture.GetPixels32();var rect=sprite.rect;int bottom=(int)rect.yMax;
            for(int y=(int)rect.y;y<rect.yMax;y++)for(int x=(int)rect.x;x<rect.xMax;x++)if(pixels[y*sprite.texture.width+x].a>80){bottom=Mathf.Min(bottom,y);break;}
            Need(Mathf.Abs((bottom-rect.y-sprite.pivot.y)/sprite.pixelsPerUnit-SlimeFootAlignment.Foot)<.001f,"Consistent animated foot: "+Path.GetFileName(path));
            importer.isReadable=readable;importer.SaveAndReimport();
        }
        session.SetPaused(true);yield return null;
        var title=menu.GetComponentsInChildren<Button>().First(b=>b.name=="Return to title");
        ExecuteEvents.Execute(title.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        yield return new WaitForSecondsRealtime(.6f);
        Need(DemoSession.Instance!=session&&!DemoSession.Instance.Started&&!DemoSession.Instance.Paused&&!AudioListener.pause&&Time.timeScale==0,"Return to title resets run and pause without leaking audio state");
        DemoFollowupChecks.Finish(0);
    }
}
