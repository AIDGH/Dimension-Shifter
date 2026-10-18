using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class DemoStabilitySetup
{
    const string Enc="Assets/Prefabs/Encounters/";
    public static void Run()
    {
        try
        {
            AssetDatabase.Refresh();int added=0,aligned=0;string report="";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Traps/IceTrap.prefab");
            foreach(string path in Directory.GetFiles(Enc,"*.prefab").Concat(Directory.GetFiles("Assets/Prefabs/PaintedChunks","*.prefab")))
            {
                var root=PrefabUtility.LoadPrefabContents(path);bool changed=false;
                foreach(var spawner in root.GetComponentsInChildren<ChunkTrapSpawner>(true)){spawner.yOffset=.59f;changed=true;}
                Physics2D.SyncTransforms();var physics=root.scene.GetPhysicsScene2D();
                foreach(var trap in root.GetComponentsInChildren<TrapDamage>(true))
                {
                    var pos=trap.transform.position;
                    var floor=physics.Raycast(new Vector2(pos.x,pos.y+2),Vector2.down,5,1<<8);
                    if(floor.collider==null)continue;
                    trap.transform.position=new Vector3(pos.x,floor.point.y+.59f,pos.z);aligned++;changed=true;
                }
                string name=Path.GetFileName(path);bool route=new[]{"07_","08_","09_","10_","11_","12_","14_"}.Any(n=>name.StartsWith(n));
                if(route)
                {
                    int count=root.GetComponentsInChildren<TrapDamage>(true).Count(t=>t.name.StartsWith("Trail ice "));float width=root.transform.Find("EndPosition").localPosition.x;
                    for(float x=8;x<width-5&&count<2;x+=5)
                    {
                        var point=new Vector2(root.transform.position.x+x,root.transform.position.y+14);
                        var floor=physics.Raycast(point,Vector2.down,35,1<<8);if(floor.collider==null||floor.normal.y<.99f)continue;
                        var left=physics.Raycast(point+Vector2.left*.5f,Vector2.down,35,1<<8);var right=physics.Raycast(point+Vector2.right*.5f,Vector2.down,35,1<<8);
                        if(left.collider==null||right.collider==null||Mathf.Abs(left.point.y-floor.point.y)>.03f||Mathf.Abs(right.point.y-floor.point.y)>.03f)continue;
                        var pos=new Vector3(point.x,floor.point.y+.59f);
                        if(root.GetComponentsInChildren<TrapDamage>(true).Any(t=>Mathf.Abs(t.transform.position.x-pos.x)<4))continue;
                        if(root.GetComponentsInChildren<TrailMarker>(true).Any(t=>Vector2.Distance(t.transform.position,pos)<4))continue;
                        if(root.GetComponentsInChildren<QuestInteractable>(true).Any(t=>Vector2.Distance(t.transform.position,pos)<3))continue;
                        if(root.GetComponentsInChildren<EnemyHealth>(true).Any(t=>Vector2.Distance(t.transform.position,pos)<2))continue;
                        if(root.GetComponentsInChildren<DimensionMember>(true).Any(t=>t.GetComponentsInChildren<Collider2D>().Any(col=>col.bounds.SqrDistance(pos)<4)))continue;
                        var trap=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform);trap.name="Trail ice "+x;trap.transform.position=pos;count++;added++;changed=true;
                        report+=name+" ice at "+pos+"\n";
                    }
                }
                if(changed)PrefabUtility.SaveAsPrefabAsset(root,path);
                PrefabUtility.UnloadPrefabContents(root);
            }
            foreach(string scene in new[]{"Assets/Scenes/PaintedDimension.unity","Assets/Scenes/SampleScene.unity"})
            {
                EditorSceneManager.OpenScene(scene);
                foreach(var spawner in UnityEngine.Object.FindObjectsByType<ChunkTrapSpawner>(FindObjectsInactive.Include))spawner.yOffset=.59f;
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            foreach(var path in Directory.GetFiles("Assets/Resources/HollowSFX","*.wav"))
            {var importer=(AudioImporter)AssetImporter.GetAtPath(path);importer.forceToMono=true;var settings=importer.defaultSampleSettings;settings.preloadAudioData=true;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.9f;importer.defaultSampleSettings=settings;importer.SaveAndReimport();}
            File.WriteAllText("stability-setup.txt",$"Aligned existing traps: {aligned}\nAdded traps across both styles: {added}\n"+report);AssetDatabase.SaveAssets();EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
