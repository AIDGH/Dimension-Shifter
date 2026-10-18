using UnityEngine;

// F3 measures game-side timing; physical-key-to-display latency still needs a human check.
public class FrameDiagnostics : MonoBehaviour
{
    bool visible; float elapsed, total, worst, mean, peak, logAt; int frames;
    PlayerMovement motor;
    readonly FrameTiming[] timing = new FrameTiming[1];
    double cpu, gpu, present;
    bool alternate;
    bool skipFocusFrame;
    void OnApplicationFocus(bool focused) {total=elapsed=worst=0;frames=0;skipFocusFrame=focused;logAt=Time.unscaledTime+1;}
    void Awake() { motor = GetComponent<PlayerMovement>(); }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3)) visible = !visible;
        if(!Application.isFocused || skipFocusFrame) {skipFocusFrame=false;return;}
        if(visible) {
            FrameTimingManager.CaptureFrameTimings();
            if(FrameTimingManager.GetLatestTimings(1, timing)>0) {cpu=timing[0].cpuMainThreadFrameTime;gpu=timing[0].gpuFrameTime;present=timing[0].cpuMainThreadPresentWaitTime;}
            if(Input.GetKeyDown(KeyCode.F4)) {alternate=!alternate;QualitySettings.vSyncCount=alternate?0:1;Application.targetFrameRate=alternate?120:-1;Debug.Log("[Frame mode] vsync="+QualitySettings.vSyncCount+" target="+Application.targetFrameRate);}
        }
        float dt = Time.unscaledDeltaTime; total += dt; elapsed += dt; frames++; worst = Mathf.Max(worst, dt);
        if (elapsed >= 1) { mean = total / frames; peak = worst; total = elapsed = worst = 0; frames = 0; }
        if (visible && Time.unscaledTime >= logAt)
        {
            logAt = Time.unscaledTime + 3;
            Debug.Log($"[Frame sample] {1 / Mathf.Max(.001f, mean):F1} FPS; mean={mean * 1000:F2}ms peak={peak * 1000:F2}ms; pixels={Screen.width}x{Screen.height}; refresh={Screen.currentResolution.refreshRateRatio.value:F1}Hz; physics={1 / Time.fixedDeltaTime:F0}Hz; cpu={cpu:F2} gpu={gpu:F2} present={present:F2}; vsync={QualitySettings.vSyncCount}; input={motor.LastInputToPhysicsMs:F2}ms; skin={GetComponent<CharacterAppearance>().Label}; power={SystemInfo.batteryStatus}");
        }
    }
    void OnGUI()
    {
        if (!visible) return;
        float scale = Mathf.Clamp(Screen.width / 1600f, .75f, 2f);
        var old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        var style = new GUIStyle(GUI.skin.box) { fontSize = 15, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12, 12, 8, 8) };
        GUI.Box(new Rect(12, Screen.height / scale - 172, 660, 110),
            $"F3 · {1 / Mathf.Max(.001f, mean):F0} FPS | frame {mean * 1000:F1} ms | worst {peak * 1000:F1} ms\n" +
            $"{Screen.width} × {Screen.height} | display {Screen.currentResolution.refreshRateRatio.value:F0} Hz | physics {1 / Time.fixedDeltaTime:F0} Hz\n" +
            $"Input poll → physics: {motor.LastInputToPhysicsMs:F1} ms | acceleration: {motor.useAcceleration}\n" +
            "Game-side timing only · F4 pacing comparison · CTRL + SHIFT + B: boss practice", style);
        GUI.matrix = old;
    }
}
