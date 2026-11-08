using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace DontSplash {
public static partial class QuestSmoke {
 static SplashGame reviewGame;
 static XRHMD reviewHead;
 static OculusTouchControllerProfile.OculusTouchController reviewLeft,reviewRight;
 static double reviewStarted;
 static int reviewPhase=-1;
 static int reviewFrameNumber=-1;
 static string reviewTag;
 static InputSettings.BackgroundBehavior reviewBackground;
 static InputSettings.EditorInputBehaviorInPlayMode reviewEditorInput;
 static readonly StringBuilder reviewLog=new StringBuilder();
 static float[] reviewSamples=new float[2048];
 static readonly System.Collections.Generic.Dictionary<AudioSource,int> reviewTimes=new System.Collections.Generic.Dictionary<AudioSource,int>();
 static readonly System.Collections.Generic.Dictionary<string,double> reviewLevels=new System.Collections.Generic.Dictionary<string,double>();
 [InitializeOnLoadMethod]static void ReviewInit(){EditorApplication.update+=ReviewPoll;}
 static void ReviewPoll(){
  if(BuildPipeline.isBuildingPlayer||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/QuestReview.request")){
   SessionState.SetString("QuestReview",File.ReadAllText("Temp/QuestReview.request").Trim());File.Delete("Temp/QuestReview.request");QuestBuild.Configure();EditorApplication.EnterPlaymode();return;
  }
  if(!EditorApplication.isPlaying)return;
  if(!reviewGame){
   string tag=SessionState.GetString("QuestReview","");if(tag==""||EditorApplication.timeSinceStartup-started<4)return;
   var game=UnityEngine.Object.FindFirstObjectByType<SplashGame>();if(!game)return;
   SessionState.SetString("QuestReview","");reviewTag=tag;ReviewSetup(game);return;
  }
  try{ReviewFrame();}catch(Exception error){File.WriteAllText("Logs/Quest-review-error.txt",error.ToString());reviewGame=null;EditorApplication.ExitPlaymode();}
 }
 static void ReviewSetup(SplashGame game){
  reviewGame=game;game.enabled=false;reviewLog.Clear();reviewTimes.Clear();reviewLevels.Clear();reviewPhase=-1;
  reviewBackground=InputSystem.settings.backgroundBehavior;reviewEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
  InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
  InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
  reviewHead=InputSystem.AddDevice<XRHMD>();simulated.Add(reviewHead);
  InputSystem.RegisterLayout<OculusTouchControllerProfile.OculusTouchController>();
  InputSystem.RegisterLayout(@"{""name"":""QuestSmokeTouch"",""extend"":""OculusTouchController"",""controls"":[{""name"":""thumbstick"",""layout"":""Stick""}]}");
  reviewLeft=(OculusTouchControllerProfile.OculusTouchController)InputSystem.AddDevice("QuestSmokeTouch");reviewRight=(OculusTouchControllerProfile.OculusTouchController)InputSystem.AddDevice("QuestSmokeTouch");
  simulated.Add(reviewLeft);simulated.Add(reviewRight);InputSystem.SetDeviceUsage(reviewLeft,CommonUsages.LeftHand);InputSystem.SetDeviceUsage(reviewRight,CommonUsages.RightHand);
  Call(game,"TryInitializeVR",true);Call(game,"OnApplicationPause",false);Call(game,"OnApplicationFocus",true);Call(game,"SetXRInputFocus",true);Set(game,"vrTrackingAvailable",true);Call(game,"RefreshSystemPause");
  Field<InputActionAsset>(game,"vrActions").devices=new InputDevice[]{reviewHead,reviewLeft,reviewRight};
  Push(reviewHead.isTracked,1f);Push(reviewHead.centerEyePosition,new Vector3(0,1.65f,0));Push(reviewHead.centerEyeRotation,Quaternion.identity);
  foreach(var controller in new[]{reviewLeft,reviewRight}){Push(controller.isTracked,1f);Push(controller.deviceRotation,Quaternion.identity);Push(controller.pointerRotation,Quaternion.identity);}
  Push(reviewLeft.devicePosition,new Vector3(-.18f,1.25f,.4f));Push(reviewRight.devicePosition,new Vector3(.18f,1.25f,.4f));
  Call(game,"UpdateVRInputGate");Call(game,"StartRun",false);Call(game,"Resume");Call(game,"UpdateVRPose");Call(game,"LateUpdate");
  reviewStarted=EditorApplication.timeSinceStartup;
 }
 static void ReviewFrame(){
  if(reviewFrameNumber==Time.frameCount)return;reviewFrameNumber=Time.frameCount;
  var game=reviewGame;float elapsed=(float)(EditorApplication.timeSinceStartup-reviewStarted);int phase=elapsed<5?0:elapsed<8?1:elapsed<10?2:elapsed<12?3:elapsed<14?4:elapsed<16?5:6;
  Call(game,"OnApplicationPause",false);Call(game,"OnApplicationFocus",true);Call(game,"SetXRInputFocus",true);
  // Real headset tracking never stays exactly still. Include translation and
  // height changes so room-scale capsule moves/resizing cannot hide false landings.
  Push(reviewHead.isTracked,1f);Push(reviewHead.centerEyePosition,new Vector3(Mathf.Sin(elapsed*7)*.025f,1.65f+Mathf.Sin(elapsed*4)*.018f,Mathf.Sin(elapsed*9)*.02f));Push(reviewHead.centerEyeRotation,Quaternion.Euler(phase>=3?38:0,0,0));
  Push(reviewLeft.isTracked,1f);Push(reviewRight.isTracked,1f);Call(game,"UpdateVRPose");Call(game,"UpdateVRInputGate");if(Field<object>(game,"mode").ToString()=="Pause")Call(game,"Resume");
  if(phase!=reviewPhase){
   reviewPhase=phase;reviewLog.AppendLine("PHASE "+phase+" t="+elapsed);
   Push(reviewLeft.thumbstick,phase==1?new Vector2(0,.55f):Vector2.zero);
   if(phase>=3&&phase<=5){
    Call(game,"ResetThrows");Set(game,"cooldown",0f);Set(game,"stones",phase==3?0:5);Set(game,"reserveBottles",0);Set(game,"flares",2);Trigger(reviewLeft,0);Trigger(reviewRight,0);Call(game,"TickVRThrows",Time.unscaledTime);
    var controller=phase==5?reviewLeft:reviewRight;Trigger(controller,1);Call(game,"UpdateVRPose");Call(game,"TickVRThrows",Time.unscaledTime+.01f);
    var grip=Field<Transform>(game,phase==5?"vrLeftGrip":"vrRightGrip");var eye=Field<Camera>(game,"eye");
    Vector3 center=GripCenter(game,phase==5?"Left":"Right");
    Quaternion correction=Quaternion.FromToRotation(grip.TransformDirection(PalmAxis(grip,phase!=5)),(eye.transform.position-center).normalized);
    Push(controller.deviceRotation,correction*controller.deviceRotation.ReadValue());Call(game,"UpdateVRPose");Call(game,"UpdateVRHeldItems");
    Push(reviewHead.centerEyeRotation,Quaternion.Euler(38,0,0));Call(game,"UpdateVRPose");
   }
   if(phase==6){ReviewCaptureFog(game);foreach(var level in reviewLevels)reviewLog.AppendLine("MAX OUTPUT RMS "+level.Key+": "+level.Value.ToString("F5"));File.WriteAllText("Logs/Quest-review-"+reviewTag+".txt",reviewLog.ToString());reviewGame=null;InputSystem.settings.backgroundBehavior=reviewBackground;InputSystem.settings.editorInputBehaviorInPlayMode=reviewEditorInput;foreach(var device in simulated)if(device.added)InputSystem.RemoveDevice(device);simulated.Clear();Field<InputActionAsset>(game,"vrActions").devices=null;EditorApplication.ExitPlaymode();return;}
  }
  Call(game,"Update");Call(game,"LateUpdate");
  if(phase>=3&&phase<=5){Field<Material>(game,"vrComfortMaterial").SetFloat("_Fade",0);Field<Material>(game,"vrComfortMaterial").SetFloat("_Vignette",0);}
  if(phase>=3&&phase<=5&&elapsed>(phase==3?10.8f:phase==4?12.8f:14.8f)){
   string kind=phase==3?"stone":phase==4?"bottle":"flare";string path="Logs/Quest-player-"+reviewTag+"-"+kind+".png";
   if(!File.Exists(path))Capture(Field<Camera>(game,"eye"),path);
  }
  foreach(var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None)){
   int previous=reviewTimes.TryGetValue(source,out var value)?value:-1;
   if(source.isPlaying){source.GetOutputData(reviewSamples,0);double rms=Math.Sqrt(reviewSamples.Average(v=>(double)v*v));string key=phase+" / "+source.clip.name;reviewLevels[key]=reviewLevels.TryGetValue(key,out var maximum)?Math.Max(maximum,rms):rms;if(previous<0||source.timeSamples<previous)reviewLog.AppendLine(elapsed.ToString("F2")+" PLAY "+source.name+" / "+source.clip.name+" vol="+source.volume+" rms="+rms.ToString("F5")+" spatial="+source.spatialBlend+" distance="+Vector3.Distance(source.transform.position,Field<Camera>(game,"eye").transform.position));reviewTimes[source]=source.timeSamples;}
   else reviewTimes[source]=-1;
  }
 }
 static void ReviewCaptureFog(SplashGame game){
  var body=Field<CharacterController>(game,"body");body.enabled=false;body.transform.position=new Vector3(0,.65f,15);body.enabled=true;Call(game,"ResetThrows");
  foreach(int yaw in new[]{0,45,90}){Push(reviewHead.centerEyeRotation,Quaternion.Euler(0,yaw,0));Call(game,"UpdateVRPose");Call(game,"LateUpdate");Capture(Field<Camera>(game,"eye"),"Logs/Quest-fog-"+reviewTag+"-"+yaw+".png");}
 }
}
}
