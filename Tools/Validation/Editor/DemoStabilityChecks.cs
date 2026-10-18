using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class DemoStabilityChecks
{
    static bool booted;static string report="";
    static DemoStabilityChecks(){if(SessionState.GetBool("StabilityChecks",false))EditorApplication.update+=Boot;}
    public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");Physics2D.simulationMode=SimulationMode2D.FixedUpdate;SessionState.SetBool("StabilityChecks",true);EditorApplication.isPlaying=true;}
    static void Boot(){EditorApplication.QueuePlayerLoopUpdate();if(!EditorApplication.isPlaying||booted||Time.frameCount<3)return;booted=true;Application.logMessageReceived+=Log;new GameObject("Stability checks").AddComponent<StabilityRunner>().StartCoroutine("Run");}
    static void Log(string m,string s,LogType t){if((t==LogType.Error||t==LogType.Exception)&&!s.Contains("UnityEditor.Search"))Done(1,m+"\n"+s);}
    public static void Need(bool ok,string message){if(!ok)throw new Exception(message);report+="PASS "+message+"\n";File.WriteAllText("stability-checks.txt",report);}
    public static void Done(int code,string message=""){SessionState.SetBool("StabilityChecks",false);Application.logMessageReceived-=Log;File.WriteAllText("stability-checks.txt",report+message);EditorApplication.Exit(code);}
}
public class StabilityRunner:MonoBehaviour
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,Hidden).SetValue(o,value);
    static void Call(object o,string method)=>o.GetType().GetMethod(method,Hidden).Invoke(o,null);
    static void Need(bool ok,string message)=>DemoStabilityChecks.Need(ok,message);
    IEnumerator Run()
    {
        var s=DemoSession.Instance;s.Begin();Object.FindAnyObjectByType<LevelGenerator>().enabled=false;
        Need(Application.targetFrameRate==60&&Mathf.Abs(Time.fixedDeltaTime-1f/60f)<.0001f&&Time.maximumDeltaTime<=.051f,"60 Hz budget limits physics catch-up to three ticks");
        Need(File.ReadAllText("ProjectSettings/ProjectSettings.asset").Contains("metalUseMetalDisplayLink: 1"),"macOS native display pacing enabled in build settings");
        var rb=s.GetComponent<Rigidbody2D>();var movement=s.GetComponent<PlayerMovement>();movement.enabled=false;rb.constraints=RigidbodyConstraints2D.FreezeAll;
        foreach(string suffix in new[]{"","_Painted"})
        {
            var arena=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Encounters/15_ProvingGrounds"+suffix+".prefab"),new Vector3(2000,0),Quaternion.identity);yield return null;
            var trial=arena.GetComponentInChildren<TrialArena>();trial.enabled=false;
            foreach(float x in new[]{trial.transform.position.x+5,trial.transform.position.x+trial.width-5,trial.transform.position.x+trial.width*.5f})
            {
                Call(trial,"ResetTrial");yield return null;
                rb.position=new Vector2(x,.7f);s.transform.position=rb.position;Physics2D.SyncTransforms();
                Call(trial,"Update");Set(trial,"<Wave>k__BackingField",1);Set(trial,"nextWave",0f);Call(trial,"Update");
                var enemies=Object.FindObjectsByType<DimensionActor>(FindObjectsInactive.Include).Where(a=>a.name.StartsWith("Trial ")).ToArray();
                Need(enemies.Length==3,"Wave two spawns all three enemies "+suffix+" x="+x+" count="+enemies.Length+" wave="+trial.Wave+" active="+trial.Active+" hp="+s.GetComponent<PlayerHealth>().currentHealth+" origin="+trial.transform.position+" width="+trial.width);
                Need(enemies.All(a=>Mathf.Abs(a.transform.position.x-x)>=2.79f),"Spawns keep clear of player "+suffix+" x="+x);
                for(int i=0;i<enemies.Length;i++)for(int j=i+1;j<enemies.Length;j++)Need(Mathf.Abs(enemies[i].transform.position.x-enemies[j].transform.position.x)>=2.99f,"Reserved wave slots cannot overlap "+suffix+" pair="+i+","+j+" x="+x);
            }
            Call(trial,"ResetTrial");Destroy(arena);yield return null;
        }
        var audio=Object.FindAnyObjectByType<GameAudio>();
        foreach(string cue in new[]{"jump","dash","hurt","cast","land","swing","step_concrete","step_wood","step_snow","slime_death","shift"})
        {
            foreach(var channel in audio.GetComponents<AudioSource>())channel.Stop();GameAudio.Play(cue,.7f);
            var voice=audio.GetComponents<AudioSource>().FirstOrDefault(a=>a.isPlaying);
            Need(voice!=null&&AssetDatabase.GetAssetPath(voice.clip).Contains("Resources/HollowSFX/"),"Actual audio voice uses supplied clip for "+cue);
        }
        Need(GameAudio.MusicMix<1,"Music makes room for foreground sound effects");
        int traps=0;
        foreach(var path in Directory.GetFiles("Assets/Prefabs/Encounters","*_Painted.prefab"))
        {
            var root=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),new Vector3(3000,0),Quaternion.identity);Physics2D.SyncTransforms();
            foreach(var trap in root.GetComponentsInChildren<TrapDamage>())
            {
                traps++;var p=trap.transform.position;var hit=Physics2D.Raycast(new Vector2(p.x,p.y+.5f),Vector2.down,3,1<<8);
                Need(hit.collider!=null&&Mathf.Abs(p.y-hit.point.y-.59f)<.02f,"Ice base above floor: "+path+" / "+trap.name);
            }
            Destroy(root);yield return null;
        }
        Need(traps>=8,"At least eight authored ice traps per painted route");
        // Render the new beam alongside a solid reference, using the actual barrier material.
        var ownerGO=new GameObject("Visual test owner");ownerGO.AddComponent<EnemyHealth>();ownerGO.AddComponent<Rigidbody2D>();ownerGO.AddComponent<BoxCollider2D>();var owner=ownerGO.AddComponent<RiftWarden>();owner.enabled=false;
        var beam=new GameObject("Lightning preview").AddComponent<WardenBeam>();beam.Launch(new Vector2(4000,0),Vector2.right,AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Polish/Rift.mat"),owner);beam.enabled=false;
        Need(beam.GetComponent<MeshRenderer>().sharedMaterial.shader.name=="DimensionShifter/LivingRift"&&beam.GetComponent<LineRenderer>()==null,"Boss shot uses living barrier filaments instead of gradient ribbon");
        var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.transform.position=new Vector3(4000,0,-10);cam.orthographicSize=1.5f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.025f,.045f,.085f);cam.cullingMask=1;
        var rt=new RenderTexture(1200,600,24);rt.Create();cam.targetTexture=rt;cam.aspect=2;RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var tex=new Texture2D(1200,600,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1200,600),0,0);tex.Apply();File.WriteAllBytes("lightning-preview.png",tex.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;rt.Release();Destroy(rt);Destroy(tex);
        DemoStabilityChecks.Done(0);
    }
}
