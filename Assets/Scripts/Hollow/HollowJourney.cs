using UnityEngine;
public class HollowJourney:MonoBehaviour
{
 public Rect[] rooms;public Vector3[] entrances;public string[] roomNames;
 public int CurrentRoom{get;private set;}
 void Update(){var s=DemoSession.Instance;if(s==null)return;Shader.SetGlobalFloat("_HollowEcho",DimensionController.Instance.Current);for(int i=0;i<rooms.Length;i++)if(rooms[i].Contains(s.transform.position))CurrentRoom=i;
 s.objectiveOverride=!s.HasEchoKey?"GREENPATH · Find the violet key above the forest":!s.HasSpirit?(CurrentRoom==0?"Q to FROST · open the upper forest door":"ECHO · Find Spirit Bolt within this grove"):!s.BossDefeated?(CurrentRoom==2?"WARDEN · change worlds to strike its exposed core":"FROST · cross the eastern door to the sanctum"):"Return to the final sanctuary";
 if(Input.GetKeyDown(KeyCode.F6)&&(Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl))){int next=(CurrentRoom+1)%rooms.Length;Teleport(entrances[next]);}
 }
 public void Teleport(Vector3 p){var s=DemoSession.Instance;var rb=s.GetComponent<Rigidbody2D>();s.GetComponent<PlayerAttack>().enabled=false;rb.position=p;rb.linearVelocity=Vector2.zero;rb.rotation=0;s.transform.position=p;s.GetComponent<PlayerMovement>().enabled=true;s.GetComponent<PlayerAttack>().enabled=true;Physics2D.SyncTransforms();var cam=Camera.main.GetComponent<HollowRoomCamera>();if(cam!=null)cam.Snap();}
}
