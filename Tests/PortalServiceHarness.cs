// Dependency-isolated regression checks; compile with the production PortalService.cs and GameSave.cs.
using System;
using System.Collections.Generic;
using System.Reflection;
using DontSplash;
using CrazyGames;
using UnityEngine;
namespace UnityEngine {
 public class MonoBehaviour {public static implicit operator bool(MonoBehaviour v)=>v!=null;}
 public static class AudioListener {public static bool pause;public static float volume=1;}
 public static class Debug {public static void Log(object value){} }
 public static class PlayerPrefs {
  public static readonly Dictionary<string,object> Values=new Dictionary<string,object>();
  public static int GetInt(string k,int d=0)=>Values.ContainsKey(k)?(int)Values[k]:d;
  public static float GetFloat(string k,float d=0)=>Values.ContainsKey(k)?(float)Values[k]:d;
  public static void SetInt(string k,int v){Values[k]=v;} public static void SetFloat(string k,float v){Values[k]=v;}
  public static void DeleteKey(string k){Values.Remove(k);}public static void Save(){}
 }
}
namespace CrazyGames {
 public class PortalUser {} public class GameSettings {public bool muteAudio;}
 public class Device {public string type="desktop";}public class SystemInfo {public Device device=new Device();}
 public enum CrazyAdType {Midgame}
 public static class CrazySDK {
  public static bool IsAvailable=true,IsInitialized;public static Action InitCallback;
  public static GameModule Game=new GameModule();public static AdModule Ad=new AdModule();
  public static DataModule Data=new DataModule();public static UserModule User=new UserModule();
  public static void Init(Action cb){InitCallback=cb;}
 }
 public class UserModule {public SystemInfo SystemInfo=new SystemInfo();public Action<PortalUser> Listener;public void AddAuthListener(Action<PortalUser> a){Listener+=a;}public void RemoveAuthListener(Action<PortalUser> a){Listener-=a;}}
 public class GameModule {
  public GameSettings Settings=new GameSettings();public int Starts,Stops;public Action<GameSettings> Listener;
  public void AddSettingsChangeListener(Action<GameSettings> a){Listener+=a;}public void RemoveSettingsChangeListener(Action<GameSettings> a){Listener-=a;}
  public void GameplayStart(){Starts++;}public void GameplayStop(){Stops++;}public void HappyTime(){}
 }
 public class AdModule {
  public int Requests;public Action Finished;public Action<string> Error;
  public void RequestAd(CrazyAdType t,Action start,Action<string> error,Action finish){Requests++;Error=error;Finished=finish;start();}
 }
 public class DataModule {
  public Dictionary<string,object> Values=new Dictionary<string,object>();
  public int GetInt(string k,int d=0)=>Values.ContainsKey(k)?(int)Values[k]:d;
  public float GetFloat(string k,float d=0)=>Values.ContainsKey(k)?(float)Values[k]:d;
  public void SetInt(string k,int v){Values[k]=v;}public void SetFloat(string k,float v){Values[k]=v;}
  public void DeleteKey(string k){Values.Remove(k);}
 }
}
class PortalServiceHarness {
 static int checks;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
 static void Main(){
  var p=new PortalService();p.Initialize();Check(!p.InitializationComplete,"wait for async SDK");
  p.StartPlay();p.StartPlay();Check(CrazySDK.Game.Starts==0,"no calls before init");
  CrazySDK.IsInitialized=true;CrazySDK.InitCallback();Check(p.InitializationComplete&&CrazySDK.Game.Starts==1,"flush active gameplay once");
  p.StopPlay();p.StopPlay();Check(CrazySDK.Game.Stops==1,"idempotent gameplay stop");
  int done=0;AudioListener.pause=true;p.ContinueImmediately(()=>done++);
  Check(!p.Busy&&!AudioListener.pause&&done==1&&CrazySDK.Ad.Requests==0,"first release continues immediately without ads");
  p.SetMuted(true);p.ContinueImmediately(()=>done++);Check(AudioListener.volume==0&&done==2,"preserve player mute on continue");
  CrazySDK.Game.Listener(new GameSettings{muteAudio=true});p.SetMuted(false);Check(AudioListener.volume==0,"platform mute overrides player toggle");
  CrazySDK.Game.Listener(new GameSettings{muteAudio=false});Check(AudioListener.volume==1,"platform unmute");
  int auth=0;p.AccountChanged+=()=>auth++;CrazySDK.User.Listener(new PortalUser());Check(auth==1,"auth event forwarded");
  CrazySDK.User.SystemInfo.device.type="tablet";Check(p.IsTouchDevice,"tablet input");
  GameSave.Initialize();PlayerPrefs.SetInt("coins",999);Check(GameSave.GetInt("coins",80)==80,"do not leak local progress into cloud");
  GameSave.SetInt("coins",42);GameSave.SetFloat("time",12.5f);Check(GameSave.GetInt("coins")==42&&GameSave.GetFloat("time")==12.5f,"cloud roundtrip");
  GameSave.DeleteKey("coins");Check(GameSave.GetInt("coins",80)==80,"cloud delete");
  typeof(PortalService).GetMethod("OnDestroy",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,null);Check(CrazySDK.Game.Listener==null&&CrazySDK.User.Listener==null,"unsubscribe on destroy");
  CrazySDK.IsAvailable=false;var local=new PortalService();local.Initialize();Check(local.InitializationComplete,"native startup");
  local.ContinueImmediately(()=>done++);Check(done==3&&!local.Busy&&CrazySDK.Ad.Requests==0,"native continuation without ads");GameSave.Initialize();GameSave.SetInt("coins",9);Check(PlayerPrefs.GetInt("coins")==9,"native save fallback");
  Console.WriteLine("PASS: "+checks+" portal/save regression checks (SDK and Unity dependencies simulated)");
 }
}
