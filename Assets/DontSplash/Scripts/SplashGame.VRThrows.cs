using UnityEngine;

namespace DontSplash {
public sealed partial class SplashGame {
 ThrownItem vrHeldItem;
 FlyingFlare vrHeldFlare;
 bool vrThrowReady,vrFlareReady;
 readonly HandThrowMotion vrRightMotion=new HandThrowMotion(),vrLeftMotion=new HandThrowMotion();

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
   for(int i=1;i<count;i++){int index=(last-i+16)%16;if(times[last]-times[index]>.11f)break;first=index;}
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
  if(mode!=Mode.Play||!VRHeld("HeadTracked")){CancelVRThrows();return;}
  bool right=VRHeld("RightTracked"),left=VRHeld("LeftTracked");
  if(right)vrRightMotion.Sample(VA("RightGripPosition").ReadValue<Vector3>(),VA("RightGripRotation").ReadValue<Quaternion>(),time);
  else{if(vrHeldItem!=null){vrHeldItem.held=vrHeldItem.active=false;vrHeldItem.t.gameObject.SetActive(false);vrHeldItem=null;}vrRightMotion.Clear();vrThrowReady=false;}
  if(left)vrLeftMotion.Sample(VA("LeftGripPosition").ReadValue<Vector3>(),VA("LeftGripRotation").ReadValue<Quaternion>(),time);
  else{if(vrHeldFlare!=null){vrHeldFlare.held=vrHeldFlare.active=false;vrHeldFlare.root.SetActive(false);vrHeldFlare=null;}vrLeftMotion.Clear();vrFlareReady=false;}
  if(right&&!VRHeld("Select"))vrThrowReady=true;
  if(left&&!VRHeld("Flare"))vrFlareReady=true;
  if(right&&vrThrowReady&&VRPressed("Select")&&cooldown<=0&&vrHeldItem==null){
   vrHeldItem=LaunchThrownItem(stones+reserveBottles<=0);vrHeldItem.held=true;vrHeldItem.velocity=Vector3.zero;vrThrowReady=false;
  }
  if(left&&vrFlareReady&&VRPressed("Flare")&&cooldown<=0&&flares+reserveFlares>0&&vrHeldFlare==null){
   vrHeldFlare=PrepareFlare();if(vrHeldFlare!=null){vrHeldFlare.held=true;vrHeldFlare.velocity=Vector3.zero;}vrFlareReady=false;
  }
  UpdateVRHeldItems();
  if(vrHeldItem!=null&&VA("Select").WasReleasedThisFrame()){
   var item=vrHeldItem;ReleaseVRMotion(vrRightMotion,out item.velocity,out item.angularVelocity);item.held=false;item.physicalThrow=true;vrHeldItem=null;
   if(!item.pebble){if(stones>0)stones--;else{reserveBottles--;SaveEconomy();}}
   cooldown=item.pebble?2:.8f;Sfx(throwSound);
  }
  if(vrHeldFlare!=null&&VA("Flare").WasReleasedThisFrame()){
   var flare=vrHeldFlare;ReleaseVRMotion(vrLeftMotion,out flare.velocity,out flare.angularVelocity);flare.held=false;flare.physicalThrow=true;vrHeldFlare=null;
   if(flares>0)flares--;else{reserveFlares--;SaveEconomy();}cooldown=1;Sfx(throwSound);
  }
 }
 void ReleaseVRMotion(HandThrowMotion motion,out Vector3 velocity,out Vector3 spin){
  motion.Velocity(out var linear,out var angular);
  velocity=vrOrigin.TransformDirection(linear)*(1+rangeLevel*.2f);
  spin=vrOrigin.TransformDirection(angular);
 }
 void UpdateVRHeldItems(){
  if(vrHeldItem!=null)PlaceVRHeldItem(vrHeldItem.t,vrRightGrip,vrRightPalm);
  if(vrHeldFlare!=null)PlaceVRHeldItem(vrHeldFlare.root.transform,vrLeftGrip,vrPalm);
 }
 void PlaceVRHeldItem(Transform item,Transform grip,Transform palm){
  item.SetPositionAndRotation((palm?palm.position:grip.position)+grip.forward*.035f,grip.rotation);
 }
}
}
