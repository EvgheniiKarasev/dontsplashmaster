using UnityEngine;

namespace DontSplash {
public sealed partial class SplashGame {
 bool applicationFocused=true,applicationPaused,xrInputFocused=true,vrTrackingAvailable=true;
 bool systemSuspended,audioPausedBeforeSystem,vrInputReady=true,vrPreview;
 bool xrSessionOwnsFocus;
 bool SystemSuspended=>systemSuspended;
 bool VRCanInteract=>!systemSuspended&&vrInputReady;
 bool VRMenuCanInteract=>!systemSuspended;

 void OnApplicationFocus(bool focus){applicationFocused=focus;RefreshSystemPause();}
 void OnApplicationPause(bool pause){applicationPaused=pause;if(pause){SaveActiveRun();BeginVRHandoff();}RefreshSystemPause();}
 void SetXRInputFocus(bool focus){xrInputFocused=focus;RefreshSystemPause();}
 void PollXRInputFocus(){
  // Use the loader that actually owns this rig. An OVRPlugin query failure is
  // indistinguishable from lost focus and can leave an OpenXR app locked out.
  if(vr&&!vrPreview&&UnityEngine.XR.Management.XRGeneralSettings.Instance?.Manager?.activeLoader is UnityEngine.XR.OpenXR.OpenXRLoader){
   SetXRSessionFocus(UnityEngine.XR.OpenXR.OpenXRUtility.IsSessionFocused);
   SetXRUserPresence(UnityEngine.XR.OpenXR.OpenXRUtility.IsUserPresent);
  }
 }
 void SetXRSessionFocus(bool focus){xrSessionOwnsFocus=true;xrInputFocused=focus;}
 void RefreshSystemPause(){
  // Android window-focus callbacks can lag behind the XR session. A focused
  // OpenXR session is authoritative; an OS pause still always suspends play.
  bool suspended=applicationPaused||(!applicationFocused&&!xrSessionOwnsFocus)||(vr&&(!xrInputFocused||!vrTrackingAvailable||!xrUserPresent||vrHandoffPending));
  if(suspended==systemSuspended)return;
  systemSuspended=suspended;
  if(suspended){
   audioPausedBeforeSystem=AudioListener.pause;
   // Ordinary play resumes through RESUME. Caught/shop/other modes keep their
   // exact state; all their update/input paths are gated until focus returns.
   Pause();AudioListener.pause=true;
   ClearGameplayInput();CancelVRThrows();CancelVRPointer();ResetVRMenuInput();vrInputReady=false;vrSnapReady=false;
   HideVRInteraction();
  }else{
   AudioListener.pause=audioPausedBeforeSystem||mode==Mode.Pause;
   // Keep an already open menu anchored through brief tracking/focus changes.
   // A new mode is placed by LateVR; X explicitly recentres an existing panel.
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
