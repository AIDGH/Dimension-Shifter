using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class SanctumDoorPolish
{
    const string Key="Demo.DoorPolish",Art="Assets/Art/Demo/SanctumDoorV2.png";
    static bool booted;
    static SanctumDoorPolish(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Boot;}
    public static void Run()
    {
        AssetDatabase.Refresh();
        var importer=(TextureImporter)AssetImporter.GetAtPath(Art);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.isReadable=true;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art);var pixels=texture.GetPixels32();
        int bottom=texture.height,top=0;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>64){bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
        importer.spritePixelsPerUnit=(top+1-bottom)/3.2f;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,(float)bottom/texture.height);importer.SetTextureSettings(settings);
        importer.isReadable=false;importer.SaveAndReimport();var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art);
        foreach(string suffix in new[]{"","_Painted"})
        {
            string path="Assets/Prefabs/Encounters/13_KeySanctum"+suffix+".prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            var door=root.GetComponentsInChildren<QuestInteractable>().Single(q=>q.kind==QuestInteractable.Kind.BlueDoor);
            var p=door.transform.localPosition;p.y=-.01f;door.transform.localPosition=p;
            door.visual.sprite=sprite;door.visual.color=Color.white;door.visual.sortingOrder=0;
            door.visual.transform.localPosition=Vector3.forward;door.visual.transform.localScale=Vector3.one;
            PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
        File.WriteAllText("door-import-results.txt","Opaque height: 3.2 world units\nThreshold: -0.01 world units\nPixels per unit: "+importer.spritePixelsPerUnit+"\nPivot: "+settings.spritePivot+"\n");
        EditorSceneManager.OpenScene("Assets/Scenes/PaintedDimension.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void Boot(){EditorApplication.QueuePlayerLoopUpdate();if(!EditorApplication.isPlaying||booted||Time.frameCount<3)return;booted=true;new GameObject("Door preview").AddComponent<DoorPreview>().StartCoroutine("Run");}
    public static void Done(){SessionState.SetBool(Key,false);EditorApplication.Exit(0);}
}
public class DoorPreview:MonoBehaviour
{
    IEnumerator Run()
    {
        var demo=DemoSession.Instance;demo.Begin();Object.FindAnyObjectByType<LevelGenerator>().enabled=false;
        var root=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Encounters/13_KeySanctum_Painted.prefab"),new Vector3(1000,0),Quaternion.identity);
        yield return null;
        var door=root.GetComponentsInChildren<QuestInteractable>().Single(q=>q.kind==QuestInteractable.Kind.BlueDoor);
        var dim=demo.GetComponent<DimensionController>();var body=demo.GetComponent<Rigidbody2D>();
        demo.transform.position=body.position=new Vector2(1031,.7f);dim.SetDimension(0);
        if(door.Interact(demo))throw new Exception("Door opened without key");
        demo.ClaimKey();dim.SetDimension(1);if(door.Interact(demo))throw new Exception("Door opened in wrong world");
        dim.SetDimension(0);if(!door.Interact(demo)||Vector2.Distance(body.position,door.destination.position)>.01f)throw new Exception("Door teleport failed");
        demo.GetComponent<PlayerMovement>().enabled=false;demo.GetComponent<PlayerAttack>().enabled=false;body.constraints=RigidbodyConstraints2D.FreezeAll;
        demo.transform.position=body.position=new Vector2(1029,.65f);body.linearVelocity=Vector2.zero;
        var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.transform.position=new Vector3(1031,2.4f,cam.transform.position.z);cam.orthographicSize=4.8f;
        yield return new WaitForSeconds(.3f);
        var canvas=Object.FindObjectsByType<Canvas>().First(c=>c.name=="Dimension HUD");
        var rt=new RenderTexture(1600,900,24);rt.Create();cam.aspect=1600f/900;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;cam.targetTexture=rt;
        canvas.GetComponent<CanvasScaler>().SendMessage("Handle");Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
        RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes("demo-door-preview.png",tex.EncodeToPNG());
        File.WriteAllText("door-interaction-results.txt","PASS key required\nPASS Frost required\nPASS key and Frost teleport to sanctuary\n");
        RenderTexture.active=null;cam.targetTexture=null;rt.Release();Destroy(tex);Destroy(rt);SanctumDoorPolish.Done();
    }
}
