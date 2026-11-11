using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace DontSplash {
public static partial class QuestSmoke {
 static SplashGame storeGame;
 static XRHMD storeHead;
 static OculusTouchControllerProfile.OculusTouchController storeLeft,storeRight;
 static InputSettings.BackgroundBehavior storeBackground;
 static InputSettings.EditorInputBehaviorInPlayMode storeEditorInput;
 static RunPreferences storePreferences;
 public static void StoreSetup(){
  storeGame=UnityEngine.Object.FindFirstObjectByType<SplashGame>();Check(storeGame,"Open the game in Play mode first");
  var game=storeGame;game.enabled=false;storePreferences=new RunPreferences();
  storeBackground=InputSystem.settings.backgroundBehavior;storeEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
  InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
  InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
  storeHead=InputSystem.AddDevice<XRHMD>();
  InputSystem.RegisterLayout<OculusTouchControllerProfile.OculusTouchController>();
  InputSystem.RegisterLayout(@"{""name"":""StoreTouch"",""extend"":""OculusTouchController"",""controls"":[{""name"":""thumbstick"",""layout"":""Stick""}]}");
  storeLeft=(OculusTouchControllerProfile.OculusTouchController)InputSystem.AddDevice("StoreTouch");
  storeRight=(OculusTouchControllerProfile.OculusTouchController)InputSystem.AddDevice("StoreTouch");
  InputSystem.SetDeviceUsage(storeLeft,CommonUsages.LeftHand);InputSystem.SetDeviceUsage(storeRight,CommonUsages.RightHand);
  Call(game,"TryInitializeVR",true);
  Field<InputActionAsset>(game,"vrActions").devices=new InputDevice[]{storeHead,storeLeft,storeRight};
  Push(storeHead.isTracked,1f);Push(storeHead.centerEyePosition,new Vector3(0,1.65f,0));Push(storeHead.centerEyeRotation,Quaternion.identity);
  foreach(var c in new[]{storeLeft,storeRight}){Push(c.isTracked,1f);Push(c.deviceRotation,Quaternion.identity);Push(c.pointerRotation,Quaternion.identity);}
  Call(game,"OnApplicationPause",false);Call(game,"OnApplicationFocus",true);Call(game,"SetXRUserPresence",true);Call(game,"SetXRSessionFocus",true);
  Set(game,"vrTrackingAvailable",true);SettleVRRecovery(game);Call(game,"StartRun",false);Call(game,"Resume");
  Check(Field<Camera>(game,"eye").GetComponent<Light>()==null,"Headlamp should be absent");
 }
 public static void StorePose(int shot){
  var game=storeGame;Check(game,"Call StoreSetup first");
  Trigger(storeLeft,0);Trigger(storeRight,0);Call(game,"ResetThrows");
  Set(game,"stage",shot==3?3:shot==4?7:shot==5?4:0);Set(game,"leg",shot==3?1:0);Call(game,"BuildRoom");
  Push(storeHead.isTracked,1f);Push(storeLeft.isTracked,1f);Push(storeRight.isTracked,1f);
  Call(game,"OnApplicationPause",false);Call(game,"OnApplicationFocus",true);Call(game,"SetXRUserPresence",true);Call(game,"SetXRSessionFocus",true);SettleVRRecovery(game);Call(game,"Resume");
  var body=Field<CharacterController>(game,"body");body.enabled=false;
  Vector3 stand=new Vector3(0,.65f,shot==1?11:shot==2?21:11);
  if(shot==3||shot==4||shot==5){var steps=Field<System.Collections.IList>(game,"steps");var homes=steps.Cast<object>().Select(s=>(Vector3)s.GetType().GetField("home").GetValue(s));stand=homes.OrderBy(p=>Vector3.Distance(p,shot==5?new Vector3(78,.1f,8):new Vector3(17,.1f,49))).First();stand.y=.65f;}
  body.transform.SetPositionAndRotation(stand,Quaternion.identity);
  Field<Transform>(game,"vrOrigin").localRotation=Quaternion.identity;
  Push(storeHead.centerEyeRotation,Quaternion.Euler(shot==2?27:shot==4?10:18,shot==2?5:shot==3?8:shot==1?-7:0,0));
  StoreHand(false,new Vector3(-.30f,1.35f,.58f),new Vector3(0,-8,35));
  StoreHand(true,new Vector3(.34f,1.29f,.60f),new Vector3(0,8,-35));
  Call(game,"UpdateVRPose");Set(game,"cooldown",0f);Set(game,"stones",shot==1||shot==4?0:4);Set(game,"reserveBottles",0);Set(game,"flares",2);Set(game,"noticeTime",0f);Set(game,"elapsed",shot*37f);
  Set(game,"tutorialJumpDone",true);Set(game,"nextUIRefresh",0f);Call(game,"UpdateVRInputGate");Call(game,"TickVRThrows",Time.unscaledTime);
  if(shot!=2&&shot!=5)Trigger(storeLeft,1);
  if(shot!=2)Trigger(storeRight,1);
  Call(game,"TickVRThrows",Time.unscaledTime+.02f);StoreRefresh();
  if(shot==2){
   StoreHand(false,new Vector3(-.35f,1.15f,.6f),new Vector3(15,-20,30));
   StoreHand(true,new Vector3(.46f,1.30f,.72f),new Vector3(30,-15,-25));StoreRefresh();
   Call(game,"EmitFX",0,new Vector3(.8f,0,24.5f));Call(game,"StepFX",.30f);
  }
  if(shot==3||shot==4){
   var monster=Field<Transform>(game,"monster");monster.position=body.transform.position+new Vector3(shot==3?2.2f:-1.3f,-1.1f,shot==3?6:4.7f);
   monster.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(body.transform.position-monster.position,Vector3.up));
   var performance=Field<MonsterPerformance>(game,"creature");performance.ResetPose();
   for(int i=0;i<24;i++)performance.Tick(.016f,true,true,shot==3,6,1);
   if(shot==3){Call(game,"EmitFX",1,body.transform.position+new Vector3(.8f,.2f,4.4f));Call(game,"StepFX",.2f);}
  }else Field<Transform>(game,"monster").position=new Vector3(60,-.5f,80);
  if(shot==5){
   StoreHand(false,new Vector3(-.22f,1.47f,.64f),Vector3.zero);OrientPalm(game,storeLeft,Vector3.up);AdvanceWrist(game,.65f);
  }
  StoreRefresh();
 }
 static void StoreHand(bool right,Vector3 position,Vector3 angles){var c=right?storeRight:storeLeft;Push(c.devicePosition,position);Push(c.deviceRotation,Quaternion.Euler(angles));Push(c.pointerPosition,position);Push(c.pointerRotation,Quaternion.Euler(angles));}
 public static void StoreRefresh(){
  var game=storeGame;Call(game,"UpdateVRPose");Field<Animator>(game,"vrLeftAnimator").Update(.1f);Field<Animator>(game,"vrRightAnimator").Update(.1f);Call(game,"UpdateVRHeldItems");Call(game,"SyncUI");Call(game,"LateVR");
  var comfort=Field<Material>(game,"vrComfortMaterial");comfort.SetFloat("_Fade",0);comfort.SetFloat("_Vignette",0);
 }
 public static void StoreCapture(string name){
  StoreRefresh();var eye=Field<Camera>(storeGame,"eye");eye.fieldOfView=65;eye.aspect=16f/9;
  var previous=eye.targetTexture;var active=RenderTexture.active;
  var rt=new RenderTexture(2560,1440,24);var pixels=new Texture2D(2560,1440,TextureFormat.RGB24,false);
  // Snapshot the actual animated meshes on CPU. Freshly spawned rigs otherwise
  // reuse stale GPU skinning buffers when several rooms are captured in one frame.
  var skins=UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None).Where(s=>s.enabled&&s.gameObject.activeInHierarchy).ToArray();
  var snapshots=new System.Collections.Generic.List<GameObject>();var meshes=new System.Collections.Generic.List<Mesh>();
  try{foreach(var skin in skins){var mesh=new Mesh();skin.BakeMesh(mesh);meshes.Add(mesh);var copy=new GameObject("Capture pose");copy.layer=skin.gameObject.layer;copy.transform.SetParent(skin.transform,false);var scale=skin.transform.lossyScale;copy.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);copy.AddComponent<MeshFilter>().sharedMesh=mesh;copy.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;snapshots.Add(copy);skin.enabled=false;}eye.targetTexture=rt;eye.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,2560,1440),0,0);pixels.Apply();Directory.CreateDirectory("Previews/Store");File.WriteAllBytes("Previews/Store/"+name+".png",pixels.EncodeToPNG());}
  finally{eye.targetTexture=previous;RenderTexture.active=active;foreach(var skin in skins)if(skin)skin.enabled=true;foreach(var copy in snapshots)UnityEngine.Object.DestroyImmediate(copy);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
 }
 public static void StoreCleanup(){
  if(storeGame){Field<InputActionAsset>(storeGame,"vrActions").devices=null;var mode=typeof(SplashGame).GetField("mode",Flags);mode.SetValue(storeGame,Enum.Parse(mode.FieldType,"Menu"));}
  foreach(var d in new InputDevice[]{storeHead,storeLeft,storeRight})if(d!=null&&d.added)InputSystem.RemoveDevice(d);
  InputSystem.RemoveLayout("StoreTouch");InputSystem.settings.backgroundBehavior=storeBackground;InputSystem.settings.editorInputBehaviorInPlayMode=storeEditorInput;
  storePreferences?.Dispose();storePreferences=null;storeGame=null;
 }
}
}
