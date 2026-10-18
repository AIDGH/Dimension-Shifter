using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using Object=UnityEngine.Object;
public static class SanctuaryTreeSetup
{
 public static void Run(){try{
  AssetDatabase.Refresh();string path="Assets/Art/Scenery/SanctuaryTree.png";
  var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.isReadable=true;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.filterMode=FilterMode.Bilinear;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=2048;imp.SaveAndReimport();
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=tex.GetPixels32();int low=tex.height,high=0;
  for(int y=0;y<tex.height;y++)for(int x=0;x<tex.width;x++)if(pixels[y*tex.width+x].a>64){low=Mathf.Min(low,y);high=Mathf.Max(high,y);}
  var settings=new TextureImporterSettings();imp.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,(float)low/tex.height);imp.SetTextureSettings(settings);imp.spritePixelsPerUnit=(high-low+1)/8f;imp.isReadable=false;imp.SaveAndReimport();
  var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);int hidden=0;
  foreach(var prefabPath in new[]{"Assets/Prefabs/PaintedChunks/End_Chunk_01_Painted.prefab","Assets/Prefabs/Encounters/TrailEnd_Painted.prefab"}){
   var root=PrefabUtility.LoadPrefabContents(prefabPath);bool exists=root.GetComponentsInChildren<SpriteRenderer>(true).Any(s=>s.gameObject.name=="Sanctuary canopy");Vector3 foot=Vector3.zero;bool found=false;
   foreach(var map in root.GetComponentsInChildren<Tilemap>(true)){
    var renderer=map.GetComponent<TilemapRenderer>();if(renderer==null||!renderer.enabled)continue;
    for(int x=27;x<=31;x++)for(int y=2;y<=9;y++){
     var cell=new Vector3Int(x,y,0);if(map.GetSprite(cell)==null)continue;
     var source=map.GetTile<Tile>(cell);var matrix=map.GetTransformMatrix(cell);
     if(source!=null){
      string hiddenPath="Assets/Art/Scenery/HiddenTiles/Canopy_"+source.name.Replace("Canopy_","")+".asset";
      var invisible=AssetDatabase.LoadAssetAtPath<Tile>(hiddenPath);
      if(invisible==null){invisible=Object.Instantiate(source);invisible.name="Canopy_"+source.name;invisible.color=Color.clear;invisible.flags=TileFlags.LockColor;AssetDatabase.CreateAsset(invisible,hiddenPath);}
      map.SetTile(cell,invisible);map.SetTileFlags(cell,TileFlags.LockColor);map.SetTransformMatrix(cell,matrix);
     }
     hidden++;
     if(!found){foot=map.CellToWorld(new Vector3Int(29,2,0))+Vector3.right*.5f;found=true;}
    }
   }
   if(!exists&&found){var tree=new GameObject("Sanctuary canopy");tree.transform.SetParent(root.transform);tree.transform.position=foot;var sr=tree.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Painted/PaintedSprites.mat");sr.sortingOrder=5;}
   PrefabUtility.SaveAsPrefabAsset(root,prefabPath);PrefabUtility.UnloadPrefabContents(root);
  }
  AssetDatabase.SaveAssets();File.WriteAllText("tree-setup.txt","Hidden old tree cells (colliders retained): "+hidden+"\nComplete painted tree added to end prefabs.\n");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
