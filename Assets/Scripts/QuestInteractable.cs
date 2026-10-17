using UnityEngine;

public class QuestInteractable : MonoBehaviour
{
    public enum Kind { EchoKey, BlueDoor, Spirit, ReturnPortal, Relay }
    public Kind kind;
    public int dimension=-1;
    public Transform destination;
    public SpriteRenderer visual;
    public float radius=1.8f;
    bool consumed;
    Color baseColor;
    void Start(){if(visual!=null)baseColor=visual.color;}
    void Update()
    {
        var session=DemoSession.Instance;if(session==null)return;
        bool present=dimension<0||DimensionController.Instance.Current==dimension;
        if(visual!=null){var c=baseColor;c.a=consumed?.16f:present?1:.16f;visual.color=c;}
        if(!session.Started||session.GetComponent<PlayerHealth>().IsDead||session.GetComponent<RunProgress>().Completed||consumed)return;
        if(kind==Kind.Relay)return;
        if(Vector2.Distance(session.transform.position,transform.position)>radius)return;
        if(kind==Kind.EchoKey&&present){session.ClaimKey();consumed=true;return;}
        if(Input.GetKeyDown(KeyCode.E))Interact(session);
    }
    public bool Interact(DemoSession session)
    {
        if(session==null||!session.Started||consumed||session.GetComponent<PlayerHealth>().IsDead||Vector2.Distance(session.transform.position,transform.position)>radius)return false;
        var run=session.GetComponent<RunProgress>();
        if(dimension>=0&&DimensionController.Instance.Current!=dimension){run.ShowMessage("This doorway answers only in the blue world. Q to shift.");return false;}
        if(kind==Kind.BlueDoor&&!session.HasEchoKey){run.ShowMessage("Locked. Find the violet key on the upper sanctuary ledge.");return false;}
        if(kind==Kind.Spirit){session.ClaimSpirit();consumed=true;return true;}
        if(kind==Kind.ReturnPortal&&!session.HasSpirit){run.ShowMessage("Claim the spirit at the altar first · E.");return false;}
        if(destination==null)return false;
        var attack=session.GetComponent<PlayerAttack>();attack.enabled=false;
        var body=session.GetComponent<Rigidbody2D>();body.position=destination.position;body.linearVelocity=Vector2.zero;body.rotation=0;
        session.transform.position=destination.position;session.GetComponent<PlayerMovement>().enabled=true;attack.enabled=true;
        Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToPlayer();
        run.ShowMessage(kind==Kind.BlueDoor?"Hidden sanctuary · claim the spirit at the altar with E.":"Return to the trail · C fires Spirit Bolt.");GameAudio.Play("shift",.6f);return true;
    }
    public bool ReceiveBolt(int world)
    {
        if(kind!=Kind.Relay||consumed||world!=dimension||DemoSession.Instance==null)return false;
        consumed=true;DemoSession.Instance.ActivateRelay(dimension);CombatFeedback.Hit(transform.position,false);return true;
    }
}
