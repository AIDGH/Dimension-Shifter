using UnityEngine;
public class FinalTreeGate:MonoBehaviour
{
 public Collider2D barrier;public SpriteRenderer door;public GameObject finalRift;
 public float interactionRadius=3.2f;public Transform farSide;
 public bool TryOpen(){var s=DemoSession.Instance;if(s==null||!s.Started||s.Paused||s.GetComponent<PlayerHealth>().IsDead||Vector2.Distance(s.transform.position,transform.position)>interactionRadius)return false;
 if(!s.OpenTree()){RunProgress.Instance.ShowMessage("The ancient tree needs the WARDEN SIGIL. Defeat the Warden first.");return false;}
 RunProgress.Instance.ShowMessage("The roots part. Beyond the tree, the last rift leads home.");GameAudio.Play("checkpoint",.6f);Apply();if(farSide!=null){var rb=s.GetComponent<Rigidbody2D>();rb.position=farSide.position;rb.linearVelocity=Vector2.zero;s.transform.position=farSide.position;Physics2D.SyncTransforms();var cam=Camera.main.GetComponent<CameraFollow>();if(cam!=null)cam.SnapToPlayer();}return true;}
 void Update(){Apply();var s=DemoSession.Instance;if(s!=null&&Vector2.Distance(s.transform.position,transform.position)<interactionRadius){if(Input.GetKeyDown(KeyCode.E))TryOpen();}}
 void Apply(){var s=DemoSession.Instance;bool open=s!=null&&s.TreeOpened;if(barrier!=null)barrier.enabled=!open;if(door!=null)door.color=open?new Color(1,.85f,.45f,.12f):new Color(1,.85f,.45f,1);if(finalRift!=null)finalRift.SetActive(open);}
 public string InteractionHint => DemoSession.Instance.TreeOpened ? "[ E ]  PASS THROUGH THE ROOTS" : DemoSession.Instance.HasWardenKey ? "[ E ]  UNSEAL THE ANCIENT TREE" : "SEALED · WARDEN SIGIL REQUIRED";
}
