using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

namespace DontSplash {
public sealed partial class SplashGame {
 bool vr, vrSnapReady=true, vrOriginReady, vrHeadReady;
 InputActionAsset vrActions;
 Transform vrOrigin, vrLeft, vrRight;
 Transform vrLeftGrip, vrRightGrip, vrCursor;
 Animator vrLeftAnimator, vrRightAnimator;
 RectTransform vrWrist;
 Transform vrPalm,vrRightPalm,vrWristBone,vrIndexBase,vrLittleBase;
 CanvasGroup vrWristFade;
 bool vrWristOpen;
 float vrWristAmount,vrWristGestureTime;
 Vector3 VRPalmNormal=>Vector3.Cross(vrIndexBase.position-vrWristBone.position,vrLittleBase.position-vrWristBone.position).normalized;
 LineRenderer vrRay;
 Material vrRayMaterial, vrComfortMaterial;
 Button vrHovered, vrPressed;
 Button[] vrButtons;
 PointerEventData vrPointerData;
 Material vrUI,vrWristUI;
 readonly List<Material> vrHandMaterials=new List<Material>();
 readonly List<SkinnedMeshRenderer> vrHandRenderers=new List<SkinnedMeshRenderer>();
 readonly Dictionary<Material,Material> vrFonts=new Dictionary<Material,Material>();
 readonly Dictionary<Material,Material> vrWristFonts=new Dictionary<Material,Material>();
 Mode vrPanelMode=(Mode)(-1);
 float vrFade, vrVignette,vrHandMotion;
 readonly List<XRInputSubsystem> vrSubsystems=new List<XRInputSubsystem>();
 InputAction VA(string name)=>vrActions.FindAction(name,true);
 bool VRPressed(string name)=>VA(name).WasPressedThisFrame();
 bool VRHeld(string name)=>VA(name).IsPressed();
 Transform ThrowOrigin=>vr&&vrRight.gameObject.activeSelf?vrRight:eye.transform;

 void TryInitializeVR(bool preview=false){
  if(vr||(!preview&&!XRSettings.isDeviceActive))return;
  vr=true;touchMode=false;Application.targetFrameRate=72;
  vrActions=Resources.Load<InputActionAsset>("QuestControls");vrActions.Enable();
  vrOrigin=new GameObject("XR tracking space").transform;vrOrigin.SetParent(body.transform,false);
  eye.transform.SetParent(vrOrigin,false);eye.nearClipPlane=.07f;eye.stereoTargetEye=StereoTargetEyeMask.Both;
  eye.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;
  vrLeft=CreateVRHand("Left",out vrLeftGrip,out vrLeftAnimator);
  vrRight=CreateVRHand("Right",out vrRightGrip,out vrRightAnimator);
  foreach(var bone in vrRightGrip.GetComponentsInChildren<Transform>())if(bone.name=="XRHand_Palm")vrRightPalm=bone;
  foreach(var bone in vrLeftGrip.GetComponentsInChildren<Transform>()){
   if(bone.name=="XRHand_Palm")vrPalm=bone;
   if(bone.name=="XRHand_Wrist")vrWristBone=bone;
   if(bone.name=="XRHand_IndexMetacarpal")vrIndexBase=bone;
   if(bone.name=="XRHand_LittleMetacarpal")vrLittleBase=bone;
  }
  vrRay=vrRight.gameObject.AddComponent<LineRenderer>();vrRay.useWorldSpace=true;vrRay.positionCount=2;
  vrRay.startWidth=.006f;vrRay.endWidth=.002f;
  vrRayMaterial=new Material(Shader.Find("DontSplash/VRUI")){renderQueue=3200};vrRay.sharedMaterial=vrRayMaterial;
  var cursor=GameObject.CreatePrimitive(PrimitiveType.Sphere);cursor.name="VR menu cursor";
  Destroy(cursor.GetComponent<Collider>());vrCursor=cursor.transform;vrCursor.SetParent(vrOrigin,false);
  vrCursor.localScale=Vector3.one*.018f;cursor.GetComponent<Renderer>().sharedMaterial=vrRayMaterial;cursor.SetActive(false);
  var shade=GameObject.CreatePrimitive(PrimitiveType.Quad);shade.name="VR comfort fade";
  Destroy(shade.GetComponent<Collider>());shade.transform.SetParent(eye.transform,false);
  shade.transform.localPosition=Vector3.forward*.12f;shade.transform.localScale=Vector3.one*2;
  vrComfortMaterial=new Material(Shader.Find("DontSplash/VRComfort"));shade.GetComponent<Renderer>().sharedMaterial=vrComfortMaterial;
  shade.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  Application.onBeforeRender+=UpdateVRPose;
  ConfigureVRCanvas();UpdateVRPose();ResetVRPosition();
  Cursor.lockState=CursorLockMode.None;Cursor.visible=false;
 }
 Transform CreateVRHand(string side,out Transform grip,out Animator animator){
  var pointer=new GameObject(side+" aim pose").transform;pointer.SetParent(vrOrigin,false);
  grip=Instantiate(Resources.Load<GameObject>("VR/"+side+"Hand"),vrOrigin).transform;
  animator=grip.GetComponentInChildren<Animator>();
  foreach(var renderer in grip.GetComponentsInChildren<SkinnedMeshRenderer>()){
   var material=new Material(renderer.sharedMaterial);renderer.sharedMaterial=material;vrHandMaterials.Add(material);vrHandRenderers.Add(renderer);
  }
  return pointer;
 }
 void ConfigureVRCanvas(){
  if(!vr||!uiCanvas)return;
  uiCanvas.renderMode=RenderMode.WorldSpace;uiCanvas.worldCamera=eye;
  var rect=(RectTransform)uiCanvas.transform;rect.sizeDelta=new Vector2(1000,650);rect.pivot=new Vector2(.5f,.5f);
  rect.localScale=Vector3.one*.0015f;uiScaler.dynamicPixelsPerUnit=1;uiScaler.scaleFactor=1;
  if(EventSystem.current){var module=EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();if(module)module.enabled=false;}
  uiKey="";nextUIRefresh=0;vrPanelMode=(Mode)(-1);
 }
 void RefreshVRUI(){
  if(!vr||!uiRoot)return;
  vrButtons=uiRoot.GetComponentsInChildren<Button>(true);vrPressed=null;vrHovered=null;
  if(!vrUI)vrUI=new Material(Shader.Find("DontSplash/VRUI"));
  if(!vrWristUI){vrWristUI=new Material(Shader.Find("DontSplash/VRUI")){renderQueue=3150};vrWristUI.SetFloat("_ZTest",4);}
  foreach(var graphic in uiRoot.GetComponentsInChildren<Graphic>(true)){
   bool wrist=vrWrist&&graphic.transform.IsChildOf(vrWrist);
   if(graphic is TMPro.TMP_Text text){
    var source=text.fontSharedMaterial;
    var fonts=wrist?vrWristFonts:vrFonts;
    if(!fonts.TryGetValue(source,out var material)){
     material=new Material(source);material.shader=Shader.Find(wrist?"TextMeshPro/Mobile/Distance Field":"TextMeshPro/Mobile/Distance Field Overlay");material.renderQueue=wrist?3150:3100;fonts[source]=material;
    }
    text.fontSharedMaterial=material;
   }else graphic.material=wrist?vrWristUI:vrUI;
  }
 }
 void UpdateVRPose(){
  if(!vr||!eye||vrActions==null)return;
  if(!vrOriginReady){
   SubsystemManager.GetSubsystems(vrSubsystems);
   foreach(var subsystem in vrSubsystems)if(subsystem.running){
    bool floor=subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
    vrOrigin.localPosition=new Vector3(0,floor?0:1.6f,0);vrOriginReady=true;break;
   }
  }
  if(VRHeld("HeadTracked")){
   eye.transform.localPosition=VA("HeadPosition").ReadValue<Vector3>();eye.transform.localRotation=VA("HeadRotation").ReadValue<Quaternion>();
   if(!vrHeadReady){vrHeadReady=true;ResetVRPosition();}
  }
  UpdateVRHand(vrLeft,"Left");UpdateVRHand(vrRight,"Right");
  UpdateVRGrip(vrLeftGrip,vrLeftAnimator,"Left");UpdateVRGrip(vrRightGrip,vrRightAnimator,"Right");
 }
 void UpdateVRHand(Transform hand,string side){
  bool tracked=VRHeld(side+"Tracked");hand.gameObject.SetActive(tracked);
  if(!tracked)return;
  hand.localPosition=VA(side+"Position").ReadValue<Vector3>();hand.localRotation=VA(side+"Rotation").ReadValue<Quaternion>();
 }
 void UpdateVRGrip(Transform grip,Animator animator,string side){
  bool tracked=VRHeld(side+"Tracked");grip.gameObject.SetActive(tracked);if(!tracked)return;
  grip.localPosition=VA(side+"GripPosition").ReadValue<Vector3>();grip.localRotation=VA(side+"GripRotation").ReadValue<Quaternion>();
  float flex=VA(side+"Grip").ReadValue<float>(),pinch=VA(side+"Trigger").ReadValue<float>();
  bool right=side=="Right",menu=mode!=Mode.Play&&mode!=Mode.Caught;
  // Menu input belongs to the right hand. Some simulator presets send the
  // same keyboard/mouse press to both controllers; the left stays relaxed.
  if(menu){if(!right){flex=0;pinch=0;}else pinch=Mathf.Max(pinch,VRHeld("Select")?1:0);}
  else if(right?vrHeldItem!=null:vrHeldFlare!=null){flex=Mathf.Max(flex,.72f);pinch=1;}
  animator.SetFloat("Flex",VRHandFlex(flex,vrHandMotion,Time.unscaledTime,side=="Right"));animator.SetFloat("Pinch",pinch);
  animator.SetLayerWeight(animator.GetLayerIndex("Point Layer"),1-pinch);
 }
 float VRHandFlex(float input,float motion,float time,bool right){
  float phase=right?Mathf.PI:0;
  float relaxed=.10f+.025f*Mathf.Sin(time*1.8f+phase);
  float running=.26f+.075f*Mathf.Sin(time*8+phase);
  // Animate only relaxed fingers. Grip/trigger inputs and tracked wrist poses win.
  return Mathf.Lerp(Mathf.Lerp(relaxed,running,motion),1,Mathf.Clamp01(input));
 }
 void ResetVRPosition(){
  if(!vr)return;
  CancelVRThrows();
  if(VRHeld("HeadTracked")){
   eye.transform.localPosition=VA("HeadPosition").ReadValue<Vector3>();
   eye.transform.localRotation=VA("HeadRotation").ReadValue<Quaternion>();
  }
  body.transform.rotation=Quaternion.identity;
  vrOrigin.localRotation=Quaternion.Euler(0,-eye.transform.localEulerAngles.y,0);
  Vector3 offset=vrOrigin.localRotation*eye.transform.localPosition;
  vrOrigin.localPosition=new Vector3(-offset.x,vrOrigin.localPosition.y,-offset.z);
  vrPanelMode=(Mode)(-1);vrFade=1;
 }
 void TickVR(){
  TryInitializeVR();if(!vr)return;
  vrHandMotion=Mathf.MoveTowards(vrHandMotion,mode==Mode.Play?Mathf.Clamp01(move.magnitude):0,Time.unscaledDeltaTime*4);
  UpdateVRPose();
  if(!VRHeld("HeadTracked")){if(mode==Mode.Play)Pause();return;}
  var head=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.Head);
  if(head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.userPresence,out bool present)&&!present&&mode==Mode.Play)Pause();
  if(VRPressed("Pause")||VRPressed("Menu")){
   if(mode==Mode.Play)Pause();else if(mode==Mode.Shop)CloseShop();else if(mode==Mode.Pause&&!codeOpen)Resume();
  }
  if(VRPressed("Recenter"))vrPanelMode=(Mode)(-1);
  // Move the capsule through collision detection, then compensate the tracking space.
  // Changing capsule.center directly would let room-scale walking bypass walls.
  Vector3 local=body.transform.InverseTransformPoint(eye.transform.position);
  body.height=Mathf.Clamp(local.y+.12f,.7f,2.3f);body.center=Vector3.up*(body.height*.5f);
  if(mode==Mode.Play){
   Vector3 delta=Vector3.ProjectOnPlane(eye.transform.position-body.transform.position,Vector3.up);
   if(delta.sqrMagnitude>.000001f){body.Move(delta);vrOrigin.position-=delta;}
  }
 }
 void ReadVRInput(){
  move=VA("Move").ReadValue<Vector2>();if(move.magnitude<.18f)move=Vector2.zero;
  move=Vector2.ClampMagnitude(move,1);look=Vector2.zero;
  jump|=VRPressed("Jump");use=VRHeld("Use");
  TickVRThrows(Time.unscaledTime);
  float turn=VA("Turn").ReadValue<Vector2>().x;
  if(Mathf.Abs(turn)<.3f)vrSnapReady=true;
  if(vrSnapReady&&Mathf.Abs(turn)>.7f){
   body.transform.RotateAround(eye.transform.position,Vector3.up,Mathf.Sign(turn)*30);vrSnapReady=false;vrFade=Mathf.Max(vrFade,.6f);
  }
 }
 Vector3 VRMoveDirection(){
  Vector3 forward=Vector3.ProjectOnPlane(eye.transform.forward,Vector3.up).normalized;
  if(forward.sqrMagnitude<.01f)forward=body.transform.forward;
  return (forward*move.y+Vector3.Cross(Vector3.up,forward)*move.x)*RunSpeed;
 }
 void LateVR(){
  if(!vr)return;
  if(mode!=Mode.Play)CancelVRThrows();else UpdateVRHeldItems();
  bool menu=mode!=Mode.Play&&mode!=Mode.Caught;
  // Menus must remain readable through scenery/the frozen death animation.
  // Their hands are kept in front of the panel and rendered after its graphics.
  // The hand shader still depth-tests against the world; gameplay uses normal depth.
  foreach(var material in vrHandMaterials)material.renderQueue=menu?3175:2000;
  foreach(var renderer in vrHandRenderers){renderer.sortingLayerID=uiCanvas.sortingLayerID;renderer.sortingOrder=menu?uiCanvas.sortingOrder+2:0;}
  if(vrPanelMode!=mode){
   vrPanelMode=mode;vrPressed=null;vrHovered=null;
   PlaceVRMenu();
  }
  if(menu)KeepVRMenuReachable();
  else uiCanvas.transform.SetPositionAndRotation(eye.transform.position+eye.transform.forward*1.1f,eye.transform.rotation);
  uiCanvas.transform.localScale=Vector3.one*(menu?.0015f:.0011f);
  UpdateVRWrist();
  if(deathBlackout)deathBlackout.color=Color.clear;
  TickVRPointer(menu);
  float desired=!VRHeld("HeadTracked")?1:mode==Mode.Caught?CatchBlackout:0;
  vrFade=Mathf.MoveTowards(vrFade,desired,Time.unscaledDeltaTime*3);
  vrVignette=Mathf.MoveTowards(vrVignette,mode==Mode.Play&&(move.sqrMagnitude>.05f||!body.isGrounded)?.7f:0,Time.unscaledDeltaTime*3);
  vrComfortMaterial.SetFloat("_Fade",vrFade);vrComfortMaterial.SetFloat("_Vignette",vrVignette);
 }
 void PlaceVRMenu(){
  Vector3 forward=Vector3.ProjectOnPlane(eye.transform.forward,Vector3.up).normalized;
  if(forward.sqrMagnitude<.01f)forward=Vector3.forward;
  uiCanvas.transform.SetPositionAndRotation(eye.transform.position+forward*1.5f,Quaternion.LookRotation(forward));
 }
 void KeepVRMenuReachable(){
  var panel=uiCanvas.transform;Vector3 normal=panel.forward;
  float headDistance=Vector3.Dot(panel.position-eye.transform.position,normal);
  if(headDistance<.9f||Vector3.Distance(panel.position,eye.transform.position)>3){PlaceVRMenu();normal=panel.forward;}
  // Tracked hands cannot be stopped by a collider. Keep the menu beyond both
  // aim poses and the full hand meshes, so a forward ray stays in front of it.
  float advance=Mathf.Max(Mathf.Max(VRHandClearance(vrLeft),VRHandClearance(vrRight)),Mathf.Max(VRHandClearance(vrLeftGrip),VRHandClearance(vrRightGrip)));
  if(advance>0)panel.position+=normal*advance;
 }
 float VRHandClearance(Transform hand)=>hand.gameObject.activeSelf?Mathf.Max(0,Vector3.Dot(hand.position-uiCanvas.transform.position,uiCanvas.transform.forward)+.3f):0;
 void UpdateVRWrist()=>TickVRWrist(Time.unscaledDeltaTime);
 void TickVRWrist(float dt){
  if(!vrWrist)return;
  if(mode!=Mode.Play||!VRHeld("HeadTracked")||!vrLeftGrip.gameObject.activeSelf||!vrPalm){
   vrWristOpen=false;vrWristAmount=vrWristGestureTime=0;vrWristFade.alpha=0;vrWrist.gameObject.SetActive(false);return;
  }
  Vector3 palm=vrPalm.position,fromHead=palm-eye.transform.position;
  float up=Vector3.Dot(VRPalmNormal,Vector3.up);
  // Different opening/closing thresholds prevent flicker near the gesture boundary.
  bool gesture=up>(vrWristOpen?.35f:.6f)&&fromHead.y>(vrWristOpen?-.6f:-.48f)
   &&fromHead.magnitude>.25f&&fromHead.magnitude<1.2f&&Vector3.Dot(fromHead,eye.transform.forward)>.08f;
  if(gesture==vrWristOpen)vrWristGestureTime=0;
  else if((vrWristGestureTime+=dt)>=(gesture?.12f:.08f)){vrWristOpen=gesture;vrWristGestureTime=0;}
  vrWristAmount=Mathf.MoveTowards(vrWristAmount,vrWristOpen?1:0,dt/(vrWristOpen?.24f:.18f));
  float show=Mathf.SmoothStep(0,1,vrWristAmount);
  vrWristFade.alpha=show;vrWrist.gameObject.SetActive(show>0);
  if(show<=0)return;
  Vector3 position=palm+Vector3.up*(.15f*show);
  vrWrist.SetPositionAndRotation(position,Quaternion.LookRotation(position-eye.transform.position,Vector3.up));
  vrWrist.localScale=Vector3.one*(.0007f*show/uiCanvas.transform.localScale.x);
 }
 void TickVRPointer(bool menu){
  // Transparent sorting order takes precedence over shader queue. Match the
  // canvas layer and draw the line AND cursor after every menu graphic/hand.
  vrRay.sortingLayerID=uiCanvas.sortingLayerID;vrRay.sortingOrder=uiCanvas.sortingOrder+3;
  var cursorRenderer=vrCursor.GetComponent<Renderer>();
  cursorRenderer.sortingLayerID=uiCanvas.sortingLayerID;cursorRenderer.sortingOrder=uiCanvas.sortingOrder+4;
  vrRay.enabled=menu&&vrRight.gameObject.activeSelf;
  vrCursor.gameObject.SetActive(false);
  if(!vrRay.enabled||vrButtons==null){CancelVRPointer();return;}
  var ray=new Ray(vrRight.position,vrRight.forward);var plane=new Plane(uiCanvas.transform.forward,uiCanvas.transform.position);
  Button hit=null;Vector3 end=ray.GetPoint(4);
  if(Vector3.Dot(ray.direction,uiCanvas.transform.forward)>.01f&&plane.Raycast(ray,out float distance)&&distance<6){
   end=ray.GetPoint(distance);
   var panelRect=(RectTransform)uiCanvas.transform;
   if(panelRect.rect.Contains(panelRect.InverseTransformPoint(end))){vrCursor.gameObject.SetActive(true);vrCursor.position=end-uiCanvas.transform.forward*.006f;}
   // Rect tests use the controller ray, independent of either eye's projection.
   foreach(var button in vrButtons){
    if(!button||!button.gameObject.activeInHierarchy||!button.enabled||!button.IsInteractable())continue;
    var rect=(RectTransform)button.transform;
    if(rect.rect.Contains(rect.InverseTransformPoint(end)))hit=button;
   }
  }
  vrRay.SetPosition(0,ray.origin);vrRay.SetPosition(1,end);
  vrRay.startColor=vrRay.endColor=hit?new Color(1,.85f,.3f):new Color(.65f,1,.85f);
  vrCursor.localScale=Vector3.one*(hit?.024f:.014f);
  if(vrPointerData==null)vrPointerData=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
  var data=vrPointerData;
  if(hit!=vrHovered){
   if(vrHovered)ExecuteEvents.Execute(vrHovered.gameObject,data,ExecuteEvents.pointerExitHandler);
   vrHovered=hit;if(hit)ExecuteEvents.Execute(hit.gameObject,data,ExecuteEvents.pointerEnterHandler);
  }
  if(VRPressed("Select")){vrPressed=hit;if(hit)ExecuteEvents.Execute(hit.gameObject,data,ExecuteEvents.pointerDownHandler);}
  if(VA("Select").WasReleasedThisFrame()){
   var pressed=vrPressed;vrPressed=null;
   if(pressed){ExecuteEvents.Execute(pressed.gameObject,data,ExecuteEvents.pointerUpHandler);if(pressed==hit)ExecuteEvents.Execute(pressed.gameObject,data,ExecuteEvents.pointerClickHandler);}
  }
 }
 void CancelVRPointer(){
  if(vrPointerData!=null){
   if(vrPressed)ExecuteEvents.Execute(vrPressed.gameObject,vrPointerData,ExecuteEvents.pointerUpHandler);
   if(vrHovered)ExecuteEvents.Execute(vrHovered.gameObject,vrPointerData,ExecuteEvents.pointerExitHandler);
  }
  vrPressed=null;vrHovered=null;
 }
 void DisposeVR(){
  Application.onBeforeRender-=UpdateVRPose;
  if(vrActions)vrActions.Disable();if(vrRayMaterial)Destroy(vrRayMaterial);if(vrComfortMaterial)Destroy(vrComfortMaterial);
  if(vrUI)Destroy(vrUI);if(vrWristUI)Destroy(vrWristUI);
  foreach(var material in vrFonts.Values)Destroy(material);foreach(var material in vrWristFonts.Values)Destroy(material);
  foreach(var material in vrHandMaterials)Destroy(material);
 }
}
}
