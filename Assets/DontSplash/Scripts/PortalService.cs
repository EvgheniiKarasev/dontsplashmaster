using System;
using CrazyGames;
using UnityEngine;

namespace DontSplash {
public sealed class PortalService : MonoBehaviour {
 bool ready, active, initialized, userMuted, platformMuted;
 public event Action AccountChanged;
 public bool InitializationComplete { get; private set; }
 public bool Busy { get; private set; }
 public void Initialize() {
  if(initialized)return;
  initialized=true;
  if(!CrazySDK.IsAvailable){InitializationComplete=true;return;}
  CrazySDK.Init(()=>{
   if(!this)return;
   ready=true;
   ApplySettings(CrazySDK.Game.Settings);
   CrazySDK.Game.AddSettingsChangeListener(ApplySettings);
   CrazySDK.User.AddAuthListener(OnAccountChanged);
   InitializationComplete=true;
   if(active)CrazySDK.Game.GameplayStart();
  });
 }
 void OnAccountChanged(PortalUser user){AccountChanged?.Invoke();}
 public bool IsTouchDevice {get{if(!ready)return false;string type=CrazySDK.User.SystemInfo?.device?.type;return type=="mobile"||type=="tablet";}}
 void ApplySettings(GameSettings settings){platformMuted=settings!=null&&settings.muteAudio;ApplyAudio();}
 void ApplyAudio(){AudioListener.volume=Busy||userMuted||platformMuted?0:1;}
 public void SetMuted(bool muted){userMuted=muted;ApplyAudio();}
 // Unity loading is measured by the host loader, not loadingStart/loadingStop.
 public void StartPlay(){if(active)return;active=true;if(ready)CrazySDK.Game.GameplayStart();}
 public void StopPlay(){if(!active)return;active=false;if(ready)CrazySDK.Game.GameplayStop();}
 public void Celebrate(){if(ready)CrazySDK.Game.HappyTime();}
 // CrazyGames has no equivalent of arbitrary Poki measure events.
 [System.Diagnostics.Conditional("UNITY_EDITOR")]
 public void Measure(string category,string what,string action){Debug.Log($"[DontSplash] {category}/{what}/{action}");}
 public void ContinueImmediately(Action next){if(Busy)return;AudioListener.pause=false;ApplyAudio();next?.Invoke();}
 // First CrazyGames release has no ad requests.
 void OnDestroy(){
  if(ready){StopPlay();CrazySDK.Game.RemoveSettingsChangeListener(ApplySettings);CrazySDK.User.RemoveAuthListener(OnAccountChanged);}
 }
}
}
