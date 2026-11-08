using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DontSplash {
public static partial class QuestBuild {
 internal const string ReleaseFolder="Builds/MetaRelease";
 internal static bool PreparingRelease;
 [Serializable]class SigningCredentials {public string storePassword;public string keyPassword;public string alias;}
 [MenuItem("Dont Splash/VR/Build store release APK")]
 public static void BuildRelease(){
  if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)throw new Exception("Switch to Android before building the store APK.");
  string key=Path.GetFullPath("Signing/dontsplash-release.keystore");
  string credentialsPath="Signing/credentials.json";
  if(!File.Exists(key)||!File.Exists(credentialsPath))throw new Exception("Permanent release signing files are missing. Restore the existing signing backup; do not create a replacement key for an existing store app.");
  var credentials=JsonUtility.FromJson<SigningCredentials>(File.ReadAllText(credentialsPath));
  if(string.IsNullOrEmpty(credentials?.storePassword)||string.IsNullOrEmpty(credentials.keyPassword)||string.IsNullOrEmpty(credentials.alias))throw new Exception("Release signing credentials are incomplete.");
  Directory.CreateDirectory(ReleaseFolder);File.WriteAllText(ReleaseFolder+"/build-result.txt","BUILDING RELEASE APK");
  Configure();
  PlayerSettings.bundleVersion="1.0.1";PlayerSettings.Android.bundleVersionCode=5;
  PlayerSettings.Android.useCustomKeystore=true;PlayerSettings.Android.keystoreName=key;
  PlayerSettings.Android.keyaliasName=credentials.alias;PlayerSettings.Android.keystorePass=credentials.storePassword;PlayerSettings.Android.keyaliasPass=credentials.keyPassword;
  PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android,Il2CppCompilerConfiguration.Release);
  EditorUserBuildSettings.development=false;EditorUserBuildSettings.allowDebugging=false;EditorUserBuildSettings.connectProfiler=false;EditorUserBuildSettings.buildWithDeepProfilingSupport=false;
  var icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/DontSplash/Editor/Branding/QuestIcon.png");
  if(!icon)throw new Exception("Release app icon is missing.");
  PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown,new[]{icon});
  var projectConfig=OVRProjectConfig.CachedProjectConfig;
  projectConfig.systemSplashScreen=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/DontSplash/Editor/Branding/QuestLoading.png");
  if(!projectConfig.systemSplashScreen)throw new Exception("System loading image is missing.");
  projectConfig.systemSplashScreenType=OVRProjectConfig.SystemSplashScreenType.Mono;
  projectConfig.systemLoadingScreenBackground=OVRProjectConfig.SystemLoadingScreenBackground.Black;
  OVRProjectConfig.CommitProjectConfig(projectConfig);
  var snapshots=new Dictionary<UnityEngine.Object,string>();
  foreach(var path in new[]{"Assets/Resources/DevAgentSettings.asset","Assets/Resources/ImmersiveDebuggerSettings.asset"}){
   var asset=AssetDatabase.LoadMainAssetAtPath(path);if(asset)snapshots[asset]=EditorJsonUtility.ToJson(asset);
  }
  AssetDatabase.SaveAssets();PreparingRelease=true;
  try{
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/DontSplash.unity"},locationPathName=ReleaseFolder+"/DontGetWet-1.0.1-Quest.apk",target=BuildTarget.Android,options=BuildOptions.None});
   File.WriteAllText(ReleaseFolder+"/build-result.txt",report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nSize: "+report.summary.totalSize+"\nTime: "+report.summary.totalTime);
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Store APK build failed: "+report.summary.result);
  }finally{
   PreparingRelease=false;PlayerSettings.Android.keystorePass="";PlayerSettings.Android.keyaliasPass="";
   foreach(var pair in snapshots){EditorJsonUtility.FromJsonOverwrite(pair.Value,pair.Key);EditorUtility.SetDirty(pair.Key);}AssetDatabase.SaveAssets();
  }
 }
}

// Meta SDK injects editor connection settings in callback 1 even when its agent
// is disabled. Clear them after all SDK preprocessors, before asset packaging.
public sealed class QuestReleaseSanitizer:IPreprocessBuildWithReport {
 public int callbackOrder=>int.MaxValue;
 public void OnPreprocessBuild(BuildReport report){
  if(!QuestBuild.PreparingRelease)return;
  ClearSettings("Assets/Resources/DevAgentSettings.asset",new[]{"enabled","useVoiceSdkForInput"},new[]{"serverAddress","accessToken","witClientAccessToken"});
  ClearSettings("Assets/Resources/ImmersiveDebuggerSettings.asset",new[]{"immersiveDebuggerEnabled","immersiveDebuggerDisplayAtStartup"},Array.Empty<string>());
  AssetDatabase.SaveAssets();Debug.Log("QUEST RELEASE: development tools disabled; editor connection credentials excluded.");
 }
 static void ClearSettings(string path,string[] booleans,string[] strings){
  var asset=AssetDatabase.LoadMainAssetAtPath(path);if(!asset)return;var serialized=new SerializedObject(asset);
  foreach(string name in booleans){var property=serialized.FindProperty(name);if(property!=null)property.boolValue=false;}
  foreach(string name in strings){var property=serialized.FindProperty(name);if(property!=null)property.stringValue="";}
  serialized.ApplyModifiedPropertiesWithoutUndo();
 }
}
}
