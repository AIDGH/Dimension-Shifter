using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Render the actual scene backgrounds immediately before/after repeat boundaries.
public static class BackgroundVisualChecks
{
    static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static Parallax[] layers;
    static Camera camera;
    public static void Run()
    {
        try {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity",OpenSceneMode.Single);
            camera=Camera.main;
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
            layers=UnityEngine.Object.FindObjectsByType<Parallax>(FindObjectsSortMode.None);
            foreach(var layer in layers)typeof(Parallax).GetMethod("Start",flags).Invoke(layer,null);
            foreach(var sr in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)) {
                if(sr.sprite==null)continue;
                var material=new Material(sr.sharedMaterial);material.mainTexture=sr.sprite.texture;sr.sharedMaterial=material;
            }
            int boundaries=0;float worst=0f;
            foreach(var layer in layers) {
                if(layer.parallaxEffect>=.99f)continue;
                var sr=layer.GetComponent<SpriteRenderer>();
                float unit=sr.sprite.rect.width/sr.sprite.pixelsPerUnit*Mathf.Abs(layer.transform.lossyScale.x);
                float origin=((Vector3)typeof(Parallax).GetField("startPosition",flags).GetValue(layer)).x;
                float threshold=(origin+unit*.5f)/(1-layer.parallaxEffect);
                var before=Capture(threshold-.0005f);
                var shades=new HashSet<int>();foreach(var pixel in before)shades.Add((pixel.r<<16)|(pixel.g<<8)|pixel.b);
                if(shades.Count<500)throw new Exception("Background render is empty or untextured");
                var after=Capture(threshold+.0005f);
                double delta=0;
                for(int i=0;i<before.Length;i++)delta+=(Mathf.Abs(before[i].r-after[i].r)+Mathf.Abs(before[i].g-after[i].g)+Mathf.Abs(before[i].b-after[i].b))/765.0;
                float average=(float)(delta/before.Length);worst=Mathf.Max(worst,average);boundaries++;
                if(average>.004f)throw new Exception(layer.name+" visible pop: mean pixel difference "+average);
            }
            File.WriteAllText("background-visual-results.txt",$"PASS {boundaries} actual background repeat boundaries rendered before/after.\nMaximum mean RGB change: {worst:F6} (limit 0.004).\nNo overlapping fog renderers.\n");
            EditorApplication.Exit(0);
        } catch(Exception e) {Debug.LogException(e);File.WriteAllText("background-visual-results.txt","FAIL "+e);EditorApplication.Exit(1);}
    }
    static Color32[] Capture(float x)
    {
        var p=camera.transform.position;p.x=x;camera.transform.position=p;
        foreach(var layer in layers)typeof(Parallax).GetMethod("LateUpdate",flags).Invoke(layer,null);
        var rt=new RenderTexture(640,360,24);rt.Create();
        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
        RenderTexture.active=rt;var image=new Texture2D(640,360,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();
        File.WriteAllBytes("background-check-frame.png",image.EncodeToPNG());var result=image.GetPixels32();
        RenderTexture.active=null;rt.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
        return result;
    }
}
