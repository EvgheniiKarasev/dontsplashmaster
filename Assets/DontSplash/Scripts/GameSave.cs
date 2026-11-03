using CrazyGames;
using UnityEngine;
namespace DontSplash {
// Select once at startup; never mix local and cloud saves mid-session.
public static class GameSave {
 static bool cloud;
 public static void Initialize(){cloud=CrazySDK.IsAvailable&&CrazySDK.IsInitialized;}
 public static int GetInt(string key,int fallback=0)=>cloud?CrazySDK.Data.GetInt(key,fallback):PlayerPrefs.GetInt(key,fallback);
 public static float GetFloat(string key,float fallback=0)=>cloud?CrazySDK.Data.GetFloat(key,fallback):PlayerPrefs.GetFloat(key,fallback);
 public static void SetInt(string key,int value){if(cloud)CrazySDK.Data.SetInt(key,value);else PlayerPrefs.SetInt(key,value);}
 public static void SetFloat(string key,float value){if(cloud)CrazySDK.Data.SetFloat(key,value);else PlayerPrefs.SetFloat(key,value);}
 public static void DeleteKey(string key){if(cloud)CrazySDK.Data.DeleteKey(key);else PlayerPrefs.DeleteKey(key);}
 // The Data module persists writes automatically.
 public static void Save(){if(!cloud)PlayerPrefs.Save();}
}
}
