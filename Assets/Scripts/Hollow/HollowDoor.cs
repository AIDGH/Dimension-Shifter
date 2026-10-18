using UnityEngine;
public class HollowDoor:MonoBehaviour
{
 public Vector3 destination;public bool needsKey,needsSpirit;public int dimension=-1;float next;
 void Update(){var s=DemoSession.Instance;if(s==null||Time.time<next||!Input.GetKeyDown(KeyCode.E))return;if(TryEnter(s))next=Time.time+.6f;}
 public bool TryEnter(DemoSession s){if(s==null||!s.Started||s.Paused||s.GetComponent<PlayerHealth>().IsDead||Vector2.Distance(s.transform.position,transform.position)>2)return false;var run=s.GetComponent<RunProgress>();if(dimension>=0&&DimensionController.Instance.Current!=dimension){run.ShowMessage("This passage opens in FROST · Q to change world");return false;}if(needsKey&&!s.HasEchoKey){run.ShowMessage("Find the key in ECHO, above the forest");return false;}if(needsSpirit&&!s.HasSpirit){run.ShowMessage("Collect Spirit Bolt in this grove before entering the sanctum");return false;}FindAnyObjectByType<HollowJourney>().Teleport(destination);run.SetCheckpoint(destination,"Greenpath passage");GameAudio.Play("shift",.6f);return true;}
}
