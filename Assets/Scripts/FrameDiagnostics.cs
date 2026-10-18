using UnityEngine;

// F3 measures game-side timing, not physical-key-to-display latency.
public class FrameDiagnostics : MonoBehaviour
{
    bool visible;float elapsed,total,worst,mean,peak;int frames;PlayerMovement motor;
    void Awake(){motor=GetComponent<PlayerMovement>();}
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.F3))visible=!visible;
        float dt=Time.unscaledDeltaTime;total+=dt;elapsed+=dt;frames++;worst=Mathf.Max(worst,dt);
        if(elapsed>=1){mean=total/frames;peak=worst;total=elapsed=worst=0;frames=0;}
    }
    void OnGUI()
    {
        if(!visible)return;
        var style=new GUIStyle(GUI.skin.box){fontSize=16,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(14,14,10,10)};
        GUI.Box(new Rect(12,Screen.height-162,570,96),
            $"F3 · {1/Mathf.Max(.001f,mean):F0} FPS | frame {mean*1000:F1} ms | worst {peak*1000:F1} ms\n"+
            $"Input poll → physics: {motor.LastInputToPhysicsMs:F1} ms | acceleration: {motor.useAcceleration}\n"+
            "Game timing only · CTRL + SHIFT + B: boss practice",style);
    }
}
