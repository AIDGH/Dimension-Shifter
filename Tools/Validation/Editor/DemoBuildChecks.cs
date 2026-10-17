using System;using System.IO;using UnityEditor;using UnityEditor.Build;using UnityEditor.Build.Reporting;using UnityEngine;
public static class DemoBuildChecks
{
 public static void Windows()=>Build(BuildTarget.StandaloneWindows64,"Builds/Windows/DimensionShifter.exe","windows-build-results.txt");
 public static void Mac()=>Build(BuildTarget.StandaloneOSX,"Builds/Mac/Dimension Shifter.app","mac-build-results.txt");
 static void Build(BuildTarget target,string destination,string reportPath)
 {
  try
  {
   Physics2D.simulationMode=SimulationMode2D.FixedUpdate;
   var physics=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset");if(physics.Length>0)EditorUtility.SetDirty(physics[0]);
   PlayerSettings.companyName="AIDGH";PlayerSettings.productName="Dimension Shifter";PlayerSettings.bundleVersion="1.0.1-demo";
   PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
   PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
   var scenes=new[]{"Assets/Scenes/PaintedDimension.unity","Assets/Scenes/SampleScene.unity"};EditorBuildSettings.scenes=Array.ConvertAll(scenes,p=>new EditorBuildSettingsScene(p,true));AssetDatabase.SaveAssets();
   Directory.CreateDirectory(Path.GetDirectoryName(destination));
   var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=destination,target=target,options=BuildOptions.None});
   File.WriteAllText(reportPath,result.summary.result+"\nErrors: "+result.summary.totalErrors+"\nBytes: "+result.summary.totalSize+"\nDuration: "+result.summary.totalTime+"\n");
   EditorApplication.Exit(result.summary.result==BuildResult.Succeeded?0:1);
  }catch(Exception e){File.WriteAllText(reportPath,e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
 }
}
