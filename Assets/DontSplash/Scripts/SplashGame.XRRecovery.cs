using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace DontSplash {
public sealed partial class SplashGame {
 InputSettings vrFocusSettings;InputSettings.BackgroundBehavior vrPreviousFocusPolicy;
 bool xrUserPresent=true,vrHandoffPending,vrRecoveryPoseReady;
 float vrRecoveryStableTime;Vector3 vrRecoveryPosition;Quaternion vrRecoveryRotation;
 readonly Queue<string> vrSessionTrace=new Queue<string>();string vrTraceState;float vrTraceWriteAt,vrTraceSampleAt;bool vrTraceDirty;
 void ConfigureVRFocusPolicy(){
  if(!vrFocusSettings){vrFocusSettings=InputSystem.settings;vrPreviousFocusPolicy=vrFocusSettings.backgroundBehavior;}
  // OpenXR focus/presence gates gameplay. Window focus must not independently
  // disable the HMD/controllers and leave their actions stuck at reset values.
  InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
 }
 void DisposeVRFocusPolicy(){
  if(vrFocusSettings&&InputSystem.settings==vrFocusSettings&&vrFocusSettings.backgroundBehavior==InputSettings.BackgroundBehavior.IgnoreFocus)
   vrFocusSettings.backgroundBehavior=vrPreviousFocusPolicy;
  vrFocusSettings=null;
 }
 void SetXRUserPresence(bool present){
  if(xrUserPresent==present)return;xrUserPresent=present;
  if(!present)BeginVRHandoff();
 }
 void BeginVRHandoff(){
  if(!vr)return;vrHandoffPending=true;vrRecoveryPoseReady=false;vrRecoveryStableTime=0;
  ClearGameplayInput();CancelVRThrows();CancelVRPointer();ResetVRMenuInput();vrInputReady=false;
  if(vrComfortMaterial)vrComfortMaterial.SetFloat("_Fade",1);vrFade=1;
 }
 void OnVRTrackingOriginChanged(XRInputSubsystem subsystem){BeginVRHandoff();RefreshSystemPause();}
 bool ReadValidVRHeadPose(out Vector3 position,out Quaternion rotation){
  position=VA("HeadPosition").ReadValue<Vector3>();rotation=VA("HeadRotation").ReadValue<Quaternion>();
  float norm=Quaternion.Dot(rotation,rotation);
  return VRHeld("HeadTracked")&&Finite(position.x)&&Finite(position.y)&&Finite(position.z)&&Finite(norm)&&norm>.5f&&norm<1.5f;
 }
 static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
 void StepVRHandoff(float dt){
  if(!vrHandoffPending)return;
  if(applicationPaused||!xrUserPresent||!xrInputFocused||!vrTrackingAvailable||!ReadValidVRHeadPose(out var position,out var rotation)){
   vrRecoveryPoseReady=false;vrRecoveryStableTime=0;return;
  }
  if(!vrRecoveryPoseReady||Vector3.Distance(position,vrRecoveryPosition)>.08f||Quaternion.Angle(rotation,vrRecoveryRotation)>10f)vrRecoveryStableTime=0;
  else vrRecoveryStableTime+=dt;
  vrRecoveryPosition=position;vrRecoveryRotation=rotation;vrRecoveryPoseReady=true;
  if(vrRecoveryStableTime<.3f)return;
  // Keep the avatar on its platform when a new wearer stands elsewhere.
  // Head height remains real; never smooth/freeze the tracked camera itself.
  vrOrigin.position+=Vector3.ProjectOnPlane(body.transform.position-eye.transform.position,Vector3.up);
  vrHandoffPending=false;vrPanelMode=(Mode)(-1);vrInputReady=false;ResetVRMenuInput();
 }
 void TraceVRSession(){
  if(Time.unscaledTime<vrTraceSampleAt)return;vrTraceSampleAt=Time.unscaledTime+.2f;
  string state=$"mode={mode} paused={applicationPaused} window={applicationFocused} xr={xrInputFocused} present={xrUserPresent} head={vrTrackingAvailable} left={VRHeld("LeftTracked")} right={VRHeld("RightTracked")} headDevice={VA("HeadTracked").activeControl?.device.enabled} rightDevice={VA("RightTracked").activeControl?.device.enabled} suspended={systemSuspended} recovering={vrHandoffPending} input={vrInputReady} policy={InputSystem.settings.backgroundBehavior}";
  if(state!=vrTraceState){
   vrTraceState=state;vrTraceDirty=true;string line=DateTime.UtcNow.ToString("O")+" "+state;vrSessionTrace.Enqueue(line);while(vrSessionTrace.Count>160)vrSessionTrace.Dequeue();Debug.Log("[Quest session] "+state);
  }
  if(!vrTraceDirty)return;
  if(Time.unscaledTime<vrTraceWriteAt)return;vrTraceWriteAt=Time.unscaledTime+1;
  try{File.WriteAllLines(Path.Combine(Application.persistentDataPath,"quest-session.log"),vrSessionTrace);vrTraceDirty=false;}catch(IOException){}catch(UnauthorizedAccessException){}
 }
}
}
