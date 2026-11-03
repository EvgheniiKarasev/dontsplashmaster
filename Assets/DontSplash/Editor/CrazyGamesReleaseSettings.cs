using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
namespace DontSplash {
public static class CrazyGamesReleaseSettings {
 [InitializeOnLoadMethod]static void Init(){EditorApplication.delayCall+=()=>{if(!File.Exists("Temp/CrazyGamesSettings.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;Apply();File.Delete("Temp/CrazyGamesSettings.request");};}
 [MenuItem("Dont Splash/Build CrazyGames release")]
 public static void Build(){
  Apply();
  const string output="Build/CrazyGames";
  var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,output,BuildTarget.WebGL,BuildOptions.None);
  if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("CrazyGames Web build failed.");
  long bytes=0;var files=Directory.GetFiles(output,"*",SearchOption.AllDirectories);
  foreach(var file in files)bytes+=new FileInfo(file).Length;
  File.WriteAllText("Build/CrazyGames-size.txt","Total files: "+files.Length+"\nTotal bytes: "+bytes+"\nInitial download must be verified in CrazyGames preview (50 MB desktop / 20 MB mobile homepage).\n");
  if(files.Length>1500||bytes>250000000)throw new BuildFailedException("CrazyGames bundle exceeds file count or total size limits.");
  Debug.Log("CrazyGames build ready: "+output+" ("+bytes+" bytes)");
 }
 [MenuItem("Dont Splash/Apply CrazyGames release settings (no build)")]
 public static void Apply(){
  var target=NamedBuildTarget.WebGL;
  PlayerSettings.productName="DONT SPLASH";
  PlayerSettings.defaultWebScreenWidth=1280;PlayerSettings.defaultWebScreenHeight=720;
  PlayerSettings.runInBackground=false;
  PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
  PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
  PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
  PlayerSettings.SplashScreen.show=false;
  PlayerSettings.WebGL.template="APPLICATION:Default";
  PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Brotli;
  PlayerSettings.WebGL.decompressionFallback=false;PlayerSettings.WebGL.dataCaching=true;
  PlayerSettings.WebGL.nameFilesAsHashes=true;PlayerSettings.WebGL.showDiagnostics=false;
  PlayerSettings.WebGL.debugSymbolMode=WebGLDebugSymbolMode.Off;
  PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
  PlayerSettings.WebGL.initialMemorySize=128;PlayerSettings.WebGL.maximumMemorySize=1024;
  PlayerSettings.WebGL.memoryGrowthMode=WebGLMemoryGrowthMode.Geometric;
  PlayerSettings.WebGL.memoryGeometricGrowthCap=64;
  PlayerSettings.WebGL.threadsSupport=false;PlayerSettings.WebGL.powerPreference=WebGLPowerPreference.HighPerformance;
  PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.IL2CPP);
  PlayerSettings.SetIl2CppCompilerConfiguration(target,Il2CppCompilerConfiguration.Release);
  PlayerSettings.SetIl2CppCodeGeneration(target,Il2CppCodeGeneration.OptimizeSize);
  PlayerSettings.SetManagedStrippingLevel(target,ManagedStrippingLevel.High);
  PlayerSettings.stripEngineCode=true;
  // Runtime materials change, and the lobby attaches lightmaps after instantiation.
  PlayerSettings.stripUnusedMeshComponents=false;
  PlayerSettings.gcIncremental=true;
  PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL,false);
  PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL,new[]{GraphicsDeviceType.OpenGLES3});
  EditorUserBuildSettings.development=false;EditorUserBuildSettings.allowDebugging=false;EditorUserBuildSettings.connectProfiler=false;
  EditorUserBuildSettings.webGLBuildSubtarget=WebGLTextureSubtarget.ETC2;
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/DontSplash.unity",true)};
  var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
  if(pipeline){
   pipeline.supportsHDR=false;pipeline.msaaSampleCount=1;pipeline.renderScale=1;
   pipeline.supportsCameraDepthTexture=true;pipeline.supportsCameraOpaqueTexture=false;
   var serialized=new SerializedObject(pipeline);serialized.FindProperty("m_MainLightShadowsSupported").boolValue=false;serialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue=false;serialized.ApplyModifiedPropertiesWithoutUndo();
   EditorUtility.SetDirty(pipeline);
  }
  int previous=QualitySettings.GetQualityLevel();QualitySettings.SetQualityLevel(0,false);
  QualitySettings.shadows=UnityEngine.ShadowQuality.Disable;QualitySettings.antiAliasing=0;QualitySettings.vSyncCount=0;
  QualitySettings.realtimeReflectionProbes=false;QualitySettings.softParticles=false;QualitySettings.anisotropicFiltering=AnisotropicFiltering.Enable;
  if(pipeline)QualitySettings.renderPipeline=pipeline;
  QualitySettings.SetQualityLevel(previous,false);
  AssetDatabase.SaveAssets();
  Directory.CreateDirectory("Logs");
  File.WriteAllText("Logs/CrazyGamesReleaseSettings.txt","APPLIED (no build)\nTemplate: "+PlayerSettings.WebGL.template+"\nCompression: "+PlayerSettings.WebGL.compressionFormat+"\nHashes: "+PlayerSettings.WebGL.nameFilesAsHashes+"\nMemory: "+PlayerSettings.WebGL.initialMemorySize+".."+PlayerSettings.WebGL.maximumMemorySize+" MB\nStripping: "+PlayerSettings.GetManagedStrippingLevel(target)+"\nDevelopment: "+EditorUserBuildSettings.development+"\nTextures: "+EditorUserBuildSettings.webGLBuildSubtarget+"\n");
 }
}
}


