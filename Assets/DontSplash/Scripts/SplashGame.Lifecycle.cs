using UnityEngine;

namespace DontSplash {
public sealed partial class SplashGame {
 bool applicationFocused=true,applicationPaused,xrInputFocused=true,vrTrackingAvailable=true;
 bool systemSuspended,audioPausedBeforeSystem,vrInputReady=true,vrPreview;
 bool SystemSuspended=>systemSuspended;
 bool VRCanInteract=>!systemSuspended&&vrInputReady;

 void OnApplicationFocus(bool focus){applicationFocused=focus;RefreshSystemPause();}
 void OnApplicationPause(bool pause){applicationPaused=pause;RefreshSystemPause();}
 void SetXRInputFocus(bool focus){xrInputFocused=focus;RefreshSystemPause();}
 void PollXRInputFocus(){
  // No OVRManager is required by our OpenXR rig. Read the native input-focus
  // flag: a system overlay need not cause OnApplicationPause/Focus callbacks.
  if(vr&&!vrPreview&&OVRPlugin.initialized)SetXRInputFocus(OVRPlugin.hasInputFocus);
 }
 void RefreshSystemPause(){
  bool suspended=applicationPaused||!applicationFocused||(vr&&(!xrInputFocused||!vrTrackingAvailable));
  if(suspended==systemSuspended)return;
  systemSuspended=suspended;
  if(suspended){
   audioPausedBeforeSystem=AudioListener.pause;
   // Ordinary play resumes through RESUME. Caught/shop/other modes keep their
   // exact state; all their update/input paths are gated until focus returns.
   Pause();AudioListener.pause=true;
   ClearGameplayInput();CancelVRThrows();CancelVRPointer();vrInputReady=false;vrSnapReady=false;
   HideVRInteraction();
  }else{
   AudioListener.pause=audioPausedBeforeSystem||mode==Mode.Pause;
   vrPanelMode=(Mode)(-1);
  }
 }
 void ClearGameplayInput(){
  moveFinger=lookFinger=useFinger=-1;move=look=Vector2.zero;
  throwInput=flareInput=jump=use=false;jumpBuffer=0;hold=0;
 }
 void HideVRInteraction(){
  if(vrLeft)vrLeft.gameObject.SetActive(false);if(vrRight)vrRight.gameObject.SetActive(false);
  if(vrLeftGrip)vrLeftGrip.gameObject.SetActive(false);if(vrRightGrip)vrRightGrip.gameObject.SetActive(false);
  if(vrRay)vrRay.enabled=false;if(vrCursor)vrCursor.gameObject.SetActive(false);
  if(vrWrist){vrWristOpen=false;vrWristAmount=vrWristGestureTime=0;vrWristFade.alpha=0;vrWrist.gameObject.SetActive(false);}
 }
 void UpdateVRInputGate(){
  if(systemSuspended){vrInputReady=false;return;}
  if(vrInputReady)return;
  if(VRHeld("Select")||VRHeld("Flare")||VRHeld("Jump")||VRHeld("Pause")||VRHeld("Menu")||VRHeld("Recenter")||VRHeld("Use"))return;
  if(VA("Move").ReadValue<Vector2>().magnitude>.18f||VA("Turn").ReadValue<Vector2>().magnitude>.3f)return;
  vrInputReady=true;vrSnapReady=true;
 }
}
}
