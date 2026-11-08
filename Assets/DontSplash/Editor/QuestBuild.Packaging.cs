using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DontSplash {
// OVRPlugin.aar carries additional native libraries under assets/lib. Android's
// ndk.abiFilters only filters jniLibs, so it does not remove these 32-bit copies.
public sealed class QuestArm64Packaging:IPostGenerateGradleAndroidProject,IPostprocessBuildWithReport {
 public int callbackOrder=>int.MaxValue;
 const string Marker="// Quest ARM64 asset-library filter";
 public void OnPostGenerateGradleAndroidProject(string path){
  if(PlayerSettings.Android.targetArchitectures!=AndroidArchitecture.ARM64)return;
  foreach(string module in new[]{path,Path.Combine(Path.GetDirectoryName(path),"launcher")}){
   string gradle=Path.Combine(module,"build.gradle");
   if(!File.Exists(gradle))throw new BuildFailedException("Missing Android module: "+gradle);
   string content=File.ReadAllText(gradle);
   if(content.Contains(Marker))continue;
   File.AppendAllText(gradle,"\n"+Marker+"\nandroid.androidResources.ignoreAssetsPatterns.addAll(['armeabi', 'armeabi-v7a', 'x86', 'x86_64'])\n");
  }
  Debug.Log("QUEST PACKAGING: non-ARM64 SDK asset directories excluded in both Android modules.");
 }
 public void OnPostprocessBuild(BuildReport report){
  if(report.summary.platform!=BuildTarget.Android||PlayerSettings.Android.targetArchitectures!=AndroidArchitecture.ARM64||!report.summary.outputPath.EndsWith(".apk",StringComparison.OrdinalIgnoreCase))return;
  int libraries=0;
  using(var archive=ZipFile.OpenRead(report.summary.outputPath)){
   foreach(var entry in archive.Entries){
    if(!entry.FullName.EndsWith(".so",StringComparison.OrdinalIgnoreCase))continue;
    var header=new byte[20];int read=0;
    using(var stream=entry.Open()){while(read<header.Length){int count=stream.Read(header,read,header.Length-read);if(count==0)break;read+=count;}}
    if(read!=20||header[0]!=0x7f||header[1]!='E'||header[2]!='L'||header[3]!='F'||header[4]!=2||header[5]!=1||header[18]!=183||header[19]!=0)
     throw new BuildFailedException("Non-AArch64 ELF library in APK: "+entry.FullName);
    libraries++;
   }
  }
  if(libraries==0)throw new BuildFailedException("No native libraries found in Quest APK.");
  Debug.Log("QUEST PACKAGING VERIFIED: all "+libraries+" native libraries are 64-bit AArch64, including SDK assets.");
 }
}
}
