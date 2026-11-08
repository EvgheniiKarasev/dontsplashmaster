using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.OpenXR;

namespace DontSplash {
public static partial class QuestBuild {
 [InitializeOnLoadMethod]static void WatchRequests(){EditorApplication.update+=()=>{
  if(BuildPipeline.isBuildingPlayer||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
  if(!File.Exists("Temp/QuestBuild.request"))return;
  if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android){
   if(!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,BuildTarget.Android)){
    File.Delete("Temp/QuestBuild.request");Directory.CreateDirectory("Builds/Quest");File.WriteAllText("Builds/Quest/build-result.txt","FAILED: could not switch to Android.");
   }
   return;
  }
  bool release=File.ReadAllText("Temp/QuestBuild.request").Trim()=="RELEASE";
  File.Delete("Temp/QuestBuild.request");try{if(release)BuildRelease();else Build();}catch(Exception e){Debug.LogException(e);string folder=release?ReleaseFolder:"Builds/Quest";Directory.CreateDirectory(folder);File.WriteAllText(folder+"/build-result.txt",e.ToString());}
 };}
 [MenuItem("Dont Splash/VR/Configure Quest")]
 public static void Configure(){
  QuestHands.Prepare();
  PlayerSettings.productName="Don't Get Wet!";
  var target=NamedBuildTarget.Android;
  PlayerSettings.WebGL.template="APPLICATION:Default";
  PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.IL2CPP);
  PlayerSettings.SetManagedStrippingLevel(target,ManagedStrippingLevel.Low);
  PlayerSettings.SetApplicationIdentifier(target,"com.dontsplash.quest");
  PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
  PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel32;
  PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel34;
  PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
  PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.Vulkan});
  PlayerSettings.colorSpace=ColorSpace.Linear;
  EditorUserBuildSettings.buildAppBundle=false;
  EditorUserBuildSettings.androidBuildSubtarget=MobileTextureSubtarget.ASTC;
  var xr=AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>("Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
  foreach(var group in new[]{BuildTargetGroup.Android,BuildTargetGroup.Standalone}){
   if(!xr.HasSettingsForBuildTarget(group))xr.CreateDefaultSettingsForBuildTarget(group);
   if(!xr.HasManagerSettingsForBuildTarget(group))xr.CreateDefaultManagerSettingsForBuildTarget(group);
   var general=xr.SettingsForBuildTarget(group);general.InitManagerOnStart=true;
   XRPackageMetadataStore.AssignLoader(general.Manager,"UnityEngine.XR.OpenXR.OpenXRLoader",group);
   EditorUtility.SetDirty(general);EditorUtility.SetDirty(general.Manager);
   var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(group);
   settings.renderMode=OpenXRSettings.RenderMode.SinglePassInstanced;
   foreach(var feature in settings.GetFeatures<UnityEngine.XR.OpenXR.Features.OpenXRFeature>()){
    string type=feature.GetType().Name;
    if(type=="MetaXRFeature"||type=="OculusTouchControllerProfile"||type=="MetaQuestTouchPlusControllerProfile"||type=="MetaQuestTouchProControllerProfile")feature.enabled=true;
    // SpaceWarp needs a separate motion-vector integration; do not enable it accidentally.
    if(type.Contains("SpaceWarp"))feature.enabled=false;
    EditorUtility.SetDirty(feature);
   }
   EditorUtility.SetDirty(settings);
  }
  EditorUtility.SetDirty(xr);
  AssetDatabase.ImportAsset("Assets/Resources/QuestControls.inputactions",ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
  var controls=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Resources/QuestControls.inputactions");
  if(!controls)throw new Exception("Quest input actions could not be imported.");
  var preloaded=PlayerSettings.GetPreloadedAssets().Where(a=>a).ToList();
  if(!preloaded.Contains(controls))preloaded.Add(controls);PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
  // Shader.Find is used at runtime, so retain these shaders in the player.
  var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
  // Runtime-created rooms enable linear fog after scene load. Automatic
  // stripping only sees the saved scene (fog off), so retain this variant.
  graphics.FindProperty("m_FogStripping").intValue=1;
  graphics.FindProperty("m_FogKeepLinear").boolValue=true;
  graphics.FindProperty("m_FogKeepExp").boolValue=false;
  graphics.FindProperty("m_FogKeepExp2").boolValue=false;
  var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
  foreach(var name in new[]{"DontSplash/VRComfort","DontSplash/VRUI","DontSplash/Atmospheric Effects","TextMeshPro/Mobile/Distance Field","TextMeshPro/Mobile/Distance Field Overlay","Universal Render Pipeline/Unlit"}){
   var shader=Shader.Find(name);if(!shader)throw new Exception("Missing shader: "+name);
   bool found=false;for(int i=0;i<shaders.arraySize;i++)found|=shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader;
   if(!found){int index=shaders.arraySize++;shaders.GetArrayElementAtIndex(index).objectReferenceValue=shader;}
  }
  graphics.ApplyModifiedPropertiesWithoutUndo();
  var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
  GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
  pipeline.supportsHDR=false;pipeline.msaaSampleCount=4;pipeline.renderScale=1;
  EditorUtility.SetDirty(pipeline);
  AssetDatabase.SaveAssets();
  AssetDatabase.ImportAsset("Assets/XR/Settings/OpenXRPackageSettings.asset",ImportAssetOptions.ForceUpdate);
  Debug.Log("QUEST CONFIGURATION COMPLETE");
 }
 [MenuItem("Dont Splash/VR/Build Quest APK")]
 public static void Build(){
  if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)throw new Exception("Switch the active platform to Android before building Quest.");
  PlayerSettings.bundleVersion="0.1.2";PlayerSettings.Android.bundleVersionCode=3;
  Directory.CreateDirectory("Builds/Quest");File.WriteAllText("Builds/Quest/build-result.txt","BUILDING Quest APK");Configure();
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
   scenes=new[]{"Assets/Scenes/DontSplash.unity"},locationPathName="Builds/Quest/DontSplash-Quest.apk",
   target=BuildTarget.Android,options=BuildOptions.None
  });
  File.WriteAllText("Builds/Quest/build-result.txt",report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nSize: "+report.summary.totalSize+"\nTime: "+report.summary.totalTime);
  if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Quest build failed: "+report.summary.result);
 }
}
}
