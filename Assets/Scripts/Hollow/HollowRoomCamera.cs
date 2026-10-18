using UnityEngine;
public class HollowRoomCamera:MonoBehaviour
{
 public HollowJourney journey;Vector3 velocity;bool snap=true;
 public void Snap(){snap=true;}
 void LateUpdate(){if(DemoSession.Instance==null)return;var cam=GetComponent<Camera>();var p=DemoSession.Instance.transform.position;Rect r=journey.rooms[journey.CurrentRoom];float x=cam.orthographicSize*cam.aspect,y=cam.orthographicSize;var target=new Vector3(Mathf.Clamp(p.x,r.xMin+x,r.xMax-x),Mathf.Clamp(p.y+1.5f,r.yMin+y,r.yMax-y),-10);transform.position=snap?target:Vector3.SmoothDamp(transform.position,target,ref velocity,.12f);snap=false;}
}
