using UnityEngine;

namespace DontSplash {
public sealed partial class SplashGame {
 ThrownItem vrHeldItem;
 FlyingFlare vrHeldFlare;
 bool vrThrowReady,vrFlareReady;
 readonly HandThrowMotion vrRightMotion=new HandThrowMotion(),vrLeftMotion=new HandThrowMotion();
 VRGrasp vrRightGrasp,vrLeftGrasp;
 // The finger curl encloses the handle. Palm/wrist bones are inside the mesh,
 // so attaching at either bone puts objects through the back of the hand.
 sealed class VRGrasp {
  readonly Transform root,wrist,palm,index,middle,intermediate,distal,tip,little;
  readonly Animator animator;
  readonly Transform[][] fingers=new Transform[5][];
  sealed class GripPose {public Vector3 center;public Quaternion rotation;public Quaternion[][] fingers;}
  readonly GripPose[] poses=new GripPose[3];GripPose pose;
  readonly bool right;
  public VRGrasp(Transform root,bool isRight){
   this.root=root;right=isRight;animator=root.GetComponentInChildren<Animator>();
   foreach(var t in root.GetComponentsInChildren<Transform>())switch(t.name){
    case "XRHand_Wrist":wrist=t;break;case "XRHand_Palm":palm=t;break;case "XRHand_IndexProximal":index=t;break;
    case "XRHand_MiddleProximal":middle=t;break;case "XRHand_MiddleIntermediate":intermediate=t;break;
    case "XRHand_MiddleDistal":distal=t;break;case "XRHand_MiddleTip":tip=t;break;
    case "XRHand_LittleProximal":little=t;break;
   }
   string[] names={"Index","Middle","Ring","Little","Thumb"};var bones=root.GetComponentsInChildren<Transform>();
   for(int f=0;f<5;f++){
    string[] joints=f==4?new[]{"Metacarpal","Proximal","Distal","Tip"}:new[]{"Proximal","Intermediate","Distal","Tip"};
    fingers[f]=new Transform[4];for(int j=0;j<4;j++)foreach(var bone in bones)if(bone.name=="XRHand_"+names[f]+joints[j])fingers[f][j]=bone;
   }
  }
  public Vector3 Center=>root.TransformPoint(pose.center);
  public Quaternion Rotation=>root.rotation*pose.rotation;
  Quaternion AnatomicalRotation{
   get{
    Vector3 axis=(index.position-little.position).normalized;
    Vector3 palmNormal=Vector3.Cross(index.position-wrist.position,little.position-wrist.position).normalized*(right?-1:1);
    return Quaternion.LookRotation(Vector3.ProjectOnPlane(palmNormal,axis).normalized,axis);
   }
  }
  public void ApplyPose(int kind){
   pose=poses[kind];
   if(pose==null){
    float curl=kind==0?.58f:kind==1?.72f:.68f;
    animator.SetFloat("Flex",curl);animator.SetFloat("Pinch",1);animator.SetLayerWeight(animator.GetLayerIndex("Point Layer"),0);animator.Update(0);
    Quaternion rotation=AnatomicalRotation;Vector3 axis=rotation*Vector3.up,normal=rotation*Vector3.forward;
    Vector3 fingerward=Vector3.ProjectOnPlane(middle.position-wrist.position,normal).normalized;
    // Move the grip out of the heel of the palm into the cup below the knuckles.
    Vector3 center=palm.position+normal*(kind==0?.029f:.022f)+fingerward*.028f;
    float radius=kind==0?.03f:kind==1?.014f:.01575f;
    pose=new GripPose{center=root.InverseTransformPoint(center),rotation=Quaternion.Inverse(root.rotation)*rotation,fingers=new Quaternion[5][]};
    var open=new Quaternion[5,3];var closed=new Quaternion[5,3];
    animator.SetFloat("Flex",0);animator.SetFloat("Pinch",0);animator.Update(0);
    for(int f=0;f<5;f++)for(int j=0;j<3;j++)open[f,j]=fingers[f][j].localRotation;
    animator.SetFloat("Flex",1);animator.SetFloat("Pinch",1);animator.Update(0);
    for(int f=0;f<5;f++)for(int j=0;j<3;j++)closed[f,j]=fingers[f][j].localRotation;
    for(int f=0;f<5;f++){
     var chain=fingers[f];float best=float.PositiveInfinity,bestCurl=curl;
     // Keep each finger's three joints coupled as in the authored curl.
     // Independent joint searches can produce a straight knuckle and hooked
     // fingertip, even when their contact distances happen to be correct.
     for(int sample=0;sample<=80;sample++){
      float candidate=sample/80f;for(int j=0;j<3;j++)chain[j].localRotation=Quaternion.Slerp(open[f,j],closed[f,j],candidate);
      float cost=FingerCost(chain,center,axis,radius,kind==0)+.00002f*(candidate-curl)*(candidate-curl);
      // A thumb parallel to an infinite handle satisfies radial contact but
      // looks like a thumbs-up. Close it across the front, opposite the fingers.
      if(f==4&&kind!=0)cost+=(chain[3].position-(center+normal*(radius+.01f)+axis*.012f)).sqrMagnitude*12;
      if(cost<best){best=cost;bestCurl=candidate;}
     }
     for(int j=0;j<3;j++)chain[j].localRotation=Quaternion.Slerp(open[f,j],closed[f,j],bestCurl);
     pose.fingers[f]=new Quaternion[3];for(int j=0;j<3;j++)pose.fingers[f][j]=chain[j].localRotation;
    }
    poses[kind]=pose;
   }
   // Reapply the cached grasp after animation and late tracking. This is
   // idempotent: late-render passes cannot accumulate finger rotations.
   for(int f=0;f<5;f++)for(int j=0;j<3;j++)fingers[f][j].localRotation=pose.fingers[f][j];
  }
  static float FingerCost(Transform[] chain,Vector3 center,Vector3 axis,float radius,bool sphere){
   float cost=0,clearance=radius+.007f;
   for(int segment=0;segment<3;segment++)for(int sample=1;sample<=4;sample++){
    Vector3 delta=Vector3.Lerp(chain[segment].position,chain[segment+1].position,sample*.25f)-center;
    float distance=(sphere?delta:Vector3.ProjectOnPlane(delta,axis)).magnitude;
    float penetration=Mathf.Max(0,clearance-distance);cost+=penetration*penetration*100;
   }
   Vector3 tip=chain[3].position-center;float gap=(sphere?tip:Vector3.ProjectOnPlane(tip,axis)).magnitude-clearance;
   return cost+gap*gap*2;
  }
 }
 static Vector3 BottleNeckAnchor=>Vector3.up*.153f; // 81.6 mm above the centre of the 24 cm bottle.

 // Use recent tracking-space samples, so locomotion/recentring/snap turns do
 // not become an artificial throw. The short window smooths tracking jitter.
 sealed class HandThrowMotion {
  readonly Vector3[] positions=new Vector3[16];
  readonly Quaternion[] rotations=new Quaternion[16];
  readonly float[] times=new float[16];
  int count,next;
  public void Clear(){count=next=0;}
  public void Sample(Vector3 position,Quaternion rotation,float time){
   if(count>0&&time<=times[(next+15)%16])return;
   positions[next]=position;rotations[next]=rotation;times[next]=time;
   next=(next+1)%16;count=Mathf.Min(count+1,16);
  }
  public void Velocity(out Vector3 linear,out Vector3 angular){
   linear=angular=Vector3.zero;if(count<2)return;
   int last=(next+15)%16,first=last;
   for(int i=1;i<count;i++){int index=(last-i+16)%16;if(times[last]-times[index]>.085f)break;first=index;}
   float dt=times[last]-times[first];if(dt<.008f)return;
   linear=Vector3.ClampMagnitude((positions[last]-positions[first])/dt,14);
   var delta=rotations[last]*Quaternion.Inverse(rotations[first]);
   delta.ToAngleAxis(out float angle,out Vector3 axis);if(angle>180)angle-=360;
   if(axis.sqrMagnitude>.001f&&Mathf.Abs(angle)>.001f)angular=Vector3.ClampMagnitude(axis*(angle*Mathf.Deg2Rad/dt),25);
  }
 }
 void CancelVRThrows(){
  if(vrHeldItem!=null){vrHeldItem.held=vrHeldItem.active=false;vrHeldItem.t.gameObject.SetActive(false);vrHeldItem=null;}
  if(vrHeldFlare!=null){vrHeldFlare.held=vrHeldFlare.active=false;vrHeldFlare.root.SetActive(false);vrHeldFlare=null;}
  vrThrowReady=vrFlareReady=false;vrRightMotion.Clear();vrLeftMotion.Clear();
 }
 void TickVRThrows(float time){
  if(!VRCanInteract||mode!=Mode.Play||!VRHeld("HeadTracked")){CancelVRThrows();return;}
  bool right=VRHeld("RightTracked"),left=VRHeld("LeftTracked");
  if(right)vrRightMotion.Sample(VA("RightGripPosition").ReadValue<Vector3>(),VA("RightGripRotation").ReadValue<Quaternion>(),time);
  else{if(vrHeldItem!=null){vrHeldItem.held=vrHeldItem.active=false;vrHeldItem.t.gameObject.SetActive(false);vrHeldItem=null;}vrRightMotion.Clear();vrThrowReady=false;}
  if(left)vrLeftMotion.Sample(VA("LeftGripPosition").ReadValue<Vector3>(),VA("LeftGripRotation").ReadValue<Quaternion>(),time);
  else{if(vrHeldFlare!=null){vrHeldFlare.held=vrHeldFlare.active=false;vrHeldFlare.root.SetActive(false);vrHeldFlare=null;}vrLeftMotion.Clear();vrFlareReady=false;}
  // Read the physical trigger amount. A controller's digital click can stay
  // latched through a partial release, and one-frame events can be missed.
  float rightTrigger=VA("RightTrigger").ReadValue<float>(),leftTrigger=VA("LeftTrigger").ReadValue<float>();
  if(right&&rightTrigger<=.25f)vrThrowReady=true;
  if(left&&leftTrigger<=.25f)vrFlareReady=true;
  if(right&&vrThrowReady&&rightTrigger>=.55f){
   vrThrowReady=false;
   if(cooldown<=0&&vrHeldItem==null){vrHeldItem=LaunchThrownItem(stones+reserveBottles<=0);vrHeldItem.held=true;vrHeldItem.velocity=Vector3.zero;}
  }
  if(left&&vrFlareReady&&leftTrigger>=.55f){
   vrFlareReady=false;
   if(cooldown<=0&&flares+reserveFlares>0&&vrHeldFlare==null){vrHeldFlare=PrepareFlare();if(vrHeldFlare!=null){vrHeldFlare.held=true;vrHeldFlare.velocity=Vector3.zero;}}
  }
  UpdateVRHeldItems();
  if(vrHeldItem!=null&&rightTrigger<=.25f){
   var item=vrHeldItem;ReleaseVRMotion(vrRightMotion,out item.velocity,out item.angularVelocity);item.held=false;item.physicalThrow=true;vrHeldItem=null;
   if(!item.pebble){if(stones>0)stones--;else{reserveBottles--;SaveEconomy();}}
   cooldown=item.pebble?2:.8f;Sfx(throwSound);
  }
  if(vrHeldFlare!=null&&leftTrigger<=.25f){
   var flare=vrHeldFlare;ReleaseVRMotion(vrLeftMotion,out flare.velocity,out flare.angularVelocity);flare.held=false;flare.physicalThrow=true;vrHeldFlare=null;
   if(flares>0)flares--;else{reserveFlares--;SaveEconomy();}cooldown=1;Sfx(throwSound);
  }
 }
 void ReleaseVRMotion(HandThrowMotion motion,out Vector3 velocity,out Vector3 spin){
  motion.Velocity(out var linear,out var angular);
  // A deliberate swing should cross the pool, while opening a stationary hand
  // still drops the item. Scale smoothly to avoid a speed threshold jump.
  float speed=linear.magnitude;
  float strength=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,2.5f,speed));
  velocity=vrOrigin.TransformDirection(linear)*Mathf.Lerp(1,2.2f,strength);
  velocity+=Vector3.up*(2*strength);
  velocity=Vector3.ClampMagnitude(velocity,20)*(1+rangeLevel*.2f);
  spin=vrOrigin.TransformDirection(angular);
 }
 void UpdateVRHeldItems(){
  if(vrHeldItem!=null)PlaceVRHeldItem(vrHeldItem.t,vrRightGrasp,vrHeldItem.pebble?Vector3.zero:BottleNeckAnchor,vrHeldItem.pebble?0:1);
  if(vrHeldFlare!=null){PlaceVRHeldItem(vrHeldFlare.root.transform,vrLeftGrasp,Vector3.zero,2);vrHeldFlare.light.intensity=.18f;vrHeldFlare.light.range=.8f;UpdateFlareBurn(vrHeldFlare);}
 }
 void PlaceVRHeldItem(Transform item,VRGrasp grasp,Vector3 anchor,int kind){
  grasp.ApplyPose(kind);
  item.rotation=grasp.Rotation;
  item.position=grasp.Center-item.TransformVector(anchor);
 }
}
}
