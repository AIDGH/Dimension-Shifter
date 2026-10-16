using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class AudioContactSetup
{
 public static void Run(){try{
  foreach(var path in new[]{"Assets/Scenes/SampleScene.unity","Assets/Scenes/PaintedDimension.unity"}){
   var scene=EditorSceneManager.OpenScene(path);
   var player=UnityEngine.Object.FindAnyObjectByType<PlayerAttack>();
   player.dashForce=16f;player.dashAttackDuration=.45f;
   var health=player.GetComponent<PlayerHealth>();health.iFramesDuration=2f;health.numberOfFlashes=6;
   if(player.GetComponent<PlayerEnemyContact>()==null)player.gameObject.AddComponent<PlayerEnemyContact>();
   if(player.GetComponent<PlayerMovementAudio>()==null)player.gameObject.AddComponent<PlayerMovementAudio>();
   foreach(var item in UnityEngine.Object.FindAnyObjectByType<LevelGenerator>().midChunks)item.weight=10;
   EditorSceneManager.SaveScene(scene);
  }
  var scenes=EditorBuildSettings.scenes.ToList();
  foreach(var path in new[]{"Assets/Scenes/SampleScene.unity","Assets/Scenes/PaintedDimension.unity"})if(!scenes.Any(s=>s.path==path&&s.enabled))scenes.Add(new EditorBuildSettingsScene(path,true));
  EditorBuildSettings.scenes=scenes.ToArray();
  AssetDatabase.SaveAssets();File.WriteAllText("audio-contact-setup.txt","Wired contact damage and movement SFX in both scenes; balanced six chunk weights to 10; enabled both scenes for retry.\n");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
