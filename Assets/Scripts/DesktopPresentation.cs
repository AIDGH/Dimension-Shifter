using UnityEngine;
using UnityEngine.Rendering;

public static class DesktopPresentation
{
    public static void Configure()
    {
        double refresh = Screen.currentResolution.refreshRateRatio.value;
        if (refresh < 30 || refresh > 500) refresh = 60;
        QualitySettings.vSyncCount = Mathf.Max(1, Mathf.CeilToInt((Mathf.RoundToInt((float)refresh) - .5f) / 120f));
        Application.targetFrameRate = -1;
        QualitySettings.maxQueuedFrames = 1;
        OnDemandRendering.renderFrameInterval = 1;
        Time.fixedDeltaTime = 1f / (refresh >= 90 ? 120f : 60f);
        Time.maximumDeltaTime = .05f;
        if (Application.platform == RuntimePlatform.OSXPlayer)
        {
            // Retina window size can otherwise turn a small 2D game into a 6MP render.
            const float pixels = 1920f * 1080f;
            if ((float)Screen.width * Screen.height > pixels)
            {
                float scale = Mathf.Sqrt(pixels / ((float)Screen.width * Screen.height));
                Screen.SetResolution(Mathf.RoundToInt(Screen.width * scale), Mathf.RoundToInt(Screen.height * scale), FullScreenMode.Windowed);
            }
        }
    }
}
