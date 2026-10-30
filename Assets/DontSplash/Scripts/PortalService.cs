using System;
using UnityEngine;
namespace DontSplash {
public sealed class PortalService : MonoBehaviour {
 bool ready, loaded, active; public bool Busy {get;private set;}
 public void Initialize() {
#if UNITY_WEBGL || UNITY_EDITOR
  PokiUnitySDK.Instance.sdkInitializedCallback=()=>{ready=true;Flush();if(active)PokiUnitySDK.Instance.gameplayStart();};
  PokiUnitySDK.Instance.init();
#else
  ready=true;
#endif
 }
 public void Loaded(){loaded=true;Flush();}
 void Flush(){
  if(!ready||!loaded)return;
#if UNITY_WEBGL || UNITY_EDITOR
  PokiUnitySDK.Instance.gameLoadingFinished();
#endif
  loaded=false;
 }
 public void StartPlay(){if(active)return;active=true;
#if UNITY_WEBGL || UNITY_EDITOR
  if(ready)PokiUnitySDK.Instance.gameplayStart();
#endif
 }
 public void StopPlay(){if(!active)return;active=false;
#if UNITY_WEBGL || UNITY_EDITOR
  if(ready)PokiUnitySDK.Instance.gameplayStop();
#endif
 }
 public void Measure(string category,string what,string action){
#if UNITY_WEBGL || UNITY_EDITOR
  if(ready)PokiUnitySDK.Instance.measure(category,what,action);
#endif
 }
 public void ContinueImmediately(Action next){if(Busy)return;AudioListener.pause=false;next();}
 public void Break(Action next){
  if(Busy)return;StopPlay();Busy=true;AudioListener.pause=true;
  Action done=()=>{Busy=false;AudioListener.pause=false;next();};
#if UNITY_EDITOR
  if(ready)PokiUnitySDK.Instance.commercialBreak();done();
#elif UNITY_WEBGL
  if(ready){PokiUnitySDK.Instance.commercialBreakCallBack=()=>done();PokiUnitySDK.Instance.commercialBreak();}
  else done();
#else
  done();
#endif
 }
}
}


