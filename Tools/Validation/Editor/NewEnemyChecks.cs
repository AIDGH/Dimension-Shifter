using System;using System.IO;using System.Linq;using System.Collections;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
[InitializeOnLoad]public static class NewEnemyChecks
{
 static bool boot;static string report="";
 static NewEnemyChecks(){if(SessionState.GetBool("NewEnemyChecks",false))EditorApplication.update+=Boot;}
 public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");SessionState.SetBool("NewEnemyChecks",true);Physics2D.simulationMode=SimulationMode2D.FixedUpdate;EditorApplication.isPlaying=true;}
 static void Boot(){EditorApplication.QueuePlayerLoopUpdate();if(!EditorApplication.isPlaying||boot||Time.frameCount<3)return;boot=true;Application.logMessageReceived+=Log;new GameObject("New enemy checks").AddComponent<NewEnemyRunner>().StartCoroutine("Run");}
 static void Log(string m,string st,LogType t){if((t==LogType.Error||t==LogType.Exception)&&!st.Contains("UnityEditor.Search"))Done(1,m+st);}
 public static void Need(bool b,string m){if(!b)throw new Exception(m);report+="PASS "+m+"\n";File.WriteAllText("new-enemies-checks.txt",report);}
 public static void Done(int code,string m=""){SessionState.SetBool("NewEnemyChecks",false);File.WriteAllText("new-enemies-checks.txt",report+m);EditorApplication.Exit(code);}
}
public class NewEnemyRunner:MonoBehaviour
{
 static void Need(bool b,string m)=>NewEnemyChecks.Need(b,m);
 IEnumerator Run(){var s=DemoSession.Instance;s.Begin();Need(Object.FindAnyObjectByType<LevelGenerator>().LoadAuthoredRoute(),"Authored demo route loads");yield return null;
 var all=Object.FindObjectsByType<HollowEnemy>(FindObjectsInactive.Include);Need(all.Length==2,"Exactly two new enemies in the demo");var fly=all.First(a=>a.flying);var horn=all.First(a=>!a.flying);var dim=s.GetComponent<DimensionController>();var rb=s.GetComponent<Rigidbody2D>();s.GetComponent<PlayerMovement>().enabled=false;rb.constraints=RigidbodyConstraints2D.FreezeAll;
 dim.SetDimension(1);Need(!fly.gameObject.activeSelf&&horn.gameObject.activeSelf,"Each new enemy belongs to a different dimension");rb.position=(Vector2)horn.transform.position+Vector2.right*3;Physics2D.SyncTransforms();yield return new WaitForSeconds(.25f);Need(horn.State==HollowEnemy.Mode.Warning&&horn.warning.enabled,"Hornhead displays a warning before charging");yield return new WaitForSeconds(.6f);Need(horn.Attacks>0,"Hornhead starts a charge after the warning");yield return new WaitForSeconds(1.1f);Need(horn.State==HollowEnemy.Mode.Recover,"Hornhead pauses after its charge");
 var hh=horn.GetComponent<EnemyHealth>();Need(!hh.HealthBarVisible,"New enemy health bar starts hidden");hh.TakeDamage(1,rb.position,0);Need(hh.HealthBarVisible,"Damage reveals the enemy health bar");int hp=hh.CurrentHealth;dim.SetDimension(0);dim.SetDimension(1);Need(hh.CurrentHealth==hp,"Enemy damage persists after changing worlds");
 // A committed charge must stop on real terrain rather than tunnelling through a wall.
 var wall=new GameObject("Test wall");wall.layer=8;wall.transform.position=horn.transform.position+Vector3.left;wall.AddComponent<BoxCollider2D>().size=new Vector2(.2f,4);rb.position=(Vector2)horn.transform.position+Vector2.left*4;Physics2D.SyncTransforms();float boundary=wall.transform.position.x;yield return new WaitForSeconds(2.5f);Need(horn.transform.position.x>boundary,"Hornhead does not cross a solid wall");Object.Destroy(wall);
 dim.SetDimension(0);rb.position=(Vector2)fly.transform.position+Vector2.right*3;Physics2D.SyncTransforms();yield return new WaitForSeconds(.3f);Need(fly.State==HollowEnemy.Mode.Warning,"Mossfly warns before pursuing");yield return new WaitForSeconds(.6f);Need(fly.Attacks>0&&fly.GetComponent<Rigidbody2D>().gravityScale==0,"Mossfly pursues without falling");
 fly.enabled=false;fly.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;rb.position+=(Vector2.up*20);Physics2D.SyncTransforms();var fh=fly.GetComponent<EnemyHealth>();var contact=s.GetComponent<PlayerEnemyContact>();var health=s.GetComponent<PlayerHealth>();health.RestoreHealth();yield return new WaitForSeconds(2.1f);contact.BeginDash();Need(!contact.TryContact(fh),"Phase dash protects from new enemy contact");contact.EndDash();yield return new WaitForSeconds(.3f);Need(contact.TryContact(fh),"Unprotected contact causes damage");float remaining=health.currentHealth;Need(!contact.TryContact(fh)&&health.currentHealth==remaining,"Contact invulnerability prevents repeated damage");
 fh.TakeDamage(100,rb.position,0);Need(fh.IsDead&&!fh.GetComponent<Collider2D>().enabled,"New enemy death disables contact");yield return new WaitForSeconds(.8f);Need(fh==null,"Defeated new enemy is removed");NewEnemyChecks.Done(0);}
}
