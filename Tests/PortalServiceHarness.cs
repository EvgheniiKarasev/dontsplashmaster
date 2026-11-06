using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using DontSplash;
using Playgama;
using Playgama.Modules.Platform;
using Playgama.Modules.Advertisement;
using UnityEngine;
namespace UnityEngine {
 public class MonoBehaviour {public static implicit operator bool(MonoBehaviour v)=>v!=null;}
 public static class AudioListener {public static bool pause;public static float volume=1;}
 public static class Time {public static float timeScale=1,unscaledTime;}
 public static class Application {public static bool isFocused=true,isMobilePlatform;}
 public static class Debug {public static void Log(object v){}public static void LogWarning(object v){} }
 public static class PlayerPrefs {
  static Dictionary<string,object> values=new Dictionary<string,object>();
  public static int GetInt(string k,int d=0)=>values.ContainsKey(k)?(int)values[k]:d;
  public static float GetFloat(string k,float d=0)=>values.ContainsKey(k)?(float)values[k]:d;
  public static void SetInt(string k,int v){values[k]=v;}public static void SetFloat(string k,float v){values[k]=v;}
  public static void DeleteKey(string k){values.Remove(k);}public static void Save(){}
 }
}
namespace Playgama.Modules.Platform {public enum PlatformMessage {GameReady,LevelStarted,LevelResumed,LevelPaused,LevelCompleted,LevelFailed}}
namespace Playgama.Modules.Advertisement {public enum InterstitialState {Loading,Opened,Closed,Failed}}
namespace Playgama.Modules.Device {public enum DeviceType {Desktop,Mobile,Tablet}}
namespace Playgama {
 public static class Bridge {
  public static Platform platform=new Platform();public static Storage storage=new Storage();public static Ads advertisement=new Ads();public static Device device=new Device();
 }
 public class Platform {
  public string language="en";public bool isAudioEnabled=true;public Action<bool> audioStateChanged,pauseStateChanged;
  public List<PlatformMessage> messages=new List<PlatformMessage>();
  public void SendMessage(PlatformMessage m,Dictionary<string,object> data=null){messages.Add(m);}
 }
 public class Storage {
  public int Reads,Writes;public string LastWrite;public Action<bool,List<string>> Loaded;public Action<bool> Saved;
  public void Get(List<string> keys,Action<bool,List<string>> cb){if(keys.Count!=1)throw new Exception("save must load once");Reads++;Loaded=cb;}
  public void Set(string key,string value,Action<bool> cb){Writes++;LastWrite=value;Saved=cb;}
 }
 public class Ads {
  public bool isInterstitialSupported=true,Throw;public int Requests;public InterstitialState interstitialState=InterstitialState.Closed;
  public Action<InterstitialState> interstitialStateChanged;
  public void ShowInterstitial(string placement){Requests++;if(Throw)throw new Exception("unavailable");interstitialStateChanged(InterstitialState.Loading);}
 }
 public class Device {public Playgama.Modules.Device.DeviceType type;}
}
class PortalServiceHarness {
 static int checks;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
 static void Invoke(PortalService p,string method,params object[] args){typeof(PortalService).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,args);}
 static void Main(){
#if UNITY_WEBGL
  Bridge.platform.isAudioEnabled=false;Bridge.platform.language="ru-RU";var p=new PortalService();p.Initialize();p.Initialize();
  Check(!p.InitializationComplete&&Bridge.storage.Reads==1,"wait for save, initialize only once");
  Check(AudioListener.volume==0,"apply initial platform mute");Check(p.Language=="en","English fallback for unsupported language");
  p.GameReady();Check(Bridge.platform.messages.Count==0,"no game ready before save");
  GameSave.SetInt("coins",999);GameSave.Tick(0);Check(Bridge.storage.Writes==0,"no writes before read completes");
  Bridge.storage.Loaded(true,new List<string>{"v1\ncoins=42\ntime=12.5\n"});
  Check(p.InitializationComplete&&GameSave.GetInt("coins")==42&&GameSave.GetFloat("time")==12.5f,"restore complete progress before startup");
  p.GameReady();p.GameReady();Check(Bridge.platform.messages.Count==1&&Bridge.platform.messages[0]==PlatformMessage.GameReady,"game ready exactly once");
  p.StartPlay();p.StartPlay();p.StopPlay();p.StopPlay();Check(Bridge.platform.messages.Count==3,"idempotent lifecycle");
  p.SetMuted(true);Bridge.platform.audioStateChanged(true);Check(AudioListener.volume==0,"user mute preserved after platform unmute");
  p.SetMuted(false);Check(AudioListener.volume==1,"audio restored when both allow");
  int pauses=0;p.PauseRequested+=()=>pauses++;Time.timeScale=.75f;Bridge.platform.pauseStateChanged(true);
  Check(p.Busy&&Time.timeScale==0&&AudioListener.volume==0&&pauses==1,"host pauses audio and simulation");
  Bridge.platform.pauseStateChanged(false);Check(!p.Busy&&Time.timeScale==.75f,"restore previous simulation speed");
  p.ShowBreakAd("level_completed");p.ShowBreakAd("level_completed");Check(p.Busy&&Bridge.advertisement.Requests==1,"block duplicate ads");
  int continued=0;p.ContinueImmediately(()=>continued++);Check(continued==0,"block interaction during ad");
  Bridge.platform.pauseStateChanged(true);Bridge.advertisement.interstitialStateChanged(InterstitialState.Closed);Check(p.Busy,"ad close cannot override host pause");
  Bridge.platform.pauseStateChanged(false);p.ContinueImmediately(()=>continued++);Check(continued==1&&!p.Busy,"continue after both pauses clear");
  p.ShowBreakAd("game_over");Bridge.advertisement.interstitialStateChanged(InterstitialState.Failed);p.ContinueImmediately(()=>continued++);Check(continued==2&&!p.Busy,"ad failure never blocks continuation");
  Bridge.advertisement.Throw=true;p.ShowBreakAd("game_over");Check(!p.Busy,"ad exception releases pause");Bridge.advertisement.Throw=false;
  Bridge.advertisement.isInterstitialSupported=false;int requests=Bridge.advertisement.Requests;p.ShowBreakAd("game_over");Check(!p.Busy&&Bridge.advertisement.Requests==requests,"unsupported ads skip cleanly");
  Invoke(p,"OnApplicationFocus",false);Check(p.Busy&&AudioListener.volume==0,"background is silent");Invoke(p,"OnApplicationFocus",true);Check(!p.Busy,"focus restores host state");
  Bridge.device.type=Playgama.Modules.Device.DeviceType.Tablet;Check(p.IsTouchDevice,"tablets use touch controls");
  CultureInfo.CurrentCulture=new CultureInfo("fr-FR");GameSave.SetFloat("time",13.25f);GameSave.SetInt("coins",43);GameSave.Save();GameSave.Tick(0);
  Check(Bridge.storage.Writes==1&&Bridge.storage.LastWrite.Contains("13.25"),"batch changes using invariant numbers");
  GameSave.SetInt("coins",44);GameSave.Tick(0);Check(Bridge.storage.Writes==1,"serialize concurrent save writes");Bridge.storage.Saved(true);GameSave.Tick(0);
  Check(Bridge.storage.Writes==2&&Bridge.storage.LastWrite.Contains("coins=44"),"flush changes arriving during an older save");Bridge.storage.Saved(true);
  GameSave.SetInt("coins",44);GameSave.Tick(0);Check(Bridge.storage.Writes==2,"unchanged values do not write");
  GameSave.DeleteKey("coins");GameSave.Tick(0);Check(!Bridge.storage.LastWrite.Contains("coins="),"deleted keys omitted from next atomic record");
  Bridge.storage.Saved(false);GameSave.Tick(4);Check(Bridge.storage.Writes==3&&GameSave.SaveFailed,"failed save retains data and waits before retry");Time.unscaledTime=5;GameSave.Tick(5);Check(Bridge.storage.Writes==4,"failed save retries");Bridge.storage.Saved(true);Check(!GameSave.SaveFailed,"successful retry clears failure");
  Invoke(p,"OnDestroy");Check(Bridge.platform.audioStateChanged==null&&Bridge.platform.pauseStateChanged==null&&Bridge.advertisement.interstitialStateChanged==null,"remove all listeners");
  bool loaded=true;GameSave.Initialize(ok=>loaded=ok);Bridge.storage.Loaded(false,null);GameSave.SetInt("coins",1);GameSave.Tick(100);Check(!loaded&&GameSave.LoadFailed&&Bridge.storage.Writes==4,"failed read cannot overwrite remote progress");
  GameSave.Initialize(ok=>loaded=ok);Bridge.storage.Loaded(true,new List<string>{"corrupt"});GameSave.SetInt("coins",1);GameSave.Tick(100);Check(!loaded&&GameSave.LoadFailed&&Bridge.storage.Writes==4,"malformed save protected");
  GameSave.Initialize(ok=>loaded=ok);Bridge.storage.Loaded(true,new List<string>{null});Check(loaded&&GameSave.GetInt("coins",80)==80,"new player gets defaults");
#else
  bool loaded=false;GameSave.Initialize(ok=>loaded=ok);GameSave.SetInt("coins",17);GameSave.SetFloat("time",1.25f);GameSave.Save();
  Check(loaded&&GameSave.GetInt("coins")==17&&GameSave.GetFloat("time")==1.25f,"native persistence");GameSave.DeleteKey("coins");Check(GameSave.GetInt("coins",80)==80,"native delete");
#endif
  Console.WriteLine("PASS: "+checks+" portal/save regression checks");
 }
}
