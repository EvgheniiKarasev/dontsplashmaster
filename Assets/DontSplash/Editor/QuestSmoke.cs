using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace DontSplash {
// Exercises the actual game with synthetic OpenXR controls; no headset required.
public static partial class QuestSmoke {
 static double started;
 static readonly System.Collections.Generic.List<UnityEngine.InputSystem.InputDevice> simulated=new System.Collections.Generic.List<UnityEngine.InputSystem.InputDevice>();
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Call(SplashGame game,string name,params object[] args)=>typeof(SplashGame).GetMethod(name,Flags).Invoke(game,args);
 static T Field<T>(SplashGame game,string name)=>(T)typeof(SplashGame).GetField(name,Flags).GetValue(game);
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 [InitializeOnLoadMethod]static void Init(){started=EditorApplication.timeSinceStartup;EditorApplication.update+=Poll;}
 [MenuItem("Dont Splash/VR/Run simulated VR checks")]
 public static void Run(){QuestBuild.Configure();SessionState.SetBool("QuestSmoke",true);EditorApplication.EnterPlaymode();}
 public static void RunBatch(){
  UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/DontSplash.unity");
  SessionState.SetBool("QuestSmokeBatch",true);Run();
 }
 static void Poll(){
  int exit=SessionState.GetInt("QuestSmokeExit",0);
  if(exit>0&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetInt("QuestSmokeExit",0);EditorApplication.Exit(exit-1);return;}
  if(BuildPipeline.isBuildingPlayer)return;
  if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/QuestSmoke.request")&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating){File.Delete("Temp/QuestSmoke.request");Run();return;}
  if(!SessionState.GetBool("QuestSmoke",false)||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup-started<4)return;
  var game=UnityEngine.Object.FindFirstObjectByType<SplashGame>();if(!game)return;
  SessionState.SetBool("QuestSmoke",false);
  // Opening the simulator can take focus while synthetic events are queued.
  // Isolate test focus handling without changing the project's input settings.
  var originalInput=InputSystem.settings;
  var background=originalInput.backgroundBehavior;var editorBehavior=originalInput.editorInputBehaviorInPlayMode;
  originalInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
  originalInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
  using var savedProgress=new RunPreferences();PlayerPrefs.DeleteKey("HasSave");
  try{Test(game);File.WriteAllText("Logs/QuestSmoke.txt","PASS: system focus/pause/tracking suspension during catch, overlapping pause sources, hidden hands/rays/HUD, live HMD tracking under system overlay, input neutral rearm and click cancellation, manifest permission removal rules, controller bindings, OpenXR-corrected animated Meta hands, neutral palms away from chest, palm-up inventory gesture, animated close and complete hiding, hand lowering and tracking loss, stable anchored menus, stick/A navigation and ray clicks, locomotion, snap turn, visible menu ray/cursor pixels, held items, analog release without digital click events, life-size bottle, amplified motion-based throw with 6-18 m simulated range and stationary drop, tracking/pause cancellation, bottle shattering, dry-surface silence and water attraction, monster face/strike targeting at three player heights with HMD movement, death camera stability and hidden monster behind retry UI.\nThis is a simulated test, not a headset performance test.");}
  catch(Exception e){File.WriteAllText("Logs/QuestSmoke.txt","FAIL\n"+e);Debug.LogException(e);}
  finally{try{var controls=Resources.Load<InputActionAsset>("QuestControls");if(controls)controls.devices=null;foreach(var device in simulated)if(device.added)InputSystem.RemoveDevice(device);simulated.Clear();InputSystem.RemoveLayout("QuestSmokeTouch");originalInput.backgroundBehavior=background;originalInput.editorInputBehaviorInPlayMode=editorBehavior;}finally{EditorApplication.ExitPlaymode();if(SessionState.GetBool("QuestSmokeBatch",false)){SessionState.SetBool("QuestSmokeBatch",false);SessionState.SetInt("QuestSmokeExit",File.ReadAllText("Logs/QuestSmoke.txt").StartsWith("PASS")?1:2);}}}
 }
 static void Trigger(OculusTouchControllerProfile.OculusTouchController controller,float value){Push(controller.triggerPressed,value>.5f?1f:0f);Push(controller.trigger,value);}
 static void Push<T>(InputControl<T> control,T value) where T:struct {using(UnityEngine.InputSystem.LowLevel.StateEvent.From(control.device,out var state)){control.WriteValueIntoEvent(value,state);InputSystem.QueueEvent(state);InputSystem.Update();}}
 static void Test(SplashGame game){
  game.enabled=false;
  var head=InputSystem.AddDevice<XRHMD>();simulated.Add(head);
  InputSystem.RegisterLayout<OculusTouchControllerProfile.OculusTouchController>();
  InputSystem.RegisterLayout(@"{""name"":""QuestSmokeTouch"",""extend"":""OculusTouchController"",""controls"":[{""name"":""thumbstick"",""layout"":""Stick""}]}");
  var left=(OculusTouchControllerProfile.OculusTouchController)InputSystem.AddDevice("QuestSmokeTouch");
  var right=(OculusTouchControllerProfile.OculusTouchController)InputSystem.AddDevice("QuestSmokeTouch");
  simulated.Add(left);simulated.Add(right);
  InputSystem.SetDeviceUsage(left,UnityEngine.InputSystem.CommonUsages.LeftHand);InputSystem.SetDeviceUsage(right,UnityEngine.InputSystem.CommonUsages.RightHand);
  Call(game,"TryInitializeVR",true);
  Call(game,"OnApplicationPause",false);Call(game,"OnApplicationFocus",true);Call(game,"SetXRInputFocus",true);
  Set(game,"vrTrackingAvailable",true);Call(game,"RefreshSystemPause");
  var actions=Field<InputActionAsset>(game,"vrActions");
  actions.devices=new UnityEngine.InputSystem.InputDevice[]{head,left,right};
  foreach(var action in actions)Check(action.controls.Count>0,"Unbound action: "+action.name);
  Push(head.centerEyePosition,new Vector3(0,1.65f,0));
  Push(head.centerEyeRotation,Quaternion.identity);Push(head.isTracked,1f);
  Push(left.isTracked,1f);Push(right.isTracked,1f);
  Push(left.pointerPosition,new Vector3(-.2f,1.25f,0));
  Push(left.pointerRotation,Quaternion.identity);
  Push(right.pointerPosition,new Vector3(.2f,1.25f,0));
  Push(right.pointerRotation,Quaternion.identity);
  Push(left.devicePosition,new Vector3(-.25f,1.3f,.4f));Push(left.deviceRotation,Quaternion.identity);
  Push(right.devicePosition,new Vector3(.25f,1.3f,.4f));Push(right.deviceRotation,Quaternion.identity);
  Call(game,"UpdateVRInputGate");
  Call(game,"UpdateVRPose");Call(game,"SyncUI");Call(game,"LateVR");
  var eye=Field<Camera>(game,"eye");var canvas=Field<Canvas>(game,"uiCanvas");
  Check(canvas.renderMode==RenderMode.WorldSpace,"UI is not world-space");
  Check(Mathf.Abs(eye.transform.localPosition.y-1.65f)<.001f,"Head pose missing: eye="+eye.transform.localPosition+" action="+actions.FindAction("HeadPosition").ReadValue<Vector3>()+" tracked="+actions.FindAction("HeadTracked").ReadValue<float>()+" enabled="+actions.FindAction("HeadPosition").enabled+" device="+head.centerEyePosition.ReadValue());
  foreach(string side in new[]{"Left","Right"}){
   var visual=Field<Transform>(game,"vr"+side+"Grip");
   var renderer=visual.GetComponentInChildren<SkinnedMeshRenderer>();
   Check(renderer&&renderer.enabled&&renderer.sharedMesh&&renderer.bones.Length>15,"Meta hand mesh missing: "+side);
   Check(renderer.bounds.size.magnitude<.6f,"Hand mesh is not life size: "+side);
  }
  var animator=Field<Animator>(game,"vrRightAnimator");animator.Update(0);
  var leftAnimator=Field<Animator>(game,"vrLeftAnimator");leftAnimator.Update(0);
  var leftGrip=Field<Transform>(game,"vrLeftGrip");var rightGrip=Field<Transform>(game,"vrRightGrip");
  FeedbackDiagnostics(game);
  Vector3 leftNormal=PalmAxis(leftGrip,false),rightNormal=PalmAxis(rightGrip,true);leftNormal.x=-leftNormal.x;
  Check(Vector3.Dot(leftNormal,rightNormal)>.97f,"Right palm is inverted relative to left: "+leftNormal+" / "+rightNormal);
  Vector3 leftFingers=FingerAxis(leftGrip),rightFingers=FingerAxis(rightGrip);leftFingers.x=-leftFingers.x;
  Check(Vector3.Dot(leftFingers,rightFingers)>.97f,"Right fingers point in the wrong direction: "+leftFingers+" / "+rightFingers);
  Check(Mathf.Abs(leftNormal.z)<.5f&&Mathf.Abs(rightNormal.z)<.5f,"Palms face the chest in neutral controller pose");
  var middle=animator.GetComponentsInChildren<Transform>().First(t=>t.name=="XRHand_MiddleProximal");var relaxed=middle.localRotation;
  typeof(SplashGame).GetField("vrHandMotion",Flags).SetValue(game,1f);Call(game,"UpdateVRPose");animator.Update(.1f);
  Check(Quaternion.Angle(relaxed,middle.localRotation)>1,"Running did not animate relaxed fingers");
  Check(Vector3.Distance(rightGrip.localPosition,new Vector3(.25f,1.3f,.4f))<.001f,"Running animation moved tracked wrist");
  typeof(SplashGame).GetField("vrHandMotion",Flags).SetValue(game,0f);Call(game,"UpdateVRPose");animator.Update(.1f);
  var index=animator.GetComponentsInChildren<Transform>().First(t=>t.name=="XRHand_IndexProximal");
  var open=index.localRotation;
  Push(right.trigger,1f);Push(right.grip,1f);Call(game,"UpdateVRPose");animator.Update(.1f);
  Check(animator.GetFloat("Flex")>.999f,"Relaxed fingers override grip input");
  Check(Quaternion.Angle(open,index.localRotation)>5,"Hand finger animation did not move");
  Push(left.trigger,1f);Push(left.grip,1f);Call(game,"UpdateVRPose");leftAnimator.Update(.1f);
  Check(leftAnimator.GetFloat("Pinch")==0&&leftAnimator.GetFloat("Flex")<.3f,"Left hand reacts to menu click");
  Check(animator.GetFloat("Pinch")>.99f,"Right hand lost menu click animation");
  Push(left.trigger,0f);Push(left.grip,0f);Call(game,"UpdateVRPose");leftAnimator.Update(.1f);
  Push(right.trigger,0f);Push(right.grip,0f);Call(game,"UpdateVRPose");animator.Update(.1f);
  Field<Material>(game,"vrComfortMaterial").SetFloat("_Fade",0);
  Check(rightGrip.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial.renderQueue>Field<Material>(game,"vrUI").renderQueue,"Menu still draws over nearer hands");
  Push(left.devicePosition,new Vector3(-.15f,1.65f,.65f));Push(right.devicePosition,new Vector3(.15f,1.65f,.65f));
  Call(game,"UpdateVRPose");leftAnimator.Update(.1f);animator.Update(.1f);
  Capture(eye,"Logs/Quest-menu.png");
  CheckMenuHandPixels();
  Push(left.devicePosition,new Vector3(-.25f,.8f,.4f));Push(right.devicePosition,new Vector3(.25f,1.3f,.4f));Call(game,"UpdateVRPose");
  var play=canvas.GetComponentsInChildren<Button>().First(b=>b.gameObject.name.EndsWith(" / PLAY"));
  var hand=Field<Transform>(game,"vrRight");
  var target=((RectTransform)play.transform).TransformPoint(((RectTransform)play.transform).rect.center);
  var localAim=Quaternion.Inverse(hand.parent.rotation)*Quaternion.LookRotation(target-hand.position);
  Push(right.pointerRotation,localAim);Call(game,"UpdateVRPose");
  Trigger(left,1f);Call(game,"TickVRPointer",true);
  Check(!Field<Button>(game,"vrPressed"),"Left trigger pressed a menu button");
  Trigger(left,0f);Call(game,"TickVRPointer",true);
  Check(Field<object>(game,"mode").ToString()!="Play","Left trigger activated PLAY");
  Trigger(right,1f);Call(game,"TickVRPointer",true);
  Check(Field<Button>(game,"vrHovered")==play,"PLAY ray did not hover; aim="+hand.forward+" target="+(target-hand.position).normalized);
  Check(Field<Button>(game,"vrPressed")==play,"PLAY press not received from simulated trigger");
  CheckMenuPointerPixels(game,eye);
  Trigger(right,0f);Call(game,"TickVRPointer",true);
  Check(Field<object>(game,"mode").ToString()=="Play","Controller ray failed to activate PLAY");
  Call(game,"SyncUI");Call(game,"LateVR");Field<Material>(game,"vrComfortMaterial").SetFloat("_Fade",0);
  var wrist=Field<RectTransform>(game,"vrWrist");Check(wrist&&!wrist.gameObject.activeSelf,"Wrist HUD must start hidden");
  Push(left.devicePosition,new Vector3(-.18f,1.4f,.5f));OrientPalm(game,left,Vector3.up);AdvanceWrist(game,.5f);
  Check(wrist.gameObject.activeInHierarchy&&Field<CanvasGroup>(game,"vrWristFade").alpha>.99f,"Raised palm did not open inventory");
  var wristBefore=wrist.position;
  Push(left.devicePosition,new Vector3(-.05f,1.5f,.5f));Call(game,"UpdateVRPose");Call(game,"LateVR");
  Check(Vector3.Distance(wrist.position,wristBefore)>.1f,"Wrist HUD does not follow left hand");
  Check(wrist.rect.width*wrist.lossyScale.x<.3f,"Wrist HUD is too large");
  Capture(eye,"Logs/Quest-gameplay.png");
  OrientPalm(game,left,Vector3.down);AdvanceWrist(game,.16f);
  Check(Field<float>(game,"vrWristAmount")>0&&Field<float>(game,"vrWristAmount")<1,"Inventory close animation is missing");
  AdvanceWrist(game,.4f);Check(!wrist.gameObject.activeSelf&&Field<CanvasGroup>(game,"vrWristFade").alpha==0,"Hidden inventory is still visible");
  OrientPalm(game,left,Vector3.up);Push(left.devicePosition,new Vector3(-.2f,.8f,.5f));Call(game,"UpdateVRPose");AdvanceWrist(game,.5f);
  Check(!wrist.gameObject.activeSelf,"Lowered hand opened inventory");Capture(eye,"Logs/Quest-inventory-hidden.png");
  Push(left.devicePosition,new Vector3(-.2f,1.45f,.5f));Call(game,"UpdateVRPose");AdvanceWrist(game,.5f);
  Check(wrist.gameObject.activeSelf,"Inventory did not reopen");
  Push(left.isTracked,0f);Call(game,"UpdateVRPose");AdvanceWrist(game,.02f);Check(!wrist.gameObject.activeSelf,"Tracking loss did not hide inventory");
  Push(left.isTracked,1f);Call(game,"UpdateVRPose");AdvanceWrist(game,.5f);
  // Raise both hands into view for a visual inspection of the imported meshes.
  Push(left.devicePosition,new Vector3(-.22f,1.6f,.55f));Push(right.devicePosition,new Vector3(.22f,1.6f,.55f));
  Call(game,"UpdateVRPose");Field<Animator>(game,"vrLeftAnimator").Update(.1f);animator.Update(.1f);Call(game,"LateVR");
  Capture(eye,"Logs/Quest-hands.png");
  Push(left.thumbstick,new Vector2(0,1));
  Push(right.thumbstick,new Vector2(1,0));
  var body=Field<CharacterController>(game,"body");float before=body.transform.eulerAngles.y;
  Call(game,"ReadVRInput");Check(Field<Vector2>(game,"move").y>.9f,"Move stick failed");
  float turned=body.transform.eulerAngles.y;Check(Mathf.Abs(Mathf.DeltaAngle(before,turned)-30)<.01f,"Snap turn failed");
  Call(game,"ReadVRInput");Check(Mathf.Abs(Mathf.DeltaAngle(turned,body.transform.eulerAngles.y))<.01f,"Held stick repeated snap turn");
  CheckPhysicalThrows(game,left,right);
  Call(game,"Pause");Check(Field<object>(game,"mode").ToString()=="Pause","Pause failed");
  Call(game,"SyncUI");Call(game,"LateVR");
  var anchored=canvas.transform.position;
  var crossed=canvas.transform.position+canvas.transform.forward*.1f;
  Push(right.pointerPosition,hand.parent.InverseTransformPoint(crossed));
  Push(right.devicePosition,hand.parent.InverseTransformPoint(crossed));
  Call(game,"UpdateVRPose");Call(game,"LateVR");
  Check(Vector3.Distance(canvas.transform.position,anchored)<.001f,"Menu jumped when hand crossed panel");
  Push(right.pointerPosition,new Vector3(.2f,1.25f,.3f));Push(right.devicePosition,new Vector3(.2f,1.25f,.3f));Call(game,"UpdateVRPose");
  Click(game,right,"RESUME");Check(Field<object>(game,"mode").ToString()=="Play","Resume ray click failed");
  Call(game,"Pause");Call(game,"SyncUI");Call(game,"LateVR");
  Push(right.pointerRotation,Quaternion.Euler(0,90,0));Call(game,"UpdateVRPose");Call(game,"TickVRPointer",true);
  Push(left.thumbstick,Vector2.zero);Push(right.thumbstick,Vector2.zero);Call(game,"TickVRPointer",true);
  Push(left.thumbstick,Vector2.down);Call(game,"TickVRPointer",true);
  var selected=Field<Button>(game,"vrNavigationButton");Check(selected&&selected.gameObject.name.EndsWith(" / RESUME"),"Stick did not select RESUME");
  Push(right.primaryButton,1f);Call(game,"TickVRPointer",true);Push(right.primaryButton,0f);Push(left.thumbstick,Vector2.zero);
  Check(Field<object>(game,"mode").ToString()=="Play","A did not activate pause selection");
  Call(game,"Pause");Call(game,"SyncUI");Call(game,"LateVR");
  Aim(game,right,"RESUME");Trigger(right,1f);Call(game,"TickVRPointer",true);
  Push(right.isTracked,0f);Call(game,"UpdateVRPose");Call(game,"TickVRPointer",true);
  Check(!Field<Button>(game,"vrPressed"),"Tracking loss left a pressed button");
  Trigger(right,0f);Push(right.isTracked,1f);Call(game,"UpdateVRPose");
  var modeField=typeof(SplashGame).GetField("mode",Flags);modeField.SetValue(game,Enum.Parse(modeField.FieldType,"Dead"));
  Call(game,"SyncUI");Call(game,"LateVR");
  Check(!Field<Transform>(game,"monster").gameObject.activeSelf,"Monster is visible behind death buttons");
  Field<Material>(game,"vrComfortMaterial").SetFloat("_Fade",0);Capture(eye,"Logs/Quest-death-menu.png");Click(game,right,"RETRY");
  Call(game,"LateVR");Check(Field<Transform>(game,"monster").gameObject.activeSelf,"Retry left monster hidden");
  Check(Field<object>(game,"mode").ToString()=="Play","Retry ray click failed");
  CheckSystemPause(game,head,left,right);
  CheckPauseCombinations(game,head,left,right);
  CheckHeadsetHandoff(game,head,left,right);
  foreach(float height in new[]{1.05f,1.65f,2.05f}){
   Push(head.centerEyePosition,new Vector3(.25f,height,.15f));Push(head.centerEyeRotation,Quaternion.Euler(0,40,0));Call(game,"UpdateVRPose");
   var pos=eye.transform.position;var rot=eye.transform.rotation;
   Call(game,"BeginCatch");Call(game,"StepCatch",.5f);
   var creature=Field<MonsterPerformance>(game,"creature");
   Check(Mathf.Abs(creature.FacePosition.y-pos.y)<.01f,"Monster face is below/above the player");
   Check(Vector3.Dot((creature.FacePosition-pos).normalized,eye.transform.forward)>.99f,"Monster is not in front of the player");
   Check(Vector3.Dot(creature.FaceForward,(pos-creature.FacePosition).normalized)>.999f,"Monster is looking past the player");
   Call(game,"SyncUI");Call(game,"LateVR");Field<Material>(game,"vrComfortMaterial").SetFloat("_Fade",0);
   if(height==1.65f)Capture(eye,"Logs/Quest-catch-face.png");
   // Move and crouch during the sequence; the attack must follow the actual HMD.
   Push(head.centerEyePosition,new Vector3(.4f,height-.15f,.25f));Call(game,"UpdateVRPose");pos=eye.transform.position;
   for(int step=0;step<180&&!Field<bool>(game,"catchHit");step++)Call(game,"StepCatch",.02f);
   Check(Field<bool>(game,"catchHit"),"VR attack never reaches impact");
   Check(Vector3.Distance(creature.StrikePosition,pos)>.3f&&Vector3.Distance(creature.StrikePosition,pos)<.65f,"Attack did not stop safely in front of headset: "+Vector3.Distance(creature.StrikePosition,pos));
   Check(Mathf.Abs(creature.FacePosition.y-pos.y)<.01f,"Attack lost player head height");
   Check(Vector3.Dot(creature.FaceForward,(pos-creature.FacePosition).normalized)>.999f,"Monster looks away during strike");
   Check(Vector3.Distance(pos,eye.transform.position)<.001f&&Quaternion.Angle(rot,eye.transform.rotation)<.01f,"Death animation moved VR camera");
   if(height==1.65f)Capture(eye,"Logs/Quest-catch-strike.png");
   Call(game,"StepCatch",.7f);Check(Field<object>(game,"mode").ToString()=="Dead","VR catch did not finish");
  }
  CheckFeedback(game,head,left,right);
  CheckReleasePersistence(game);AuditMetaSetup();
 }
 static void Set(SplashGame game,string name,object value)=>typeof(SplashGame).GetField(name,Flags).SetValue(game,value);
 static void CheckSystemPause(SplashGame game,XRHMD head,OculusTouchControllerProfile.OculusTouchController left,OculusTouchControllerProfile.OculusTouchController right){
  Push(left.thumbstick,Vector2.zero);Push(right.thumbstick,Vector2.zero);
  Trigger(right,0f);Trigger(left,0f);Set(game,"cooldown",0f);
  Call(game,"TickVRThrows",20f);Trigger(right,1f);Call(game,"TickVRThrows",20.02f);
  Check(Field<object>(game,"vrHeldItem")!=null,"Focus test did not hold item");int stock=Field<int>(game,"stones");
  Call(game,"SetXRInputFocus",false);
  Check(Field<object>(game,"mode").ToString()=="Pause"&&AudioListener.pause,"XR focus loss did not pause play/audio");
  Check(Field<object>(game,"vrHeldItem")==null&&Field<int>(game,"stones")==stock,"Focus loss spent held item");
  Call(game,"UpdateVRPose");Call(game,"LateVR");Call(game,"TickVRPointer",true);Call(game,"TickVRWrist",1f);
  foreach(string name in new[]{"vrLeft","vrRight","vrLeftGrip","vrRightGrip","vrCursor","vrWrist"})Check(!Field<Transform>(game,name).gameObject.activeSelf,"System overlay left visible: "+name);
  Check(!Field<LineRenderer>(game,"vrRay").enabled,"System overlay left ray visible");
  var eye=Field<Camera>(game,"eye");Push(head.centerEyePosition,new Vector3(.1f,1.7f,.1f));Call(game,"UpdateVRPose");
  Check(eye.enabled&&Vector3.Distance(eye.transform.localPosition,head.centerEyePosition.ReadValue())<.001f,"System overlay stopped head rendering/tracking");
  Call(game,"Resume");Check(Field<object>(game,"mode").ToString()=="Pause","Resume bypassed system pause");
  Call(game,"OnApplicationPause",true);Call(game,"SetXRInputFocus",true);
  Check(Field<bool>(game,"systemSuspended"),"XR focus return bypassed OS pause");
  Call(game,"OnApplicationPause",false);SettleVRRecovery(game);Call(game,"UpdateVRInputGate");
  Check(!Field<bool>(game,"vrInputReady")&&AudioListener.pause,"Held trigger rearmed input or resumed pause audio");
  Call(game,"Resume");Call(game,"ReadVRInput");Call(game,"TickVRThrows",21f);
  Check(Field<object>(game,"vrHeldItem")==null,"Held trigger spawned item after focus return");
  Trigger(right,0f);Push(left.thumbstick,Vector2.up);Call(game,"UpdateVRInputGate");Call(game,"ReadVRInput");
  Check(!Field<bool>(game,"vrInputReady")&&Field<Vector2>(game,"move")==Vector2.zero,"Held stick bypassed input gate");
  Push(left.thumbstick,Vector2.zero);Call(game,"UpdateVRInputGate");Call(game,"UpdateVRPose");
  Check(Field<bool>(game,"vrInputReady"),"Neutral controls did not rearm input");
  Call(game,"Pause");Call(game,"SyncUI");Call(game,"LateVR");Aim(game,right,"RESUME");
  Trigger(right,1f);Call(game,"TickVRPointer",true);Check(Field<Button>(game,"vrPressed"),"Focus test did not press RESUME");
  Call(game,"OnApplicationFocus",false);Trigger(right,0f);Call(game,"TickVRPointer",true);
  Check(!Field<Button>(game,"vrPressed")&&Field<object>(game,"mode").ToString()=="Pause","Lost-focus release clicked RESUME");
  Call(game,"OnApplicationFocus",true);Call(game,"UpdateVRInputGate");Call(game,"UpdateVRPose");Call(game,"LateVR");
  Click(game,right,"RESUME");Check(Field<object>(game,"mode").ToString()=="Play","Fresh click failed after focus return");
  Call(game,"BeginCatch");Call(game,"StepCatch",.5f);
  float clock=Field<float>(game,"catchClock");int deaths=Field<int>(game,"deaths");var monster=Field<Transform>(game,"monster");var position=monster.position;
  Call(game,"OnApplicationPause",true);Call(game,"OnApplicationFocus",false);Call(game,"SetXRInputFocus",false);
  Call(game,"StepCatch",10f);Call(game,"Update");
  Check(Field<float>(game,"catchClock")==clock&&!Field<bool>(game,"catchHit")&&Field<int>(game,"deaths")==deaths&&monster.position==position,"Suspended catch advanced/struck/killed player");
  Call(game,"OnApplicationPause",false);Call(game,"OnApplicationFocus",true);Call(game,"StepCatch",10f);
  Check(Field<float>(game,"catchClock")==clock&&AudioListener.pause,"Catch resumed before XR focus returned");
  Call(game,"SetXRInputFocus",true);SettleVRRecovery(game);Call(game,"StepCatch",.02f);
  Check(Field<float>(game,"catchClock")>clock&&!AudioListener.pause,"Catch did not resume at saved point");
  Push(head.isTracked,0f);Call(game,"TickVR");clock=Field<float>(game,"catchClock");Call(game,"StepCatch",10f);
  Check(Field<bool>(game,"systemSuspended")&&Field<float>(game,"catchClock")==clock,"Lost HMD tracking did not freeze catch");
  Push(head.isTracked,1f);Call(game,"TickVR");Call(game,"Resume");Call(game,"UpdateVRPose");
  var manifest=new System.Xml.XmlDocument();manifest.Load("Assets/Plugins/Android/AndroidManifest.xml");
  foreach(string permission in new[]{"RECORD_AUDIO","MODIFY_AUDIO_SETTINGS","BLUETOOTH"})Check(manifest.SelectNodes("/manifest/uses-permission").Cast<System.Xml.XmlElement>().Any(e=>e.GetAttribute("name","http://schemas.android.com/apk/res/android")=="android.permission."+permission&&e.GetAttribute("node","http://schemas.android.com/tools")=="remove"),"Missing manifest removal rule: "+permission);
 }
 static T Item<T>(object item,string name)=>(T)item.GetType().GetField(name).GetValue(item);
 static void ItemSet(object item,string name,object value)=>item.GetType().GetField(name).SetValue(item,value);
 static void CheckPhysicalThrows(SplashGame game,OculusTouchControllerProfile.OculusTouchController left,OculusTouchControllerProfile.OculusTouchController right){
  Call(game,"ResetThrows");Set(game,"cooldown",0f);Set(game,"rangeLevel",0);
  Trigger(right,0f);Trigger(left,0f);
  Push(right.devicePosition,new Vector3(.25f,1.4f,.5f));Push(right.deviceRotation,Quaternion.identity);Call(game,"UpdateVRPose");
  Call(game,"TickVRThrows",10f);int bottles=Field<int>(game,"stones");
  Trigger(right,1f);Call(game,"TickVRThrows",10.02f);
  var held=Field<object>(game,"vrHeldItem");Check(held!=null&&Item<bool>(held,"held"),"Right trigger did not hold bottle");
  Check(Field<object>(game,"vrHeldFlare")==null,"Right trigger also spawned a left-hand flare");
  Check(Field<int>(game,"stones")==bottles,"Holding spent ammunition");
  Call(game,"TickThrows",.5f);Check(Item<float>(held,"age")==0,"Held bottle simulated flight");
  Call(game,"UpdateVRPose");Field<Animator>(game,"vrRightAnimator").Update(.1f);Call(game,"UpdateVRHeldItems");
  Check(Vector3.Distance(Item<Transform>(held,"t").TransformPoint(Vector3.up*.153f),GripCenter(game,"Right"))<.002f,"Bottle neck is outside the fingers");
  var bottleRenderer=Item<GameObject>(held,"vrBottle").GetComponentInChildren<Renderer>();
  Check(Mathf.Abs(bottleRenderer.localBounds.size.y*bottleRenderer.transform.lossyScale.y-.24f)<.02f,"Bottle has incorrect hand-held scale: "+bottleRenderer.bounds.size);
  Check(Vector3.Distance(bottleRenderer.bounds.center,Item<Transform>(held,"t").position)<.02f,"Bottle is not centred in palm: "+bottleRenderer.bounds.center+" vs "+Item<Transform>(held,"t").position);
  Field<Material>(game,"vrComfortMaterial").SetFloat("_Fade",0);Field<Material>(game,"vrComfortMaterial").SetFloat("_Vignette",0);
  Capture(Field<Camera>(game,"eye"),"Logs/Quest-held-bottle.png");
  // The analog trigger is released without a digital click-release event.
  Push(right.trigger,.2f);Push(right.devicePosition,new Vector3(.25f,1.4f,.5f));Call(game,"TickVRThrows",10.04f);
  Check(Field<object>(game,"vrHeldItem")==null&&!Item<bool>(held,"held"),"Release did not detach bottle");
  Check(Item<Vector3>(held,"velocity").magnitude<.001f,"Stationary release adds artificial launch speed");
  Check(Field<int>(game,"stones")==bottles-1,"Release did not consume exactly one bottle");
  Trigger(right,0f);
  Set(game,"cooldown",0f);Call(game,"TickVRThrows",11f);Trigger(right,1f);Call(game,"TickVRThrows",11.02f);
  held=Field<object>(game,"vrHeldItem");
  Push(right.devicePosition,new Vector3(.25f,1.45f,.7f));Push(right.deviceRotation,Quaternion.Euler(20,0,0));Call(game,"UpdateVRPose");Call(game,"TickVRThrows",11.06f);
  Push(right.devicePosition,new Vector3(.25f,1.5f,.9f));Push(right.deviceRotation,Quaternion.Euler(40,0,0));Call(game,"UpdateVRPose");
  Trigger(right,0f);Call(game,"TickVRThrows",11.1f);
  var velocity=Item<Vector3>(held,"velocity");var forward=Field<Transform>(game,"vrOrigin").forward;
  Check(Vector3.Dot(velocity,forward)>3.9f&&Item<Vector3>(held,"angularVelocity").magnitude>5,"Swing speed/spin was not transferred");
  float flight=(velocity.y+Mathf.Sqrt(velocity.y*velocity.y+2*18*1.3f))/18;
  float range=Vector3.ProjectOnPlane(velocity,Vector3.up).magnitude*flight;
  Check(range>6&&range<18,"Deliberate swing does not travel a useful distance: "+range);
  File.WriteAllText("Logs/Quest-throw-range.txt","Simulated 5 m/s forward swing, 1.25 m/s upward, release 1.3 m above water: "+range.ToString("F2")+" m unobstructed range. Stationary release: zero launch velocity.\n");
  Set(game,"cooldown",0f);Call(game,"TickVRThrows",12f);Trigger(right,1f);Call(game,"TickVRThrows",12.02f);
  bottles=Field<int>(game,"stones");Call(game,"Pause");Check(Field<object>(game,"vrHeldItem")==null&&Field<int>(game,"stones")==bottles,"Pause spent/threw held item");
  Call(game,"Resume");Call(game,"TickVRThrows",12.04f);Check(Field<object>(game,"vrHeldItem")==null,"Resume with held trigger spawned an item");
  Trigger(right,0f);Call(game,"TickVRThrows",12.06f);Trigger(right,1f);Call(game,"TickVRThrows",12.08f);
  Push(right.isTracked,0f);Call(game,"UpdateVRPose");Call(game,"TickVRThrows",12.1f);
  Check(Field<object>(game,"vrHeldItem")==null&&Field<int>(game,"stones")==bottles,"Tracking loss launched/spent bottle");
  Trigger(right,0f);Push(right.isTracked,1f);Call(game,"UpdateVRPose");
  Set(game,"cooldown",0f);Call(game,"TickVRThrows",13f);int flares=Field<int>(game,"flares");
  Trigger(left,1f);Call(game,"TickVRThrows",13.02f);var flare=Field<object>(game,"vrHeldFlare");
  Check(Field<object>(game,"vrHeldItem")==null,"Left trigger also spawned a right-hand bottle");
  Check(flare!=null&&Vector3.Distance(Item<GameObject>(flare,"root").transform.position,GripCenter(game,"Left"))<.002f,"Flare not held inside LEFT fingers");
  Call(game,"TickThrows",1f);Check(Item<float>(flare,"age")==0&&Field<int>(game,"flares")==flares,"Held flare activated or spent stock");
  Trigger(left,0f);Call(game,"TickVRThrows",13.04f);Check(Field<int>(game,"flares")==flares-1&&!Item<bool>(flare,"held"),"Flare release failed");
  Call(game,"ResetThrows");
  // A dedicated dry collider in the real pool isolates impact classification.
  Vector3 water=Vector3.zero;bool found=false;
  float end=(float)typeof(SplashGame).GetProperty("EndZ",Flags).GetValue(game);
  Physics.SyncTransforms();
  for(float z=8;z<end-8&&!found;z+=2)for(float x=-5;x<=5&&!found;x+=2){var p=new Vector3(x,.1f,z);if((bool)Call(game,"IsOpenWater",p)){water=p;found=true;}}
  Check(found,"Could not locate test water");
  var platform=GameObject.CreatePrimitive(PrimitiveType.Cube);platform.name="Throw test dry platform";platform.transform.position=water+Vector3.up*.4f;platform.transform.localScale=new Vector3(2,.4f,2);Physics.SyncTransforms();
  try{
   Set(game,"heard",0f);var bottle=Call(game,"LaunchThrownItem",false);Item<Transform>(bottle,"t").position=water+Vector3.up*1.5f;ItemSet(bottle,"velocity",Vector3.down*2);
   for(int i=0;i<50&&Item<bool>(bottle,"active");i++)Call(game,"TickThrows",.02f);
   Check(!Item<bool>(bottle,"active"),"Bottle remained intact on dry floor");
   Check(Field<float>(game,"heard")==0,"Dry bottle attracted monster");
   Check(((Array)Field<object>(game,"bottleShards")).Cast<object>().Any(s=>s!=null&&Item<float>(s,"remaining")>0),"Bottle produced no fragments");
   var stone=Call(game,"LaunchThrownItem",true);Item<Transform>(stone,"t").position=water+Vector3.up*1.5f;ItemSet(stone,"velocity",Vector3.down*2);
   for(int i=0;i<70;i++)Call(game,"TickThrows",.02f);Check(Field<float>(game,"heard")==0,"Dry stone attracted monster");
  }finally{UnityEngine.Object.DestroyImmediate(platform);Physics.SyncTransforms();Call(game,"ResetThrows");}
  Set(game,"heard",0f);var splashBottle=Call(game,"LaunchThrownItem",false);Item<Transform>(splashBottle,"t").position=water+Vector3.up;ItemSet(splashBottle,"velocity",Vector3.down*2);
  for(int i=0;i<50&&Item<bool>(splashBottle,"active");i++)Call(game,"TickThrows",.02f);
  Check(Field<float>(game,"heard")>0&&!Item<bool>(splashBottle,"active"),"Water bottle did not attract monster");
  Call(game,"ResetThrows");Call(game,"ResetAtmosphericFX");Set(game,"heard",0f);
  var splashStone=Call(game,"LaunchThrownItem",true);Item<Transform>(splashStone,"t").position=water+Vector3.up;ItemSet(splashStone,"velocity",Vector3.down*2);
  for(int i=0;i<50&&Item<bool>(splashStone,"active");i++)Call(game,"TickThrows",.02f);
  Check(!Item<bool>(splashStone,"active")&&Field<float>(game,"heard")>0,"Stone did not sink and attract monster on first water impact");
  var waterEffects=((System.Collections.IEnumerable)Field<object>(game,"bursts")).Cast<object>().Where(b=>Item<int>(b,"kind")==0).ToArray();
  Check(waterEffects.Count(b=>Item<float>(b,"remaining")>0)==1,"One stone created multiple water impacts");
  var singleSplash=waterEffects.Single(b=>Item<float>(b,"remaining")>0);Vector3 splashOrigin=Item<GameObject>(singleSplash,"go").transform.position;
  Set(game,"heard",0f);
  for(int i=0;i<100;i++){
   Call(game,"TickThrows",.02f);Call(game,"TickEconomy",.02f);Call(game,"StepFX",.02f);
   Check(waterEffects.Count(b=>Item<float>(b,"remaining")>0)==1,"Sunk stone produced delayed skipping splashes");
   Check(Item<GameObject>(singleSplash,"go").transform.position==splashOrigin,"Ripple centre travelled after water impact");
   Check(Field<float>(game,"heard")==0,"Sunk stone emitted another attraction noise");
  }
  File.WriteAllText("Logs/Quest-water-impact-checks.txt","PASS: stone sinks on first impact; one fixed splash centre; no delayed skipping ripples or attraction noises over 2 seconds.\n");
  Call(game,"ResetAtmosphericFX");
  Call(game,"ResetThrows");Set(game,"heard",0f);Set(game,"cooldown",0f);
  int stock=Field<int>(game,"stones"),reserve=Field<int>(game,"reserveBottles");Set(game,"stones",0);Set(game,"reserveBottles",0);
  Call(game,"TickVRThrows",15f);Trigger(right,1f);Call(game,"TickVRThrows",15.02f);
  Check(Field<object>(game,"vrHeldItem")!=null&&Item<bool>(Field<object>(game,"vrHeldItem"),"pebble"),"Empty bottle stock did not provide held pebble");
  Call(game,"CancelVRThrows");Trigger(right,0f);Set(game,"stones",stock);Set(game,"reserveBottles",reserve);
 }
 static void CheckMenuPointerPixels(SplashGame game,Camera eye){
  var ray=Field<LineRenderer>(game,"vrRay");var cursor=Field<Transform>(game,"vrCursor");
  cursor.gameObject.SetActive(false);ray.enabled=false;Capture(eye,"Logs/Quest-pointer-off.png");
  ray.enabled=true;Capture(eye,"Logs/Quest-ray.png");
  CheckImageDifference("Logs/Quest-pointer-off.png","Logs/Quest-ray.png",20,"Ray hidden by menu");
  cursor.gameObject.SetActive(true);Capture(eye,"Logs/Quest-pointer.png");
  CheckImageDifference("Logs/Quest-ray.png","Logs/Quest-pointer.png",10,"Cursor hidden by menu");
 }
 static void CheckImageDifference(string before,string after,int minimum,string message){
  var a=new Texture2D(2,2);var b=new Texture2D(2,2);
  try{a.LoadImage(File.ReadAllBytes(before));b.LoadImage(File.ReadAllBytes(after));int changed=0;
   for(int y=250;y<580;y++)for(int x=400;x<880;x++){var p=a.GetPixel(x,y);var q=b.GetPixel(x,y);if(Mathf.Abs(p.r-q.r)+Mathf.Abs(p.g-q.g)+Mathf.Abs(p.b-q.b)>.2f)changed++;}
   Check(changed>minimum,message+": changed pixels="+changed);
  }finally{UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
 }
 static Vector3 PalmAxis(Transform grip,bool right){
  var bones=grip.GetComponentsInChildren<Transform>();var wrist=bones.First(t=>t.name=="XRHand_Wrist");
  var index=bones.First(t=>t.name=="XRHand_IndexMetacarpal");var little=bones.First(t=>t.name=="XRHand_LittleMetacarpal");
  return grip.InverseTransformDirection(Vector3.Cross(index.position-wrist.position,little.position-wrist.position).normalized)*(right?-1:1);
 }
 static void CheckMenuHandPixels(){
  var snapshot=new Texture2D(2,2);
  try{
   snapshot.LoadImage(File.ReadAllBytes("Logs/Quest-menu.png"));int visible=0;
   // The cyan left hand is deliberately held over the dark centre of the menu.
   for(int y=300;y<480;y++)for(int x=380;x<620;x++){
    var pixel=snapshot.GetPixel(x,y);if(pixel.g>pixel.r+.08f&&pixel.b>pixel.r+.04f)visible++;
   }
   Check(visible>100,"Menu painted over the hand in the rendered image");
  }finally{UnityEngine.Object.DestroyImmediate(snapshot);}
 }
 static Vector3 FingerAxis(Transform grip){
  var bones=grip.GetComponentsInChildren<Transform>();var wrist=bones.First(t=>t.name=="XRHand_Wrist");
  var index=bones.First(t=>t.name=="XRHand_IndexMetacarpal");var little=bones.First(t=>t.name=="XRHand_LittleMetacarpal");
  return grip.InverseTransformDirection(((index.position+little.position)*.5f-wrist.position).normalized);
 }
 static void AdvanceWrist(SplashGame game,float seconds){for(int i=0;i<Mathf.CeilToInt(seconds/.02f);i++)Call(game,"TickVRWrist",.02f);}
 static void OrientPalm(SplashGame game,OculusTouchControllerProfile.OculusTouchController left,Vector3 normal){
  Call(game,"UpdateVRPose");Field<Animator>(game,"vrLeftAnimator").Update(.1f);
  var grip=Field<Transform>(game,"vrLeftGrip");
  var current=(Vector3)typeof(SplashGame).GetProperty("VRPalmNormal",Flags).GetValue(game);
  Push(left.deviceRotation,Quaternion.Inverse(grip.parent.rotation)*Quaternion.FromToRotation(current,normal)*grip.rotation);
  Call(game,"UpdateVRPose");Field<Animator>(game,"vrLeftAnimator").Update(.1f);
 }
 static void Aim(SplashGame game,OculusTouchControllerProfile.OculusTouchController right,string caption){
  var canvas=Field<Canvas>(game,"uiCanvas");var hand=Field<Transform>(game,"vrRight");
  var button=canvas.GetComponentsInChildren<Button>().First(b=>b.gameObject.name.EndsWith(" / "+caption));
  var rect=(RectTransform)button.transform;var target=rect.TransformPoint(rect.rect.center);
  Push(right.pointerRotation,Quaternion.Inverse(hand.parent.rotation)*Quaternion.LookRotation(target-hand.position));Call(game,"UpdateVRPose");
 }
 static void Click(SplashGame game,OculusTouchControllerProfile.OculusTouchController right,string caption){
  Aim(game,right,caption);
  Trigger(right,1f);Call(game,"TickVRPointer",true);
  Check(Field<Transform>(game,"vrCursor").gameObject.activeSelf,"Menu cursor is not visible");
  Trigger(right,0f);Call(game,"TickVRPointer",true);
 }
 static void Capture(Camera camera,string path){
  var previous=camera.targetTexture;var active=RenderTexture.active;var target=new RenderTexture(1280,832,24);
  // Several poses are rendered in one editor frame; invalidate Unity's skinning cache.
  var skins=UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
  var recalculate=skins.Select(s=>s.forceMatrixRecalculationPerRender).ToArray();
  foreach(var skin in skins)skin.forceMatrixRecalculationPerRender=true;
  var image=new Texture2D(1280,832,TextureFormat.RGB24,false);
  try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,832),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
  finally{for(int i=0;i<skins.Length;i++)if(skins[i])skins[i].forceMatrixRecalculationPerRender=recalculate[i];camera.targetTexture=previous;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);}
 }
}
}
