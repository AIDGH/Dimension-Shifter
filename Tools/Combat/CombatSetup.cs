using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.Animations;using UnityEditor.SceneManagement;
// Run in an isolated project, then copy the changed scenes, assets and metadata together.
public static class CombatSetup {
 public static void Run(){try{
  var materialPath="Assets/Art/Combat/EnemyFlash.mat";
  var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
  if(material==null){material=new Material(Shader.Find("DimensionShifter/EnemyFlash"));AssetDatabase.CreateAsset(material,materialPath);}
  foreach(var clipPath in new[]{"Assets/Animations/Enemies/Slime/Slime_Hurt.anim","Assets/Animations/Enemies/Slime/Slime_Die.anim","Assets/Animations/Red/Player_Attack.anim"}){var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);}
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Red/Player.controller");
  var attackState=controller.layers[0].stateMachine.states.First(s=>s.state.name=="Player_Attack").state;
  foreach(var transition in attackState.transitions){transition.exitTime=1f;transition.duration=0f;}
  foreach(var transition in controller.layers[0].stateMachine.anyStateTransitions)if(transition.destinationState==attackState){transition.duration=0f;transition.canTransitionToSelf=false;}
  EditorUtility.SetDirty(controller);
  foreach(var scenePath in new[]{"Assets/Scenes/SampleScene.unity","Assets/Scenes/PaintedDimension.unity"}){
   var scene=EditorSceneManager.OpenScene(scenePath);foreach(var attack in UnityEngine.Object.FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None)){attack.normalAttackDuration=.6667f;attack.activeFrom=.0833f;attack.activeUntil=.5f;}
   foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyPatrol>(FindObjectsSortMode.None)){var health=enemy.GetComponent<EnemyHealth>();if(health==null)health=enemy.gameObject.AddComponent<EnemyHealth>();enemy.GetComponent<SpriteRenderer>().sharedMaterial=material;}
   EditorSceneManager.SaveScene(scene);
  }
  AssetDatabase.SaveAssets();File.WriteAllText("combat-setup-results.txt","Combat wired into SampleScene and PaintedDimension; Hurt/Die/Attack clips nonlooping; player animation transition waits for complete swing.\n");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
