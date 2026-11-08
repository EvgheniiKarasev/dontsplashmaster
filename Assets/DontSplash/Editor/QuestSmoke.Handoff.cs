using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.OpenXR.Features.Interactions;
namespace DontSplash {
public static partial class QuestSmoke {
 static void SettleVRRecovery(SplashGame game){
  Call(game,"UpdateVRPose");Call(game,"StepVRHandoff",0f);Call(game,"StepVRHandoff",.31f);Call(game,"RefreshSystemPause");Call(game,"UpdateVRInputGate");Call(game,"UpdateVRPose");
 }
 static void CheckHeadsetHandoff(SplashGame game,XRHMD head,OculusTouchControllerProfile.OculusTouchController left,OculusTouchControllerProfile.OculusTouchController right){
  var report=new StringBuilder();var canvas=Field<Canvas>(game,"uiCanvas");var body=Field<CharacterController>(game,"body");var eye=Field<Camera>(game,"eye");
  void Frame(float dt){Call(game,"TickVR");Call(game,"StepVRHandoff",dt);Call(game,"RefreshSystemPause");Call(game,"UpdateVRPose");Call(game,"SyncUI");Call(game,"LateVR");}
  void Neutral(){Trigger(left,0);Trigger(right,0);Push(left.gripPressed,0f);Push(right.secondaryButton,0f);Push(right.primaryButton,0f);Push(left.primaryButton,0f);Push(left.menu,0f);Push(left.thumbstick,Vector2.zero);Push(right.thumbstick,Vector2.zero);}
  var settings=InputSystem.settings;var manager=typeof(InputSystem).GetField("s_Manager",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
  var focus=manager.GetType().GetMethod("OnFocusChanged",BindingFlags.Instance|BindingFlags.NonPublic);
  try{
   Check(focus!=null,"Input System focus handler missing");
   // Exercise the real package handler, not just our MonoBehaviour callbacks.
   settings.backgroundBehavior=InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices;
   focus.Invoke(manager,new object[]{false});
   report.AppendLine("Default focus policy: HMD enabled="+head.enabled+", right enabled="+right.enabled+"; background capabilities="+head.canRunInBackground+"/"+right.canRunInBackground);
   focus.Invoke(manager,new object[]{true});
   settings.backgroundBehavior=InputSettings.BackgroundBehavior.ResetAndDisableAllDevices;
   focus.Invoke(manager,new object[]{false});
   Check(!head.enabled&&!right.enabled,"Baseline focus reset did not disable XR devices");
   focus.Invoke(manager,new object[]{true});Call(game,"ConfigureVRFocusPolicy");
   focus.Invoke(manager,new object[]{false});
   Check(head.enabled&&left.enabled&&right.enabled,"Window focus still disables XR devices");
   focus.Invoke(manager,new object[]{true});
   report.AppendLine("Real Input System reset-all stress case disables XR devices; VR policy keeps HMD/controllers enabled.");
   foreach(int fps in new[]{72,90,120}){
    Neutral();Push(head.isTracked,1f);Push(head.centerEyePosition,new Vector3(0,1.65f,0));Push(head.centerEyeRotation,Quaternion.identity);Push(left.isTracked,1f);Push(right.isTracked,1f);
    Call(game,"OnApplicationPause",false);Call(game,"SetXRUserPresence",true);Call(game,"SetXRSessionFocus",true);SettleVRRecovery(game);Call(game,"Respawn");Call(game,"Resume");Frame(0);
    Vector3 avatar=body.transform.position;
    Call(game,"OnApplicationPause",true);Call(game,"OnApplicationFocus",false);Call(game,"SetXRUserPresence",false);Call(game,"SetXRSessionFocus",false);
    Push(head.isTracked,0f);Push(left.isTracked,0f);Push(right.isTracked,0f);Frame(0);
    Check(Field<bool>(game,"systemSuspended")&&!canvas.enabled&&!Field<LineRenderer>(game,"vrRay").enabled,"Unworn headset left interactive/visible handoff UI");
    // Window callback stays false while the new wearer is present and focused.
    Call(game,"OnApplicationPause",false);Call(game,"SetXRSessionFocus",true);Call(game,"SetXRUserPresence",true);Push(head.isTracked,1f);
    Push(head.centerEyePosition,new Vector3(1.3f,1.2f,-.7f));Push(head.centerEyeRotation,Quaternion.Euler(12,145,27));
    for(int i=0;i<10;i++){Call(game,"SetXRUserPresence",i%2==0);Frame(1f/fps);Check(!canvas.enabled,"Presence flicker revealed an unstable menu");}
    Call(game,"SetXRUserPresence",true);
    // Reject a zero quaternion during tracking recovery, retaining the last pose.
    Push(head.centerEyeRotation,new Quaternion(0,0,0,0));Frame(.4f);Check(Field<bool>(game,"systemSuspended"),"Invalid pose resumed handoff");
    Push(head.centerEyeRotation,Quaternion.Euler(12,145,27));for(int i=0;i<fps;i++)Frame(1f/fps);
    Check(!Field<bool>(game,"systemSuspended")&&canvas.enabled,"New wearer stayed permanently suspended");
    Check(Field<object>(game,"mode").ToString()=="Pause","Handoff resumed gameplay automatically");
    Check(Vector3.Distance(body.transform.position,avatar)<.001f,"Handing headset moved avatar off platform");
    Check(Vector3.ProjectOnPlane(eye.transform.position-avatar,Vector3.up).magnitude<.001f,"New wearer retained old room-scale offset");
    Check(Vector3.Dot(canvas.transform.up,Vector3.up)>.999f,"Handoff panel inherited headset roll");
    Vector3 ahead=Vector3.ProjectOnPlane(eye.transform.forward,Vector3.up).normalized;
    Check(Vector3.Distance(canvas.transform.position,eye.transform.position+ahead*1.5f)<.001f,"Menu did not reanchor for new wearer");
    Vector3 anchor=canvas.transform.position;
    Push(head.centerEyePosition,new Vector3(1.32f,1.23f,-.68f));Push(head.centerEyeRotation,Quaternion.Euler(8,152,-18));Frame(1f/fps);
    Check(Vector3.Distance(canvas.transform.position,anchor)<.001f,"Recovered menu follows head instead of staying anchored");
    Push(left.isTracked,1f);Push(right.isTracked,1f);Neutral();Frame(1f/fps);Aim(game,right,"RESUME");Click(game,right,"RESUME");
    Check(Field<object>(game,"mode").ToString()=="Play","Returned controllers cannot click RESUME");
    report.AppendLine(fps+" Hz handoff: missing controllers, stale window focus, presence flicker, invalid pose, new height/offset/yaw/roll; upright anchored menu recovers and RESUME works.");
   }
   Call(game,"OnVRTrackingOriginChanged",new object[]{null});Check(Field<bool>(game,"systemSuspended"),"Tracking-origin reset did not suspend play");
   Frame(0);SettleVRRecovery(game);Frame(0);Check(canvas.enabled&&!Field<bool>(game,"systemSuspended")&&Field<object>(game,"mode").ToString()=="Pause","Tracking-origin recovery failed");
   report.AppendLine("Tracking-origin reset recovers to a newly anchored pause panel.");
   Check(!GameObject.Find("Entrance highlight")&&!GameObject.Find("Worn edge"),"Decorative platform stripes remain");
   File.WriteAllText("Logs/Quest-handoff-checks.txt","PASS\n"+report);
  }catch(Exception e){File.WriteAllText("Logs/Quest-handoff-checks.txt","FAIL\n"+report+e);throw;}
  finally{
   focus?.Invoke(manager,new object[]{true});Call(game,"ConfigureVRFocusPolicy");Neutral();Push(head.isTracked,1f);Push(head.centerEyePosition,new Vector3(0,1.65f,0));Push(head.centerEyeRotation,Quaternion.identity);Push(left.isTracked,1f);Push(right.isTracked,1f);
   Call(game,"OnApplicationPause",false);Call(game,"OnApplicationFocus",true);Call(game,"SetXRUserPresence",true);Call(game,"SetXRSessionFocus",true);SettleVRRecovery(game);Set(game,"xrSessionOwnsFocus",false);Call(game,"Respawn");Call(game,"Resume");Frame(0);
  }
 }
}
}
