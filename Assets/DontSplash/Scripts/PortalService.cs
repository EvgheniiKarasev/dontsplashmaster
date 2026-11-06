using System;
using UnityEngine;
#if UNITY_WEBGL
using Playgama;
using Playgama.Modules.Platform;
using Playgama.Modules.Advertisement;
#endif
namespace DontSplash {
public sealed class PortalService : MonoBehaviour {
 bool initialized, active, gameReadySent, userMuted, platformMuted, hostPaused, unfocused, adPending, frozen;
 float previousTimeScale=1;
 public event Action PauseRequested;
 public bool InitializationComplete {get; private set;}
 public bool Busy=>hostPaused||unfocused||adPending;
 public string Language {get; private set;}="en";
 public bool IsTouchDevice {
  get {
#if UNITY_WEBGL
   return Bridge.device.type==Playgama.Modules.Device.DeviceType.Mobile||Bridge.device.type==Playgama.Modules.Device.DeviceType.Tablet;
#else
   return Application.isMobilePlatform;
#endif
  }
 }
 public void Initialize(){
  if(initialized)return;
  initialized=true;
  // The Bridge template initializes JavaScript before starting the Unity loader.
#if UNITY_WEBGL
  Language=(Bridge.platform.language??"en").ToLowerInvariant().Split('-')[0];
  // This release declares English only; unsupported platform languages fall back to English.
  if(Language!="en")Language="en";
  platformMuted=!Bridge.platform.isAudioEnabled;
  Bridge.platform.audioStateChanged+=OnAudioStateChanged;
  Bridge.platform.pauseStateChanged+=OnPauseStateChanged;
  Bridge.advertisement.interstitialStateChanged+=OnInterstitialStateChanged;
  adPending=Bridge.advertisement.interstitialState==InterstitialState.Opened;
#endif
  unfocused=!Application.isFocused;
  ApplyPause();
  GameSave.Initialize(success=>{if(this&&success)InitializationComplete=true;});
 }
 public void GameReady(){
  if(!InitializationComplete||gameReadySent)return;
  gameReadySent=true;
#if UNITY_WEBGL
  Bridge.platform.SendMessage(PlatformMessage.GameReady);
#endif
 }
 public void SetMuted(bool muted){userMuted=muted;ApplyAudio();}
 void OnAudioStateChanged(bool enabled){platformMuted=!enabled;ApplyAudio();}
 void OnPauseStateChanged(bool paused){hostPaused=paused;if(paused)PauseRequested?.Invoke();ApplyPause();}
 void OnApplicationFocus(bool focus){unfocused=!focus;if(!focus){PauseRequested?.Invoke();GameSave.Save();}ApplyPause();}
 void OnApplicationPause(bool paused){OnPauseStateChanged(paused);if(paused)GameSave.Save();}
 void ApplyAudio(){AudioListener.volume=Busy||userMuted||platformMuted?0:1;}
 void ApplyPause(){
  if(Busy&&!frozen){previousTimeScale=Time.timeScale;Time.timeScale=0;frozen=true;}
  else if(!Busy&&frozen){Time.timeScale=previousTimeScale;frozen=false;}
  ApplyAudio();
 }
 public void StartPlay(){
  if(active)return;active=true;
#if UNITY_WEBGL
  Bridge.platform.SendMessage(PlatformMessage.LevelResumed);
#endif
 }
 public void StopPlay(){
  if(!active)return;active=false;
#if UNITY_WEBGL
  Bridge.platform.SendMessage(PlatformMessage.LevelPaused);
#endif
 }
 public void Celebrate(){}
 public void Measure(string category,string what,string action){
#if UNITY_WEBGL
  if(category=="level"){
   var message=action=="start"?PlatformMessage.LevelStarted:action=="complete"?PlatformMessage.LevelCompleted:PlatformMessage.LevelFailed;
   Bridge.platform.SendMessage(message,new System.Collections.Generic.Dictionary<string,object>{{"level",what}});
  }
#else
  Debug.Log($"[DontSplash] {category}/{what}/{action}");
#endif
 }
 // Show on the result screen. The subsequent Resume/Next click retains its
 // browser user gesture so desktop pointer lock remains reliable after an ad.
 public void ShowBreakAd(string placement){
  GameSave.Save();
#if UNITY_WEBGL
  if(adPending||!Bridge.advertisement.isInterstitialSupported)return;
  adPending=true;ApplyPause();
  try{Bridge.advertisement.ShowInterstitial(placement);}
  catch(Exception e){Debug.LogWarning("Playgama ad unavailable: "+e.Message);adPending=false;ApplyPause();}
#endif
 }
#if UNITY_WEBGL
 void OnInterstitialStateChanged(InterstitialState state){
  adPending=state==InterstitialState.Loading||state==InterstitialState.Opened;
  ApplyPause();
 }
#endif
 public void ContinueImmediately(Action next){if(Busy)return;AudioListener.pause=false;ApplyAudio();next?.Invoke();}
 void Update(){GameSave.Tick(Time.unscaledTime);}
 void OnDestroy(){
  if(!initialized)return;
  StopPlay();GameSave.Save();
#if UNITY_WEBGL
  Bridge.platform.audioStateChanged-=OnAudioStateChanged;
  Bridge.platform.pauseStateChanged-=OnPauseStateChanged;
  Bridge.advertisement.interstitialStateChanged-=OnInterstitialStateChanged;
#endif
  if(frozen)Time.timeScale=previousTimeScale;
 }
}
}
