using UnityEngine;
public class HollowEnemy:MonoBehaviour
{
 public bool flying;public Sprite[] idle,move,charge;public SpriteRenderer visual,warning;
 public float speed=2.4f;public int Attacks {get;private set;}
 public enum Mode{Patrol,Warning,Charge,Recover}public Mode State{get;private set;}
 Rigidbody2D body;EnemyHealth health;Transform player;Vector2 home;float timer=1.8f,age,heading=1;
 void Awake(){body=GetComponent<Rigidbody2D>();health=GetComponent<EnemyHealth>();home=transform.position;}
 void Start(){player=FindAnyObjectByType<PlayerHealth>().transform;}
 void FixedUpdate(){if(player==null||health.IsDead||health.IsStunned)return;age+=Time.fixedDeltaTime;timer-=Time.fixedDeltaTime;Vector2 delta=(Vector2)player.position-body.position;
 if(flying){body.gravityScale=0;if(State==Mode.Patrol&&delta.magnitude<7){State=Mode.Warning;timer=.6f;}if(State==Mode.Warning){body.linearVelocity=Vector2.zero;if(timer<=0){State=Mode.Charge;Attacks++;}}else if(State==Mode.Charge){body.linearVelocity=Vector2.MoveTowards(body.linearVelocity,delta.normalized*speed,6*Time.fixedDeltaTime);if(delta.magnitude>11)State=Mode.Patrol;}else body.linearVelocity=(home+Vector2.up*Mathf.Sin(age)*.3f-body.position)*2;}
 else{float vx=0;switch(State){case Mode.Patrol:vx=heading*1.1f;if(Mathf.Abs(delta.x)<7&&Mathf.Abs(delta.y)<1.6f){heading=Mathf.Sign(delta.x);State=Mode.Warning;timer=.65f;vx=0;}break;case Mode.Warning:if(timer<=0){State=Mode.Charge;timer=.85f;Attacks++;}break;case Mode.Charge:vx=heading*7;if(timer<=0){State=Mode.Recover;timer=1.2f;vx=0;}break;case Mode.Recover:if(timer<=0){State=Mode.Patrol;timer=2;}break;}
 var foot=GetComponent<Collider2D>().bounds.min.y;var wall=Physics2D.Raycast(body.position,Vector2.right*heading,.75f,1<<8);var floor=Physics2D.Raycast(new Vector2(body.position.x+heading*.7f,foot+.25f),Vector2.down,.7f,1<<8);if(vx!=0&&(wall.collider!=null||floor.collider==null)){vx=0;heading=-heading;if(State==Mode.Charge){State=Mode.Recover;timer=1.2f;}}body.linearVelocity=new Vector2(vx,body.linearVelocity.y);}
 if(warning!=null)warning.enabled=State==Mode.Warning;
 }
 void LateUpdate(){if(health.IsDead)return;var frames=State==Mode.Charge?charge:body.linearVelocity.sqrMagnitude>.1f?move:idle;if(frames!=null&&frames.Length>0)visual.sprite=frames[Mathf.FloorToInt(age*12)%frames.Length];if(Mathf.Abs(body.linearVelocity.x)>.1f)visual.flipX=body.linearVelocity.x>0;}
}
