using System;using System.IO;using UnityEditor;using UnityEditor.Build;using UnityEditor.Build.Reporting;using UnityEngine;
public static class ReleaseBuild
{
 public static void MainMac()=>Build(false,BuildTarget.StandaloneOSX,"Builds/ReleaseMac/Dimension Shifter.app","release-mac.txt");
 public static void MainWindows()=>Build(false,BuildTarget.StandaloneWindows64,"Builds/ReleaseWindows/DimensionShifter.exe","release-windows.txt");
 public static void HollowMac()=>Build(true,BuildTarget.StandaloneOSX,"Builds/ReleaseHollowMac/Dimension Shifter - Hollow Edition.app","release-hollow-mac.txt");
 static void Build(bool hollow,BuildTarget target,string destination,string report){try{
 Physics2D.simulationMode=SimulationMode2D.FixedUpdate;PlayerSettings.companyName="AIDGH";PlayerSettings.productName=hollow?"Dimension Shifter - Hollow Edition":"Dimension Shifter";PlayerSettings.bundleVersion=hollow?"0.3-hollow-preview":"1.1.0-demo";
 PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
 var scenes=new[]{hollow?"Assets/Scenes/HollowGreenpath.unity":"Assets/Scenes/PaintedDimension.unity"};EditorBuildSettings.scenes=Array.ConvertAll(scenes,p=>new EditorBuildSettingsScene(p,true));AssetDatabase.SaveAssets();Directory.CreateDirectory(Path.GetDirectoryName(destination));
 var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=destination,target=target,options=BuildOptions.None});File.WriteAllText(report,r.summary.result+"\nErrors: "+r.summary.totalErrors+"\nBytes: "+r.summary.totalSize+"\nDuration: "+r.summary.totalTime+"\n");EditorApplication.Exit(r.summary.result==BuildResult.Succeeded?0:1);
 }catch(Exception e){File.WriteAllText(report,e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
}
