using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class WardenBeamChecks
{
    static bool booted;static string report="";
    static WardenBeamChecks(){if(SessionState.GetBool("BeamChecks",false))EditorApplication.update+=Boot;}
    public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");Physics2D.simulationMode=SimulationMode2D.FixedUpdate;SessionState.SetBool("BeamChecks",true);EditorApplication.isPlaying=true;}
    static void Boot(){EditorApplication.QueuePlayerLoopUpdate();if(!EditorApplication.isPlaying||booted||Time.frameCount<3)return;booted=true;Application.logMessageReceived+=Log;new GameObject("Beam checks").AddComponent<BeamCheckRunner>().StartCoroutine("Run");}
    static void Log(string m,string s,LogType t){if((t==LogType.Error||t==LogType.Exception)&&!s.Contains("UnityEditor.Search"))Done(1,m+"\n"+s);}
    public static void Need(bool value,string message){if(!value)throw new Exception(message);report+="PASS "+message+"\n";}
    public static void Done(int code,string message=""){SessionState.SetBool("BeamChecks",false);Application.logMessageReceived-=Log;File.WriteAllText("warden-beam-checks.txt",report+message);EditorApplication.Exit(code);}
}
public class BeamCheckRunner:MonoBehaviour
{
    RiftWarden boss;PlayerHealth hp;PlayerEnemyContact contact;
    static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
    WardenBeam Shot(float height=20){var b=new GameObject("Test lance").AddComponent<WardenBeam>();b.Launch(new Vector2(2195,height),Vector2.right,boss.beamMaterial,boss);return b;}
    IEnumerator Run()
    {
        var session=DemoSession.Instance;session.Begin();Object.FindAnyObjectByType<LevelGenerator>().enabled=false;
        hp=session.GetComponent<PlayerHealth>();contact=session.GetComponent<PlayerEnemyContact>();session.GetComponent<PlayerMovement>().enabled=false;
        var rb=session.GetComponent<Rigidbody2D>();rb.constraints=RigidbodyConstraints2D.FreezeAll;rb.position=new Vector2(2200,20);session.transform.position=rb.position;Physics2D.SyncTransforms();
        var arena=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Encounters/16_WardenArena_Painted.prefab"),new Vector3(2000,0),Quaternion.identity);yield return null;
        boss=arena.GetComponentInChildren<RiftWarden>();boss.enabled=false;boss.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
        typeof(RiftWarden).GetMethod("Enter",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(boss,new object[]{RiftWarden.Phase.Chase,100f});
        Set(hp,"invincibleRemaining",0f);float before=hp.currentHealth;
        var wall=new GameObject("Beam shield");wall.layer=8;wall.transform.position=new Vector3(2197,20);wall.AddComponent<BoxCollider2D>().size=new Vector2(.4f,5);Physics2D.SyncTransforms();
        Shot();yield return new WaitForSeconds(.5f);WardenBeamChecks.Need(hp.currentHealth==before&&Object.FindObjectsByType<WardenBeam>().Length==0,"Terrain absorbs energy lance before player");
        Destroy(wall);yield return null;Shot(22);yield return new WaitForSeconds(.6f);WardenBeamChecks.Need(hp.currentHealth==before,"Lance passing above visible player does not deal phantom damage");
        foreach(var b in Object.FindObjectsByType<WardenBeam>())Destroy(b.gameObject);yield return null;
        Shot();yield return new WaitForSeconds(.5f);WardenBeamChecks.Need(hp.currentHealth==before-22,"Visible direct lance hit deals exactly 22 damage once");
        Set(hp,"invincibleRemaining",0f);contact.BeginDash();before=hp.currentHealth;Shot();yield return new WaitForSeconds(.5f);WardenBeamChecks.Need(hp.currentHealth==before,"Phase dash protects against lance damage");contact.EndDash();
        var inactive=Shot();typeof(RiftWarden).GetMethod("Enter",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(boss,new object[]{RiftWarden.Phase.Dormant,0f});yield return new WaitForFixedUpdate();yield return null;WardenBeamChecks.Need(inactive==null,"Lance disappears when encounter is inactive");
        WardenBeamChecks.Done(0);
    }
}
