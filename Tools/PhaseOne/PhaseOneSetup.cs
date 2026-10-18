using System;using System.IO;using System.Linq;using UnityEngine;using UnityEngine.Tilemaps;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
public static class PhaseOneSetup
{
 public static void Main()=>Run(false); public static void Hollow()=>Run(true);
 public static void Mac()=>Run(false,true);
 static void Run(bool hollow,bool build=false){try{
 AssetDatabase.Refresh();var path="Assets/Resources/Story/Backdrop.png";var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.maxTextureSize=2048;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.SaveAndReimport();
 if(!hollow)Tree();
 PlayerSettings.macRetinaSupport=false;PlayerSettings.defaultIsNativeResolution=false;PlayerSettings.enableFrameTimingStats=true;
 AssetDatabase.SaveAssets();File.WriteAllText("phase1-setup.txt","Story backdrop imported; native Retina render disabled; frame timing enabled; "+(hollow?"Hollow intro updated.":"tree moved +2 X, door grounded, hidden trunk collisions cleared, exit raised to floor.")+"\n");if(build)PhaseOneBuild.Mac();else EditorApplication.Exit(0);
 }catch(Exception e){File.WriteAllText("phase1-setup.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
 static void Tree(){const string path="Assets/Prefabs/Encounters/TrailEnd_Painted.prefab";var root=PrefabUtility.LoadPrefabContents(path);var tree=root.GetComponentsInChildren<SpriteRenderer>(true).First(s=>s.name=="Sanctuary canopy");tree.transform.position=new Vector3(31.5f,2.92f,0);tree.sortingOrder=4;
 // The invisible original canopy tiles still carried tall colliders. Only remove the old trunk cells.
 int removed=0;foreach(var map in root.GetComponentsInChildren<Tilemap>(true))for(int x=27;x<=31;x++)for(int y=2;y<=10;y++){
 var cell=new Vector3Int(x,y,0);var tile=map.GetTile(cell);if(tile==null)continue;
 bool hidden=AssetDatabase.GetAssetPath(tile).Contains("HiddenTiles/Canopy_");
 if(hidden||y>=3){map.SetTile(cell,null);removed++;}}
 foreach(var c in root.GetComponentsInChildren<TilemapCollider2D>(true))c.ProcessTilemapChanges();
 foreach(var map in root.GetComponentsInChildren<Tilemap>(true))if(map.name=="Original props")for(int x=26;x<=31;x++)for(int y=0;y<=4;y++)map.SetTile(new Vector3Int(x,y,0),null);
 foreach(var prop in root.GetComponentsInChildren<SpriteRenderer>(true))if(prop.name=="Ancient root column"&&prop.transform.position.x>=27&&prop.transform.position.x<=31)prop.enabled=false;
 var gate=root.GetComponentInChildren<FinalTreeGate>();gate.transform.localPosition=new Vector3(29.3f,3,0);gate.interactionRadius=2.8f;
 gate.door.transform.localScale=Vector3.one*.85f;gate.door.transform.localPosition=new Vector3(0,.043f,0);gate.door.sortingOrder=20;
 gate.barrier.transform.localPosition=Vector3.zero;
 var exit=gate.finalRift.transform;exit.localPosition=new Vector3(36,4,0);var portal=exit.Find("Golden home rift");portal.localPosition=new Vector3(0,.65f,0);
 gate.farSide.localPosition=new Vector3(34.4f,3.65f,0);
 var old=root.transform.Find("Door stone approach");if(old!=null)Object.DestroyImmediate(old.gameObject);
 var foundation=new GameObject("Door stone approach");foundation.transform.SetParent(root.transform,false);foundation.layer=8;
 var points=new[]{new Vector2(26.2f,0),new Vector2(31,0),new Vector2(31,3),new Vector2(27.8f,3),new Vector2(26.2f,1.2f)};
 var vertices=points.Select(v=>new Vector3(v.x,v.y,-.01f)).ToList();var uv=points.Select(v=>v/4f).ToList();var colors=points.Select(v=>new Color(1,1,1,0)).ToList();var indices=new System.Collections.Generic.List<int>{0,1,2,0,2,3,0,3,4};
 for(int i=2;i<4;i++){var a=points[i];var b=points[i+1];var normal=new Vector2(b.y-a.y,a.x-b.x).normalized;int n=vertices.Count;foreach(var v in new[]{a,b,b-normal*.045f,a-normal*.045f}){vertices.Add(new Vector3(v.x,v.y,-.015f));uv.Add(v/4);colors.Add(new Color(1,1,1,1));}indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
 var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
 const string meshPath="Assets/Art/Painted/Meshes/FinalDoorApproach.asset";var previous=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(previous!=null){EditorUtility.CopySerialized(mesh,previous);Object.DestroyImmediate(mesh);}else AssetDatabase.CreateAsset(mesh,meshPath);
 foundation.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);var renderer=foundation.AddComponent<MeshRenderer>();renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Painted/ColdCavernStone.mat");renderer.sortingOrder=1;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
 foundation.AddComponent<PolygonCollider2D>().SetPath(0,points);
 // Keep only the painted terrain and the new approach as the walkable surface here.
 foreach(var map in root.GetComponentsInChildren<Tilemap>(true))for(int x=27;x<=31;x++)for(int y=0;y<3;y++)if(map.HasTile(new Vector3Int(x,y,0)))map.SetColliderType(new Vector3Int(x,y,0),Tile.ColliderType.None);
 foreach(var c in root.GetComponentsInChildren<TilemapCollider2D>(true))c.ProcessTilemapChanges();
 var checkpoint=root.GetComponentsInChildren<TrailMarker>(true).FirstOrDefault(m=>m.kind==TrailMarker.MarkerKind.Checkpoint);if(checkpoint!=null)checkpoint.transform.localPosition=new Vector3(25.5f,.9f,0);
 Physics2D.SyncTransforms();string report="Removed trunk cells: "+removed+"\nTree foot "+tree.transform.position+" Door foot "+gate.door.bounds.min+"\n";
 for(float x=27;x<=38;x+=.5f){var hit=root.scene.GetPhysicsScene2D().Raycast(new Vector2(x,16),Vector2.down,25,1<<8);report+="Floor "+x+" = "+hit.point+" "+(hit.collider!=null?hit.collider.name:"none")+"\n";}
 File.WriteAllText("phase1-tree-layout.txt",report);PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
 }
}
