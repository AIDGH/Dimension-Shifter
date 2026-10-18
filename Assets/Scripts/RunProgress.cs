using UnityEngine;
using UnityEngine.SceneManagement;

public class RunProgress : MonoBehaviour
{
    public static RunProgress Instance { get; private set; }
    public bool Completed { get; private set; }
    public Vector3 CheckpointPosition { get; private set; }
    public string CheckpointName { get; private set; } = "Trail entrance";
    public bool useAbsoluteFallLimit;
    int checkpointDimension;
    PlayerHealth health;
    PlayerMovement movement;
    PlayerAttack attack;
    Rigidbody2D body;
    float messageUntil;
    string message;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }
    void Awake() { Instance=this; health=GetComponent<PlayerHealth>(); movement=GetComponent<PlayerMovement>(); attack=GetComponent<PlayerAttack>(); body=GetComponent<Rigidbody2D>(); CheckpointPosition=transform.position; }
    void Update()
    {
        if(Completed) { if(Input.GetKeyDown(KeyCode.R))SceneManager.LoadScene(SceneManager.GetActiveScene().name); return; }
        if(!health.IsDead && transform.position.y < (useAbsoluteFallLimit ? -4f : CheckpointPosition.y-18f)) Respawn();
    }
    public void ShowMessage(string text) { message=text; messageUntil=Time.time+Mathf.Clamp(5f+text.Length/22f,8f,14f); }
    public void ResumeForPractice(){Completed=false;health.enabled=true;}
    public void SetCheckpoint(Vector3 position, string label)
    {
        CheckpointPosition=position; CheckpointName=label;
        checkpointDimension=DimensionController.Instance != null ? DimensionController.Instance.Current : 0;
        health.RestoreHealth(); ShowMessage("Checkpoint saved: "+label); GameAudio.Play("checkpoint",.5f);
    }
    public void Respawn()
    {
        if(Completed) return;
        // Finish any dash before restoring the movement controller and collisions.
        attack.enabled=false; movement.enabled=false;
        var contact=GetComponent<PlayerEnemyContact>(); if(contact!=null)contact.enabled=false;
        body.position=CheckpointPosition; body.rotation=0; body.linearVelocity=Vector2.zero; body.angularVelocity=0;
        transform.position=CheckpointPosition; transform.rotation=Quaternion.identity;
        foreach(var shot in FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)) Destroy(shot.gameObject);
        if(DimensionController.Instance!=null)DimensionController.Instance.SetDimension(checkpointDimension);
        health.RestoreHealth(); movement.enabled=true; attack.enabled=true; if(contact!=null)contact.enabled=true;
        Physics2D.SyncTransforms(); var camera=Camera.main!=null?Camera.main.GetComponent<CameraFollow>():null; if(camera!=null)camera.SnapToPlayer();
        ShowMessage("Returned to "+CheckpointName);
    }
    public void Complete()
    {
        if(Completed || health.IsDead)return;
        var demo=GetComponent<DemoSession>();if(demo!=null&&!demo.BossDefeated){ShowMessage("The Rift Warden still guards the exit.");return;}
        if(demo!=null&&demo.RequiresTreeKey&&!demo.TreeOpened){ShowMessage("Use the Warden sigil at the ancient tree first · E.");return;}
        Completed=true; attack.enabled=false; movement.enabled=false; body.linearVelocity=Vector2.zero;
        health.enabled=false; GameAudio.Play("checkpoint",.6f);
    }
    public string ActiveMessage => Time.time < messageUntil ? message : "";
    void OnGUI()
    {
        if(GetComponent<GameHUD>() != null)return;
        var style=new GUIStyle(GUI.skin.box){fontSize=16,alignment=TextAnchor.MiddleCenter,wordWrap=true};
        string dimension=DimensionController.Instance != null && DimensionController.Instance.Current==1 ? "ECHO / violet" : "FROST / cyan";
        GUI.Box(new Rect(12,Screen.height-65,510,52),"V  Strike     B  Phase dash     Q  Shift dimension\n"+dimension+"     |     "+CheckpointName,style);
        if(Time.time<messageUntil)GUI.Box(new Rect(Screen.width/2f-260,85,520,65),message,style);
        if(Completed){style.fontSize=26;GUI.Box(new Rect(Screen.width/2f-230,Screen.height/2f-65,460,130),"Rift sealed!\nDemo complete\nR — play again",style);}
    }
    void OnDestroy() { if(Instance==this)Instance=null; }
}
