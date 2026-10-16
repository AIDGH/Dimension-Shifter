using System;using System.Collections.Generic;using System.IO;using System.Reflection;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class CombatRegressionChecks
{
 static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static readonly List<string> results=new List<string>();static int failures;
 static PlayerAttack player;static EnemyHealth enemy;
 static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Private).Invoke(o,args);
 static void Set(object o,string field,object value)=>o.GetType().GetField(field,Private).SetValue(o,value);
 static void Need(bool value,string message){if(!value)throw new Exception(message);}
 static void Setup(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);Physics2D.simulationMode=SimulationMode2D.Script;var p=new GameObject("Player");p.AddComponent<Rigidbody2D>().gravityScale=0;p.AddComponent<CapsuleCollider2D>();player=p.AddComponent<PlayerAttack>();Call(player,"Awake");enemy=MakeEnemy(new Vector2(.9f,0));Physics2D.SyncTransforms();}
 static EnemyHealth MakeEnemy(Vector2 position){var go=new GameObject("Slime");go.layer=10;go.transform.position=position;go.AddComponent<Rigidbody2D>().gravityScale=0;go.AddComponent<BoxCollider2D>().size=new Vector2(.6f,.6f);var patrol=go.AddComponent<EnemyPatrol>();Call(patrol,"Awake");var e=go.AddComponent<EnemyHealth>();Call(e,"Awake");return e;}
 static void Strike(int damage=1){Call(player,"BeginAttack");Set(player,"attackDamage",damage);Set(player,"attackElapsed",.1f);Call(player,"FixedUpdate");}
 static void Check(string name,Action run){try{Setup();run();results.Add("PASS "+name);}catch(Exception e){failures++;results.Add("FAIL "+name+": "+e.GetBaseException().Message);}}
 public static void Run(){
 Check("normal strike removes exactly one HP",()=>{Strike();Need(enemy.CurrentHealth==2,"Wrong damage");});
 Check("dash strike removes two HP",()=>{Strike(2);Need(enemy.CurrentHealth==1,"Wrong dash damage");});
 Check("same swing cannot damage twice even with multiple colliders",()=>{enemy.gameObject.AddComponent<CircleCollider2D>();Physics2D.SyncTransforms();Strike();for(int i=0;i<15;i++)Call(player,"FixedUpdate");Need(enemy.CurrentHealth==2,"Repeated hit");});
 Check("new swing can hit same enemy",()=>{Strike();Strike();Need(enemy.CurrentHealth==1,"New attack ignored");});
 Check("windup and recovery have no hitbox",()=>{Call(player,"BeginAttack");Call(player,"FixedUpdate");Need(enemy.CurrentHealth==3,"Windup damaged");Set(player,"attackElapsed",.55f);Call(player,"FixedUpdate");Need(enemy.CurrentHealth==3,"Recovery damaged");});
 Check("facing direction excludes enemies behind player",()=>{player.transform.localScale=new Vector3(-1,1,1);Strike();Need(enemy.CurrentHealth==3,"Hit behind");enemy.transform.position=new Vector3(-.9f,0);Physics2D.SyncTransforms();Strike();Need(enemy.CurrentHealth==2,"Left swing missed");});
 Check("out-of-range enemy unaffected",()=>{enemy.transform.position=Vector3.right*3;Physics2D.SyncTransforms();Strike();Need(enemy.CurrentHealth==3,"Too much reach");});
 Check("wall blocks blade",()=>{var wall=new GameObject("Wall");wall.layer=8;wall.transform.position=new Vector3(.5f,0);wall.AddComponent<BoxCollider2D>().size=new Vector2(.1f,3);Physics2D.SyncTransforms();Strike();Need(enemy.CurrentHealth==3,"Hit through wall");});
 Check("one swing hits each distinct target once",()=>{var second=MakeEnemy(new Vector2(.95f,.35f));Physics2D.SyncTransforms();Strike();Need(enemy.CurrentHealth==2&&second.CurrentHealth==2,"Missed distinct target");});
 Check("hurt suspends patrol, applies knockback, then resumes",()=>{Strike();Need(!enemy.GetComponent<EnemyPatrol>().enabled&&enemy.IsStunned,"No stun");Need(enemy.GetComponent<Rigidbody2D>().linearVelocity.x>0,"Wrong knockback");Call(enemy,"Tick",.4f);Need(enemy.GetComponent<EnemyPatrol>().enabled&&!enemy.IsStunned,"No resume");});
 Check("death is immediate, disables every collider and rejects later damage",()=>{enemy.TakeDamage(3,Vector2.left);Need(enemy.IsDead&&!enemy.GetComponent<Rigidbody2D>().simulated&&!enemy.GetComponent<EnemyPatrol>().enabled,"Death still moves");foreach(var c in enemy.GetComponentsInChildren<Collider2D>())Need(!c.enabled,"Ghost collider");Need(!enemy.TakeDamage(1,Vector2.left)&&enemy.CurrentHealth==0,"Damage accepted after death");});
 Check("disabling attack cancels its damage window",()=>{Call(player,"BeginAttack");Call(player,"OnDisable");Call(player,"FixedUpdate");Need(enemy.CurrentHealth==3,"Disabled attack damaged");});
 Check("both playable scenes have wired combat",()=>{foreach(var path in new[]{"Assets/Scenes/SampleScene.unity","Assets/Scenes/PaintedDimension.unity"}){EditorSceneManager.OpenScene(path);var health=UnityEngine.Object.FindAnyObjectByType<EnemyHealth>();Need(health!=null,"Missing health "+path);Need(health.GetComponent<SpriteRenderer>().sharedMaterial.shader.name=="DimensionShifter/EnemyFlash","Missing flash shader");var attack=UnityEngine.Object.FindAnyObjectByType<PlayerAttack>();Need(attack.normalAttackDuration>.66f,"Wrong animation duration");}});
 File.WriteAllLines("combat-results.txt",results);EditorApplication.Exit(failures==0?0:1);
 }
}
