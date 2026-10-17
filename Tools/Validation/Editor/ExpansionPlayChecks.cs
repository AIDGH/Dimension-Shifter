using System;using System.Collections;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class ExpansionPlayChecks
{
 const string Key="DimensionShifter.ExpansionChecks";
 static bool started;public static string Report="";
 static ExpansionPlayChecks(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Boot;}
 public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");Object.FindFirstObjectByType<LevelGenerator>().enabled=false;foreach(var h in Object.FindObjectsByType<EnemyHealth>())h.gameObject.SetActive(false);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 static void Boot(){EditorApplication.QueuePlayerLoopUpdate();if(!EditorApplication.isPlaying||started||Time.frameCount<3)return;started=true;File.WriteAllText("expansion-play-results.txt","STARTED\n");Physics2D.simulationMode=SimulationMode2D.FixedUpdate;var g=new GameObject("Expansion play regression");Application.logMessageReceived+=Log;var runner=g.AddComponent<ExpansionCheckRunner>();runner.StartCoroutine(runner.RunChecks());}
 static void Log(string message,string trace,LogType type){if((type==LogType.Exception || type==LogType.Error) && !trace.Contains("UnityEditor.Search"))Finish(1,"Unity error: "+message+"\n"+trace);}
 public static void Need(bool value,string label){if(!value)throw new Exception(label);Report+="PASS "+label+"\n";File.WriteAllText("expansion-play-results.txt",Report);}
 public static void Finish(int code,string text=""){SessionState.SetBool(Key,false);EditorApplication.update-=Boot;Application.logMessageReceived-=Log;File.WriteAllText("expansion-play-results.txt",Report+text);EditorApplication.Exit(code);}
}
public class ExpansionCheckRunner:MonoBehaviour
{
 PlayerHealth hp;Rigidbody2D body;PlayerMovement movement;DimensionController dimension;RunProgress run;
 GameObject Spawn(string name,Vector3 p){return Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Encounters/"+name+".prefab"),p,Quaternion.identity);}
 void Place(float x,float y=.65f){body.position=new Vector2(x,y);hp.transform.position=new Vector3(x,y,0);body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
 static void Need(bool b,string s)=>ExpansionPlayChecks.Need(b,s);
 public IEnumerator RunChecks()
 {
  hp=Object.FindFirstObjectByType<PlayerHealth>();body=hp.GetComponent<Rigidbody2D>();movement=hp.GetComponent<PlayerMovement>();dimension=hp.GetComponent<DimensionController>();run=hp.GetComponent<RunProgress>();
  movement.enabled=false;Camera.main.GetComponent<CameraFollow>().enabled=false;body.constraints=RigidbodyConstraints2D.FreezeAll;body.gravityScale=0;
  var rift=Spawn("09_RiftCrossing_Painted",new Vector3(1000,0));Place(1006);yield return new WaitForFixedUpdate();yield return null;
  var gates=rift.GetComponentsInChildren<DimensionMember>().Where(m=>m.name.Contains("seal")).ToArray();var frost=gates.First(m=>m.dimension==0);var echo=gates.First(m=>m.dimension==1);
  Need(frost.IsPresent&&!echo.IsPresent,"Frost starts with the cyan gate solid and violet gate ghosted");
  Need(dimension.TryShift(),"Q can change dimension in clear space");
  Need(!frost.IsPresent&&echo.IsPresent&&frost.GetComponentInChildren<Collider2D>().isTrigger,"Shift changes collision as well as appearance");
  Place(1008,1);yield return new WaitForSeconds(2.1f);
  Need(!dimension.TryShift()&&dimension.Current==1,"Shift is refused if a new solid gate would trap the player");
  Place(1006);Need(dimension.TryShift(),"Leaving the ghost gate permits shifting again");
  Need(rift.GetComponentsInChildren<TrailMarker>().Any(m=>m.kind==TrailMarker.MarkerKind.Checkpoint),"Rift encounter includes a safe checkpoint");
  Destroy(rift);
  var floor=new GameObject("Test floor");floor.layer=8;floor.transform.position=new Vector3(2005,-.5f);floor.AddComponent<BoxCollider2D>().size=new Vector2(40,1);
  Place(2000);yield return new WaitForSeconds(2.1f);
  var shot=Spawn("SporeProjectile",new Vector3(1998,.65f));shot.GetComponent<EnemyProjectile>().Launch(Vector2.right,null);yield return new WaitForSeconds(.6f);
  Need(hp.currentHealth==85,"A real moving spore hits for 15 HP");
  hp.RestoreHealth();yield return new WaitForSeconds(2.1f);var contact=hp.GetComponent<PlayerEnemyContact>();contact.BeginDash();
  shot=Spawn("SporeProjectile",new Vector3(1998,.65f));shot.GetComponent<EnemyProjectile>().Launch(Vector2.right,null);yield return new WaitForSeconds(.6f);contact.EndDash();
  Need(hp.currentHealth==100,"Phase dash passes through an incoming spore without damage");
  var wall=new GameObject("Cover");wall.layer=9;wall.transform.position=new Vector3(1999,.8f);wall.AddComponent<BoxCollider2D>().size=new Vector2(.4f,1.6f);yield return new WaitForSeconds(.3f);
  shot=Spawn("SporeProjectile",new Vector3(1997,.65f));shot.GetComponent<EnemyProjectile>().Launch(Vector2.right,null);yield return new WaitForSeconds(.8f);
  Need(hp.currentHealth==100&&shot==null,"Stone cover intercepts a spore before it reaches the player");Destroy(wall);
  var ghost=new GameObject("Ghost combat barrier");ghost.layer=9;ghost.transform.position=new Vector3(2001,.8f);ghost.AddComponent<BoxCollider2D>().size=new Vector2(.1f,1.6f);var member=ghost.AddComponent<DimensionMember>();member.dimension=1;member.Apply(0);
  var target=Spawn("PouncingSlime",new Vector3(2002,.5f));target.GetComponent<EnemyAttackAI>().enabled=false;target.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;Place(2000.2f);hp.GetComponent<PlayerAttack>().StartCoroutine("NormalAttackRoutine");yield return new WaitForSeconds(.8f);
  Need(target.GetComponent<EnemyHealth>().CurrentHealth==2,"Normal strikes pass through ghost barriers");Destroy(target);Destroy(ghost);Place(2000);
  var leap=Spawn("PouncingSlime",new Vector3(2004,.5f));var ai=leap.GetComponent<EnemyAttackAI>();yield return new WaitForSeconds(1.25f);
  Need(ai.IsWindingUp&&ai.AttackCount==0,"Slime visibly warns before its first leap");yield return new WaitForSeconds(.55f);
  Need(ai.AttackCount==1&&leap.GetComponent<Rigidbody2D>().linearVelocity.y>0,"Slime commits to an actual physics jump after warning");Destroy(leap);
  var mushroom=Spawn("SporeKeeper",new Vector3(2005,.05f));var shooter=mushroom.GetComponent<EnemyAttackAI>();yield return new WaitForSeconds(1.2f);
  Need(shooter.IsWindingUp,"Mushroom telegraphs an aimed shot");mushroom.GetComponent<EnemyHealth>().TakeDamage(1,hp.transform.position,0);yield return new WaitForSeconds(.8f);
  Need(shooter.AttackCount==0,"Hitting the mushroom cancels the prepared shot");yield return new WaitForSeconds(3.2f);
  Need(shooter.AttackCount>=1,"Mushroom recovers and fires after its cooldown");Destroy(mushroom);
  foreach(var p in Object.FindObjectsByType<EnemyProjectile>())Destroy(p.gameObject);
  var pass=Spawn("08_SporePass_Painted",new Vector3(3000,0));foreach(var e in pass.GetComponentsInChildren<EnemyHealth>())e.gameObject.SetActive(false);
  var trap=pass.GetComponentInChildren<TimedHazard>();Place(3010,.65f);hp.RestoreHealth();yield return new WaitForSeconds(2.1f);
  float initial=hp.currentHealth;float until=Time.time+4;bool warned=false,active=false;while(Time.time<until){warned|=trap.IsWarning;active|=trap.IsActive;yield return null;}
  Need(warned&&active&&hp.currentHealth<initial,"Pulse trap cycles through warning and active damage, including standing inside it");
  Place(3002,.75f);run.SetCheckpoint(new Vector3(3002,.75f),"Regression checkpoint");yield return new WaitForSeconds(2.1f);Place(3018);hp.TryTakeDamage(1000);Need(hp.IsDead,"Lethal damage stops the player");run.Respawn();movement.enabled=false;
  Need(hp.currentHealth==100&&Vector3.Distance(hp.transform.position,run.CheckpointPosition)<.01f&&!hp.IsDead,"Respawn restores health and returns to the saved checkpoint");
  Need(hp.GetComponent<PlayerAttack>().enabled&&contact.enabled,"Respawn restores attack and enemy collision handling");
  run.Complete();Need(run.Completed&&!movement.enabled&&!hp.GetComponent<PlayerAttack>().enabled,"Exit completes the demo and stops player input");
  ExpansionPlayChecks.Finish(0);
 }
}
