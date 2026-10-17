using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Keep the visible foot fixed as differently cropped animation frames change.
public static class SlimeFootAlignment
{
    public const float Foot = -.4475f; // Body bottom (-.4375) minus the physics contact gap.
    public static void Run()
    {
        foreach(var path in Directory.GetFiles("Assets/Sprites/Enemies/Slime/Individual Sprites", "*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            bool readable=importer.isReadable;importer.isReadable=true;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();
            var factories=new SpriteDataProviderFactories();factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
            var frames=provider.GetSpriteRects();
            foreach(var frame in frames)
            {
                var rect=frame.rect;int bottom=(int)rect.yMax;
                for(int y=(int)rect.y;y<rect.yMax;y++)for(int x=(int)rect.x;x<rect.xMax;x++)
                    if(pixels[y*texture.width+x].a>80){bottom=Mathf.Min(bottom,y);break;}
                frame.alignment=SpriteAlignment.Custom;
                frame.pivot=new Vector2(.5f,(bottom-rect.y-Foot*importer.spritePixelsPerUnit)/rect.height);
            }
            provider.SetSpriteRects(frames);provider.Apply();
            importer.isReadable=readable;importer.SaveAndReimport();
        }
    }
}
