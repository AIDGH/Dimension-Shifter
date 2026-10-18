using System;using System.IO;using UnityEditor;using UnityEditor.Build;using UnityEditor.Build.Reporting;using UnityEngine;
public static class HollowBuild
{
 public static void Mac()=>Build(BuildTarget.StandaloneOSX,"Builds/Mac/Dimension Shifter - Hollow Edition.app","hollow-mac-build.txt");
 static void Build(BuildTarget target,string destination,string report){try{
 Physics2D.simulationMode=SimulationMode2D.FixedUpdate;PlayerSettings.companyName="AIDGH";PlayerSettings.productName="Dimension Shifter - Hollow Edition";PlayerSettings.bundleVersion="0.2-hollow-preview";PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;string[] scenes={"Assets/Scenes/HollowGreenpath.unity"};EditorBuildSettings.scenes=Array.ConvertAll(scenes,p=>new EditorBuildSettingsScene(p,true));AssetDatabase.SaveAssets();Directory.CreateDirectory(Path.GetDirectoryName(destination));var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=destination,target=target,options=BuildOptions.None});File.WriteAllText(report,r.summary.result+"\nErrors: "+r.summary.totalErrors+"\nBytes: "+r.summary.totalSize+"\nDuration: "+r.summary.totalTime+"\n");EditorApplication.Exit(r.summary.result==BuildResult.Succeeded?0:1);
 }catch(Exception e){File.WriteAllText(report,e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
}
