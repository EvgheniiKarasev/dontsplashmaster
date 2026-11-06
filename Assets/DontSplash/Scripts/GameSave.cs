using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
#if UNITY_WEBGL
using Playgama;
#endif
namespace DontSplash {
// One versioned record keeps the economy, rewards and progress atomic. Web builds
// use Bridge exclusively; a failed read must never overwrite a player's save.
public static class GameSave {
 const string StorageKey="dontsplash.save.v1";
 static readonly Dictionary<string,string> values=new Dictionary<string,string>();
 static bool ready,dirty,writing;static int revision;static float retryAt;
 public static bool LoadFailed {get;private set;}
 public static bool SaveFailed {get;private set;}
 public static void Initialize(Action<bool> completed){
  ready=dirty=writing=LoadFailed=SaveFailed=false;revision=0;retryAt=0;values.Clear();
#if UNITY_WEBGL
  Bridge.storage.Get(new List<string>{StorageKey},(success,data)=>{
   if(!success||data==null||data.Count!=1){LoadFailed=true;completed(false);return;}
   try{Decode(data[0]);ready=true;completed(true);}
   catch(Exception e){LoadFailed=true;Debug.LogWarning("Could not read Playgama progress: "+e.Message);completed(false);}
  });
#else
  ready=true;completed(true);
#endif
 }
 static void Decode(string data){
  if(string.IsNullOrEmpty(data))return;
  var lines=data.Split('\n');if(lines[0]!="v1")throw new FormatException("Unknown save version");
  foreach(var line in lines){
   if(line=="v1"||line.Length==0)continue;
   int split=line.IndexOf('=');float number;
   if(split<=0||!float.TryParse(line.Substring(split+1),NumberStyles.Float,CultureInfo.InvariantCulture,out number)||float.IsNaN(number)||float.IsInfinity(number))throw new FormatException("Invalid save entry");
   values[line.Substring(0,split)]=line.Substring(split+1);
  }
 }
 public static int GetInt(string key,int fallback=0){
#if UNITY_WEBGL
  string value;int number;return values.TryGetValue(key,out value)&&int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out number)?number:fallback;
#else
  return PlayerPrefs.GetInt(key,fallback);
#endif
 }
 public static float GetFloat(string key,float fallback=0){
#if UNITY_WEBGL
  string value;float number;return values.TryGetValue(key,out value)&&float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out number)?number:fallback;
#else
  return PlayerPrefs.GetFloat(key,fallback);
#endif
 }
 static void Set(string key,string value){if(!ready)return;string old;if(values.TryGetValue(key,out old)&&old==value)return;values[key]=value;dirty=true;revision++;}
 public static void SetInt(string key,int value){
#if UNITY_WEBGL
  Set(key,value.ToString(CultureInfo.InvariantCulture));
#else
  PlayerPrefs.SetInt(key,value);
#endif
 }
 public static void SetFloat(string key,float value){
  if(float.IsNaN(value)||float.IsInfinity(value))return;
#if UNITY_WEBGL
  Set(key,value.ToString("R",CultureInfo.InvariantCulture));
#else
  PlayerPrefs.SetFloat(key,value);
#endif
 }
 public static void DeleteKey(string key){
#if UNITY_WEBGL
  if(ready&&values.Remove(key)){dirty=true;revision++;}
#else
  PlayerPrefs.DeleteKey(key);
#endif
 }
 public static void Save(){
#if !UNITY_WEBGL
  PlayerPrefs.Save();
#endif
  // Web writes are coalesced by Tick, including changes in the same frame.
 }
 public static void Tick(float now){
#if UNITY_WEBGL
  if(!ready||!dirty||writing||now<retryAt)return;
  var record=new StringBuilder("v1\n");foreach(var entry in values)record.Append(entry.Key).Append('=').Append(entry.Value).Append('\n');
  int sentRevision=revision;writing=true;
  Bridge.storage.Set(StorageKey,record.ToString(),success=>{
   writing=false;SaveFailed=!success;
   if(success&&revision==sentRevision)dirty=false;
   retryAt=Time.unscaledTime+(success?0:5);
  });
#endif
 }
}
}
