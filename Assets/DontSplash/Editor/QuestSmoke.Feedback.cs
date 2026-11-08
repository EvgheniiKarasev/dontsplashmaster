using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace DontSplash {
public static partial class QuestSmoke {
 static Vector3 GripCenter(SplashGame game,string side){var grasp=Field<object>(game,"vr"+side+"Grasp");return (Vector3)grasp.GetType().GetProperty("Center").GetValue(grasp);}
 static void CheckFeedback(SplashGame game,XRHMD head,OculusTouchControllerProfile.OculusTouchController left,OculusTouchControllerProfile.OculusTouchController right){
  var report=new StringBuilder();
  CheckAudioFeedback(game,report);
  CheckGroundFeedback(game,head,report);
  CheckGraspFeedback(game,head,left,right,report);
  CheckChaseFeedback(game,report);
  CheckCatchFeedback(game,head,report);
  CheckFogFeedback(game,head,report);
  CheckAtmosphericFX(game,report);
  File.WriteAllText("Logs/Quest-feedback-checks.txt","PASS\n"+report);
 }
 static void CheckGroundFeedback(SplashGame game,XRHMD head,StringBuilder report){
  var body=Field<CharacterController>(game,"body");
  foreach(int fps in new[]{72,90,120}){
   Set(game,"spawn",new Vector3(0,.65f,-18));Call(game,"Respawn");Call(game,"Resume");
   var contact=Field<object>(game,"playerContact");float speed=-2,dt=1f/fps;int idleLandings=0,jumpLandings=0;
   for(int frame=0;frame<fps*8;frame++){
    float time=frame*dt;
    Push(head.centerEyePosition,new Vector3(Mathf.Sin(time*7)*.025f,1.65f+Mathf.Sin(time*4)*.018f,Mathf.Sin(time*9)*.02f));
    Push(head.centerEyeRotation,Quaternion.identity);Push(head.isTracked,1f);
    Set(game,"vertical",speed);Call(game,"TickVR");
    if(Item<bool>(contact,"grounded")&&speed<0)speed=-2;
    if(frame==fps*3)speed=7.2f;
    speed-=(speed>0?22:29)*dt;Vector3 before=body.transform.position;body.Move(Vector3.up*speed*dt);
    bool landed=(bool)Call(game,"TickPlayerFootsteps",before,speed,dt);
    if(landed){if(frame<fps*3||frame>fps*5)idleLandings++;else jumpLandings++;}
   }
   Check(idleLandings==0,"False landing from VR tracking at "+fps+" Hz: "+idleLandings);
   Check(jumpLandings==1,"Expected one real jump landing at "+fps+" Hz, got "+jumpLandings);
   report.AppendLine(fps+" Hz moving-head floor test: zero false landings, exactly one real jump landing.");
  }
  Call(game,"Respawn");Call(game,"Resume");Push(head.centerEyePosition,new Vector3(0,1.65f,0));Call(game,"UpdateVRPose");
 }
 static void CheckAudioFeedback(SplashGame game,StringBuilder report){
  var air=game.roomAmbienceSound;var airData=new float[air.samples*air.channels];Check(air.GetData(airData,0),"Room air PCM unavailable");
  double airRms=Math.Sqrt(airData.Average(v=>(double)v*v));
  Check(airRms>.07&&airRms<.16,"Room air too quiet or loud: "+airRms);
  Check(Math.Abs(airData[0]-airData[airData.Length-1])<.035,"Room air clicks at loop boundary");
  Check(Field<AudioSource>(game,"ambience").volume>=.25f&&Field<AudioSource>(game,"playerFeet").volume>=.5f,"Player mix is inaudible");
  report.AppendLine("Room air RMS="+airRms.ToString("F4")+", seam delta="+Math.Abs(airData[0]-airData[airData.Length-1]).ToString("F5")+"; footsteps normalized with audible independent 2D channel.");
  foreach(var entry in new[]{("playerStepSound",.41f),("waterStepSound",.39f),("throwSound",.26f),("landingSound",.33f),("waterImpactSound",.66f)}){
   var clip=(AudioClip)typeof(SplashGame).GetField(entry.Item1,Flags|System.Reflection.BindingFlags.Public).GetValue(game);
   Check(clip&&clip.length<=entry.Item2,"Long overlapping audio event: "+entry.Item1);
   var data=new float[clip.samples*clip.channels];Check(clip.GetData(data,0),"Cannot inspect audio PCM");
   float peak=data.Max(v=>Mathf.Abs(v));Check(peak<=.651f&&peak>.005f,"Invalid event audio level: "+entry.Item1);
   Check(Mathf.Abs(data[0])<.001f&&Mathf.Abs(data[data.Length-1])<.001f,"Audio event has no click-free fade");
   report.AppendLine(entry.Item1+": "+clip.length.ToString("F3")+" s, peak "+peak.ToString("F3"));
  }
  Call(game,"Resume");Call(game,"StopGameplaySounds");
  for(int i=0;i<100;i++){Call(game,"Sfx",Field<AudioClip>(game,"splashClip"));Call(game,"Sfx",Field<AudioClip>(game,"clickClip"));}
  var sources=UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
  Check(sources.Length<=8,"Unexpected audio emitters: "+sources.Length);
  Check(Field<AudioSource>(game,"impactAudio").clip==Field<AudioClip>(game,"splashClip"),"Impact channel is not bounded");
  Check(Field<AudioSource>(game,"uiAudio").clip==Field<AudioClip>(game,"clickClip"),"UI channel is not bounded");
  Call(game,"StopGameplaySounds");report.AppendLine("200 repeated sound requests: fixed channels; no dynamically stacked voices. Total sources: "+sources.Length);
 }
 static Camera FeedbackCamera(Camera eye){
  var camera=new GameObject("Feedback capture camera").AddComponent<Camera>();camera.CopyFrom(eye);camera.enabled=false;
  var data=camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.allowXRRendering=false;data.requiresDepthTexture=true;data.requiresColorTexture=false;
  camera.nearClipPlane=.01f;camera.fieldOfView=46;return camera;
 }
 static void CheckFogFeedback(SplashGame game,XRHMD head,StringBuilder report){
  Call(game,"Respawn");Call(game,"Resume");var body=Field<CharacterController>(game,"body");var eye=Field<Camera>(game,"eye");
  body.enabled=false;body.transform.position=new Vector3(0,.65f,15);body.enabled=true;
  foreach(int stage in new[]{0,2,7})foreach(int yaw in new[]{0,45,90}){
   Set(game,"stage",stage);Call(game,"ApplyAtmosphere");Push(head.centerEyeRotation,Quaternion.Euler(0,yaw,0));Call(game,"UpdateVRPose");Call(game,"LateVR");
   Field<Material>(game,"vrComfortMaterial").SetFloat("_Fade",0);Field<Material>(game,"vrComfortMaterial").SetFloat("_Vignette",0);
   float far=eye.farClipPlane;Check(far>=RenderSettings.fogEndDistance+15,"Fog does not cover far plane");
   string a="Logs/Quest-fog-check-near.png",b="Logs/Quest-fog-check-far.png";
   Capture(eye,a);eye.farClipPlane=far+40;Capture(eye,b);eye.farClipPlane=far;
   var ta=new Texture2D(2,2);var tb=new Texture2D(2,2);ta.LoadImage(File.ReadAllBytes(a));tb.LoadImage(File.ReadAllBytes(b));var pa=ta.GetPixels32();var pb=tb.GetPixels32();
   double error=0;int visible=0;for(int i=0;i<pa.Length;i++){int d=Math.Max(Math.Abs(pa[i].r-pb[i].r),Math.Max(Math.Abs(pa[i].g-pb[i].g),Math.Abs(pa[i].b-pb[i].b)));error+=d;if(d>16)visible++;}
   UnityEngine.Object.DestroyImmediate(ta);UnityEngine.Object.DestroyImmediate(tb);
   report.AppendLine("Fog stage "+stage+", yaw "+yaw+": extending far plane 40 m changes mean RGB by "+(error/pa.Length).ToString("F3")+"/255; visible pixels="+visible);
   Check(error/pa.Length<1.5&&visible<pa.Length*.01,"Visible geometry clipping beyond fog: stage "+stage+", yaw "+yaw+", error="+error/pa.Length);
  }
  Set(game,"stage",0);Call(game,"ApplyAtmosphere");
 }
 static void CheckGraspFeedback(SplashGame game,XRHMD head,OculusTouchControllerProfile.OculusTouchController left,OculusTouchControllerProfile.OculusTouchController right,StringBuilder report){
  Call(game,"Respawn");Call(game,"Resume");Push(head.centerEyeRotation,Quaternion.identity);Push(head.centerEyePosition,new Vector3(0,1.65f,0));Call(game,"UpdateVRPose");
  var camera=FeedbackCamera(Field<Camera>(game,"eye"));
  var canvas=Field<Canvas>(game,"uiCanvas");canvas.gameObject.SetActive(false);
  Field<Material>(game,"vrComfortMaterial").SetFloat("_Fade",0);Field<Material>(game,"vrComfortMaterial").SetFloat("_Vignette",0);
  try{
   foreach(string kind in new[]{"stone","bottle","flare"}){
    Call(game,"ResetThrows");Set(game,"cooldown",0f);Set(game,"stones",kind=="stone"?0:5);Set(game,"reserveBottles",0);Set(game,"flares",2);
    Trigger(left,0);Trigger(right,0);Call(game,"TickVRThrows",100f);
    var controller=kind=="flare"?left:right;string side=kind=="flare"?"Left":"Right";
    Push(controller.devicePosition,new Vector3(side=="Left"?-.15f:.15f,1.35f,.45f));Push(controller.deviceRotation,Quaternion.identity);Call(game,"UpdateVRPose");
    Trigger(controller,1);Call(game,"TickVRThrows",100.02f);
    var grip=Field<Transform>(game,"vr"+side+"Grip");var animator=Field<Animator>(game,"vr"+side+"Animator");
    Transform item=kind=="flare"?Item<GameObject>(Field<object>(game,"vrHeldFlare"),"root").transform:Item<Transform>(Field<object>(game,"vrHeldItem"),"t");
    foreach(var pose in new[]{Vector3.zero,new Vector3(55,20,0),new Vector3(-40,0,70),new Vector3(0,135,25),new Vector3(0,0,180),new Vector3(25,-60,-80)}){
     Push(controller.deviceRotation,Quaternion.Euler(pose));Call(game,"UpdateVRPose");animator.Update(.2f);Call(game,"UpdateVRHeldItems");
     Vector3 center=GripCenter(game,side),anchor=kind=="bottle"?Vector3.up*.153f:Vector3.zero;
     Check(Vector3.Distance(item.TransformPoint(anchor),center)<.002f,"Wrong "+kind+" grip at "+pose);
     if(kind=="bottle")Check(Vector3.Dot(item.position-center,item.up)<-.075f,"Bottle body is not hanging below neck grip");
     foreach(var fingertip in grip.GetComponentsInChildren<Transform>().Where(t=>t.name.EndsWith("Tip"))){
      Vector3 delta=fingertip.position-center;float radius=kind=="stone"?.03f:kind=="bottle"?.014f:.01575f;
      float separation=kind=="stone"?delta.magnitude:Vector3.ProjectOnPlane(delta,item.up).magnitude;
      Check(separation>=radius+.001f,"Finger penetrates "+kind+": "+fingertip.name+", separation "+separation);
     }
     var before=item.position;Call(game,"BeforeRenderVR");Check(Vector3.Distance(item.position,before)<.002f,"Late pose displaces "+kind+" from fingers");
    }
    Push(controller.deviceRotation,Quaternion.identity);Call(game,"UpdateVRPose");animator.Update(.2f);Call(game,"UpdateVRHeldItems");
    Vector3 focus=GripCenter(game,side);
    for(int view=0;view<3;view++){
     Vector3 direction=Quaternion.AngleAxis((view-1)*65,item.up)*item.forward;
     camera.transform.position=focus+direction*.38f+item.up*.035f;camera.transform.rotation=Quaternion.LookRotation(focus-camera.transform.position,item.up);
     Capture(camera,"Logs/Quest-grip-"+kind+"-"+view+".png");
    }
    report.AppendLine(kind+": grip remains inside fingers over 6 wrist orientations; 3 rendered inspection views.");
   }
  }finally{canvas.gameObject.SetActive(true);UnityEngine.Object.DestroyImmediate(camera.gameObject);Call(game,"ResetThrows");Trigger(left,0);Trigger(right,0);Set(game,"stones",5);}
 }
 static void CheckChaseFeedback(SplashGame game,StringBuilder report){
  var body=Field<CharacterController>(game,"body");var monster=Field<Transform>(game,"monster");
  foreach(int fps in new[]{72,90,120}){
   Call(game,"Resume");body.enabled=false;body.transform.position=new Vector3(0,.65f,20);body.enabled=true;
   monster.position=new Vector3(0,-.5f,32);monster.rotation=Quaternion.identity;Set(game,"creatureVelocity",Vector3.zero);Set(game,"wet",0f);Set(game,"pursuit",100f);Set(game,"heard",100f);Set(game,"stun",0f);
   float largestTurn=0,settledTravel=0;Vector3 previous=monster.position;Quaternion rotation=monster.rotation;
   for(int frame=0;frame<fps*5;frame++){
    Call(game,"TickCreature",1f/fps);
    largestTurn=Mathf.Max(largestTurn,Quaternion.Angle(rotation,monster.rotation));
    if(frame>fps*4)settledTravel+=Vector3.Distance(previous,monster.position);
    previous=monster.position;rotation=monster.rotation;
    Check(Field<object>(game,"mode").ToString()=="Play","Creature caught a dry player");
   }
   Check(largestTurn<=220f/fps+.02f,"Creature snapped its heading");
   Check(settledTravel<.025f,"Creature jitters at stopping distance: "+settledTravel);
   report.AppendLine(fps+" Hz approach: max turn "+largestTurn.ToString("F2")+" deg/frame; settled travel "+settledTravel.ToString("F4")+" m.");
  }
  foreach(var sample in new[]{new Vector2(.3f,4),new Vector2(.3f,-3),new Vector2(-.08f,9.5f)}){
   Call(game,"Resume");body.enabled=false;body.transform.position=new Vector3(0,sample.x,20);body.enabled=true;
   monster.position=new Vector3(0,-.5f,20+1.56f);Set(game,"wet",1f);Set(game,"vertical",sample.y);Set(game,"pursuit",1.4f);Set(game,"creatureVelocity",Vector3.zero);
   Call(game,"TickCreature",1f/90);Check(Field<object>(game,"mode").ToString()=="Play","Jump escape still allows a catch: "+sample);
  }
  body.enabled=false;body.transform.position=new Vector3(0,-.08f,20);body.enabled=true;Set(game,"vertical",-1f);monster.position=new Vector3(0,-.5f,21.56f);Call(game,"TickCreature",1f/90);
  Check(Field<object>(game,"mode").ToString()=="Caught","Creature cannot catch a player still in water");
  report.AppendLine("Escape: ascending and descending above water safe; water-jump launch safe; submerged descending player still caught.");
 }
 static void CheckCatchFeedback(SplashGame game,XRHMD head,StringBuilder report){
  var eye=Field<Camera>(game,"eye");var camera=FeedbackCamera(eye);
  try{
   foreach(int fps in new[]{72,90,120})foreach(float height in new[]{1.05f,1.65f,2.05f}){
    Call(game,"Respawn");Call(game,"Resume");Set(game,"deaths",0);Push(head.centerEyeRotation,Quaternion.identity);Push(head.centerEyePosition,new Vector3(0,height,0));Call(game,"UpdateVRPose");
    Call(game,"BeginCatch");Call(game,"SyncUI");Call(game,"LateVR");Field<Material>(game,"vrComfortMaterial").SetFloat("_Fade",0);var creature=Field<MonsterPerformance>(game,"creature");
    var arms=creature.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("joint_Shoulder")||t.name.StartsWith("joint_Elbow")||t.name.StartsWith("joint_Hand")).ToArray();
    var rotations=arms.Select(t=>t.localRotation).ToArray();float maxAngle=0,maxTime=0;string maxBone="";int capture=0;
    for(int frame=0;frame<fps*4&&!Field<bool>(game,"catchHit");frame++){
     float time=frame/(float)fps;
     Push(head.centerEyePosition,new Vector3(Mathf.Sin(time*2)*.06f,height+Mathf.Sin(time)*.025f,0));Call(game,"UpdateVRPose");
     Vector3 headPosition=eye.transform.position;Quaternion headRotation=eye.transform.rotation;
     Call(game,"StepCatch",1f/fps);
     for(int i=0;i<arms.Length;i++){float angle=Quaternion.Angle(rotations[i],arms[i].localRotation);if(angle>maxAngle){maxAngle=angle;maxTime=time;maxBone=arms[i].name;}rotations[i]=arms[i].localRotation;}
     Check(Vector3.Distance(headPosition,eye.transform.position)<.0001f&&Quaternion.Angle(headRotation,eye.transform.rotation)<.01f,"Catch moved HMD camera");
     if(fps==90&&height==1.65f&&time>=2+capture*.2f&&capture<4){
      Vector3 focus=creature.FacePosition-Vector3.up*.55f;
      camera.fieldOfView=55;camera.transform.position=focus+creature.transform.forward*2.8f+creature.transform.right*1.8f;
      camera.transform.LookAt(focus);Capture(camera,"Logs/Quest-strike-side-"+capture+".png");capture++;
     }
    }
    Check(Field<bool>(game,"catchHit"),"Catch never reached impact");
    float contact=Vector3.Distance(creature.StrikePosition,eye.transform.position);
    Check(contact>.3f&&contact<.65f,"Strike enters camera or misses: "+contact);
    Check(maxAngle<45,"Arm has a discontinuous rotation: "+maxAngle+" at "+fps+" Hz, height "+height+", "+maxBone+" at t="+maxTime+", selected arm="+typeof(MonsterPerformance).GetField("victimArm",Flags).GetValue(creature));
    Vector3 wrist=creature.StrikePosition;for(int i=0;i<10;i++)Call(game,"StepCatch",1f/fps);
    Check(Vector3.Distance(wrist,creature.StrikePosition)<.001f,"IK accumulates after impact");
    if(fps==90&&height==1.65f){Call(game,"SyncUI");Call(game,"LateVR");Field<Material>(game,"vrComfortMaterial").SetFloat("_Fade",0);Capture(eye,"Logs/Quest-strike-player.png");}
    report.AppendLine(fps+" Hz catch at "+height+" m: max arm step "+maxAngle.ToString("F2")+" deg; wrist stops "+contact.ToString("F3")+" m from head; stable after impact.");
   }
  }finally{UnityEngine.Object.DestroyImmediate(camera.gameObject);}
 }
 static void FeedbackDiagnostics(SplashGame game){
  var log=new StringBuilder();
  foreach(var field in typeof(SplashGame).GetFields(Flags|System.Reflection.BindingFlags.Public))if(field.FieldType==typeof(AudioClip)){
   var clip=field.GetValue(game) as AudioClip;if(clip)log.AppendLine(field.Name+": "+clip.name+" "+clip.length+" seconds");
  }
  var grip=Field<Transform>(game,"vrRightGrip");var animator=Field<Animator>(game,"vrRightAnimator");
  foreach(float flex in new[]{.5f,.72f,1f}){
   animator.SetFloat("Flex",flex);animator.SetFloat("Pinch",1);animator.SetLayerWeight(animator.GetLayerIndex("Point Layer"),0);animator.Update(.2f);
   log.AppendLine("FLEX "+flex);
   foreach(var t in grip.GetComponentsInChildren<Transform>())if(new[]{"XRHand_Wrist","XRHand_Palm","XRHand_IndexProximal","XRHand_MiddleProximal","XRHand_MiddleIntermediate","XRHand_MiddleDistal","XRHand_MiddleTip","XRHand_LittleProximal","XRHand_ThumbTip"}.Contains(t.name))log.AppendLine(t.name+" "+grip.InverseTransformPoint(t.position).ToString("F4"));
  }
  foreach(var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))log.AppendLine("SOURCE "+source.name+" clip="+(source.clip?source.clip.name:"none")+" loop="+source.loop+" play="+source.isPlaying);
  File.WriteAllText("Logs/Quest-feedback-diagnostics.txt",log.ToString());
  Call(game,"UpdateVRPose");animator.Update(.1f);
 }
}
}
