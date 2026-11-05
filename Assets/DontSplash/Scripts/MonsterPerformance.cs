using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using System.Collections.Generic;
namespace DontSplash {
/// <summary>Uses the supplied creature rig and attack take, with inexpensive procedural idle motion.</summary>
public sealed class MonsterPerformance : MonoBehaviour {
 Transform[] rig; Quaternion[] rigRotations; Vector3[] rigPositions;
 readonly List<Transform> jaws=new List<Transform>();readonly List<Quaternion> openJaws=new List<Quaternion>();bool jawsCaptured;
 Transform face; readonly List<Transform> limbs=new List<Transform>();readonly List<Quaternion> limbRests=new List<Quaternion>();
 public Vector3 FacePosition=>face?face.position:transform.position+Vector3.up*2.65f;
 Vector3 faceForwardLocal;
 Quaternion faceAimOffset;
 Transform[] strikeShoulders=new Transform[2],strikeElbows=new Transform[2],strikeHands=new Transform[2];
 int victimArm=-1;
 public Vector3 FaceForward=>face?face.TransformDirection(faceForwardLocal):transform.forward;
 public float StrikeProgress=>Mathf.Clamp01(attackClock/.68f);
 public Vector3 StrikePosition=>victimArm>=0?strikeHands[victimArm].position:FacePosition;
 Transform visual; Vector3 visualHome; Quaternion visualRotation;
 readonly List<Transform> bones=new List<Transform>();
 readonly List<Quaternion> rests=new List<Quaternion>();
 Animator animator; PlayableGraph graph; AnimationClipPlayable attackPlayable,idlePlayable,runPlayable,walkPlayable,revealPlayable;float walkDuration=1,revealDuration=1;bool hasWalk,hasReveal;AnimationMixerPlayable mixer;float idleDuration=1,runDuration=1,runBlend;bool hasLocomotion;
 AudioSource breathing,voice,footsteps; static AudioClip breathClip,alertClip,strikeClip,hitClip,waterClip;
 float gaitClock;
 float clock,stepClock,alertCooldown,attackClock=-1,attackDuration=1,blend;
 bool impactSent,stunnedBefore; public bool Attacking=>attackClock>=0;
 public void Initialize(Transform model,AnimationClip attack,AnimationClip idle=null,AnimationClip run=null,AudioClip waterSteps=null,AnimationClip walk=null,AnimationClip reveal=null) {
  // Infer the imported rig's forward axis from the feet, rather than assuming FBX orientation.
  Transform leftFoot=null,leftToe=null,rightFoot=null,rightToe=null;
  foreach(var bone in model.GetComponentsInChildren<Transform>()){
   if(bone.name=="joint_FootLT_01")leftFoot=bone;
   if(bone.name=="joint_Toe_Middle_CLT_01")leftToe=bone;
   if(bone.name=="joint_FootRT_01")rightFoot=bone;
   if(bone.name=="joint_Toe_Middle_CRT_01")rightToe=bone;
  }
  if(leftFoot&&leftToe&&rightFoot&&rightToe){
   Vector3 forward=Vector3.ProjectOnPlane((leftToe.position-leftFoot.position)+(rightToe.position-rightFoot.position),Vector3.up);
   if(forward.sqrMagnitude>.0001f)model.rotation=Quaternion.FromToRotation(forward.normalized,transform.forward)*model.rotation;
  }
  visual=model;visualHome=model.localPosition;visualRotation=model.localRotation;rig=model.GetComponentsInChildren<Transform>();rigRotations=new Quaternion[rig.Length];rigPositions=new Vector3[rig.Length];for(int i=0;i<rig.Length;i++){rigRotations[i]=rig[i].localRotation;rigPositions[i]=rig[i].localPosition;}
  foreach(var b in model.GetComponentsInChildren<Transform>()){
   string n=b.name.ToLowerInvariant();
   if(n.StartsWith("joint_jaw_")){jaws.Add(b);openJaws.Add(b.localRotation);}
   if(n=="joint_head_01"||(face==null&&n.Contains("head")&&!n.Contains("skeleton")&&!n.Contains("end")))face=b;
   if(n.Contains("thigh")||n.Contains("upperarm")||n.Contains("upper_arm")||n.Contains("calf")||n.Contains("forearm")){limbs.Add(b);limbRests.Add(b.localRotation);}
   if(n=="spine1"||n=="spine2"||n=="spine3"||n=="neck1"||n=="neck1_001"||n=="joint_necka_01"){bones.Add(b);rests.Add(b.localRotation);}
   for(int side=0;side<2;side++){
    string suffix=side==0?"lt_01":"rt_01";
    if(n=="joint_shoulder"+suffix)strikeShoulders[side]=b;
    if(n=="joint_elbow"+suffix)strikeElbows[side]=b;
    if(n=="joint_hand"+suffix)strikeHands[side]=b;
   }
  }
  if(face){faceForwardLocal=face.InverseTransformDirection(transform.forward);faceAimOffset=Quaternion.Inverse(transform.rotation)*face.rotation;}
  animator=model.GetComponent<Animator>();if(!animator)animator=model.gameObject.AddComponent<Animator>();
  animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
  if(attack){
   graph=PlayableGraph.Create("Creature attack");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
   attackPlayable=AnimationClipPlayable.Create(graph,attack);attackPlayable.SetApplyFootIK(false);
   var output=AnimationPlayableOutput.Create(graph,"Attack",animator);mixer=AnimationMixerPlayable.Create(graph,5);
   graph.Connect(attackPlayable,0,mixer,2);mixer.SetInputWeight(2,1);
   if(idle&&run){idlePlayable=AnimationClipPlayable.Create(graph,idle);runPlayable=AnimationClipPlayable.Create(graph,run);graph.Connect(idlePlayable,0,mixer,0);graph.Connect(runPlayable,0,mixer,1);idleDuration=idle.length;runDuration=run.length;hasLocomotion=true;}
   if(walk){walkPlayable=AnimationClipPlayable.Create(graph,walk);graph.Connect(walkPlayable,0,mixer,3);walkDuration=Mathf.Max(.01f,walk.length);hasWalk=true;}
   if(reveal){revealPlayable=AnimationClipPlayable.Create(graph,reveal);graph.Connect(revealPlayable,0,mixer,4);revealDuration=Mathf.Max(.01f,reveal.length);hasReveal=true;}
   output.SetSourcePlayable(mixer);
   attackDuration=Mathf.Max(.65f,attack.length);graph.Play();
  }
  if(!breathClip){breathClip=Sound("Creature breathing",3.2f,48,.55f);alertClip=Sound("Listening growl",.85f,72,.8f);strikeClip=Sound("Attack roar",.7f,115,1);hitClip=Sound("Impact",.28f,35,1);waterClip=Sound("Water step",.32f,90,.75f);}
  if(waterSteps)waterClip=waterSteps;
  breathing=Source(.55f);breathing.loop=true;breathing.clip=breathClip;
  voice=Source(.8f);footsteps=Source(.35f);
 }
 AudioSource Source(float volume){var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=1;s.rolloffMode=AudioRolloffMode.Linear;s.minDistance=2;s.maxDistance=25;s.dopplerLevel=0;s.volume=volume;return s;}
 static AudioClip Sound(string name,float length,float hz,float rough){
  int count=(int)(22050*length);var data=new float[count];uint seed=12345;float filtered=0;
  for(int i=0;i<count;i++){
   seed=1664525*seed+1013904223;float noise=(seed>>8)/16777216f*2-1;filtered=Mathf.Lerp(filtered,noise,.09f);
   float t=i/22050f,phase=t/length;
   float envelope=Mathf.Pow(Mathf.Sin(Mathf.PI*phase),.7f);
   float growl=Mathf.Sin(t*hz*6.283f+Mathf.Sin(t*19)*2)*.2f+Mathf.Sin(t*hz*3.12f)*.15f;
   data[i]=Mathf.Clamp((growl+filtered*rough*1.6f)*envelope,-.7f,.7f);
  }
  var clip=AudioClip.Create(name,count,1,22050,false);clip.SetData(data,0);return clip;
 }
 public void Hear(){
  if(alertCooldown>0||Attacking)return;alertCooldown=2.8f;
  voice.pitch=.85f;voice.PlayOneShot(alertClip,.7f);blend=1;
 }
 // Applied after the idle pose, so head movement never accumulates between frames.
 public void InspectVictim(float progress){
  if(hasReveal){
   float weight=Mathf.SmoothStep(0,1,Mathf.Clamp01(progress/.45f));
   for(int i=0;i<5;i++)mixer.SetInputWeight(i,0);
   mixer.SetInputWeight(0,1-weight);mixer.SetInputWeight(4,weight);
   revealPlayable.SetTime(Mathf.Min(progress*1.3f,revealDuration-.001f));graph.Evaluate(0);
   for(int i=0;i<jaws.Count;i++)openJaws[i]=jaws[i].localRotation;jawsCaptured=true;
  }
  float envelope=Mathf.Sin(progress*Mathf.PI);
  float turn=Mathf.Sin(progress*Mathf.PI*2)*24*envelope;
  for(int i=0;i<bones.Count;i++){
   if(bones[i].name.IndexOf("neck",System.StringComparison.OrdinalIgnoreCase)>=0)
    bones[i].localRotation*=Quaternion.Euler(-4*envelope,turn*.4f,Mathf.Sin(progress*Mathf.PI)*5);
  }
  if(face)face.rotation=Quaternion.AngleAxis(turn*.5f,transform.up)*face.rotation;
 }
 public void PlayImpact(){voice.pitch=1;voice.PlayOneShot(hitClip,.6f);}
 // Applied after sampling the clip. The animation supplies the wind-up and
 // elbow bend; a two-bone correction steers its finishing strike to the HMD.
 public void AimAtVictim(Vector3 target,bool strike){
  Vector3 look=target-FacePosition;
  if(face&&look.sqrMagnitude>.0001f)face.rotation=Quaternion.LookRotation(look,Vector3.up)*faceAimOffset;
  if(!strike||StrikeProgress<.25f)return;
  if(victimArm<0){
   if(!strikeHands[0]||!strikeHands[1])return;
   victimArm=Vector3.Distance(strikeHands[0].position,target)<Vector3.Distance(strikeHands[1].position,target)?0:1;
  }
  var shoulder=strikeShoulders[victimArm];var elbow=strikeElbows[victimArm];var hand=strikeHands[victimArm];
  if(!shoulder||!elbow||!hand)return;
  Vector3 contact=target+(FacePosition-target).normalized*.18f;
  float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,1,StrikeProgress));
  Vector3 goal=Vector3.Lerp(hand.position,contact,weight),start=shoulder.position;
  float upper=Vector3.Distance(start,elbow.position),lower=Vector3.Distance(elbow.position,hand.position);
  Vector3 direction=(goal-start).normalized;
  float distance=Mathf.Clamp(Vector3.Distance(start,goal),Mathf.Abs(upper-lower)+.001f,upper+lower-.001f);
  Vector3 bend=Vector3.ProjectOnPlane(elbow.position-start,direction).normalized;
  if(bend.sqrMagnitude<.001f)bend=Vector3.ProjectOnPlane(transform.right,direction).normalized;
  float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
  Vector3 elbowGoal=start+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
  shoulder.rotation=Quaternion.FromToRotation(elbow.position-start,elbowGoal-start)*shoulder.rotation;
  elbow.rotation=Quaternion.FromToRotation(hand.position-elbow.position,start+direction*distance-elbow.position)*elbow.rotation;
 }
 public void BeginAttack(){
  if(Attacking)return;attackClock=0;impactSent=false;
  if(graph.IsValid())attackPlayable.SetTime(0);
  voice.pitch=1;voice.PlayOneShot(strikeClip,.8f);
 }
 public void ResetPose(){victimArm=-1;jawsCaptured=false;runBlend=0;gaitClock=0;attackClock=-1;impactSent=false;blend=0;alertCooldown=0;stepClock=0;stunnedBefore=false;voice.Stop();footsteps.Stop();breathing.Stop();Restore();}
 void Restore(){for(int i=0;i<rig.Length;i++){rig[i].localRotation=rigRotations[i];rig[i].localPosition=rigPositions[i];}visual.localPosition=visualHome;visual.localRotation=visualRotation;}
 // Returns one impact pulse; the game checks water, range and stun again at that moment.
 public bool Tick(float dt,bool active,bool hunting,bool stunned,float distance,float movement=1){
  if(!active){if(breathing.isPlaying)breathing.Pause();return false;}
  clock+=dt;alertCooldown-=dt;blend=Mathf.MoveTowards(blend,0,dt);
  if(distance<25&&!stunned){if(!breathing.isPlaying)breathing.Play();breathing.volume=Mathf.Lerp(.18f,.48f,1-distance/25);breathing.pitch=hunting?1.12f:.8f;}
  else if(breathing.isPlaying)breathing.Stop();
  if(stunned){if(!stunnedBefore){voice.Stop();voice.pitch=.6f;voice.PlayOneShot(alertClip,.5f);}attackClock=-1;Restore();visual.localRotation=visualRotation*Quaternion.Euler(12,0,Mathf.Sin(clock*18)*3);stunnedBefore=true;return false;}
  stunnedBefore=false;
  if(Attacking){
   attackClock+=dt;
   if(graph.IsValid()){mixer.SetInputWeight(3,0);mixer.SetInputWeight(4,0);mixer.SetInputWeight(0,0);mixer.SetInputWeight(1,0);mixer.SetInputWeight(2,1);attackPlayable.SetTime(attackClock/1.05f*attackDuration);graph.Evaluate(0);}
   else{Restore();visual.localRotation=visualRotation*Quaternion.Euler(Mathf.Sin(attackClock*3)*-24,0,0);}
   if(jawsCaptured)for(int i=0;i<jaws.Count;i++)jaws[i].localRotation=openJaws[i];
   visual.localRotation=visualRotation;
   visual.localPosition=visualHome+Vector3.forward*(Mathf.Sin(Mathf.Clamp01(attackClock/1.05f)*Mathf.PI)*.55f);
   bool impact=!impactSent&&attackClock>=.68f;
   if(impact)impactSent=true;
   if(attackClock>=1.05f){attackClock=-1;Restore();}
   return impact;
  }
  if(!hasLocomotion)Restore();else{visual.localPosition=visualHome;visual.localRotation=visualRotation;}
  gaitClock+=dt*Mathf.Clamp(movement/(hunting?7f:4f),0,3f);
  if(hasLocomotion){
   runBlend=Mathf.MoveTowards(runBlend,Mathf.Clamp01(movement/1.5f),dt*5);
   mixer.SetInputWeight(0,1-runBlend);mixer.SetInputWeight(1,hunting||!hasWalk?runBlend:0);mixer.SetInputWeight(2,0);mixer.SetInputWeight(3,!hunting&&hasWalk?runBlend:0);mixer.SetInputWeight(4,0);
   if(hasWalk)walkPlayable.SetTime(gaitClock%walkDuration);
   idlePlayable.SetTime(clock%idleDuration);runPlayable.SetTime(gaitClock%runDuration);graph.Evaluate(0);
  }
  float rate=movement>.1f?(hunting?18:6):1.4f;
  visual.localPosition=visualHome+Vector3.up*(Mathf.Sin(clock*rate)*.055f);
  visual.localRotation=visualRotation*Quaternion.Euler(hunting?8:0,0,Mathf.Sin(clock*rate)*2);
  for(int i=0;i<bones.Count;i++){bool neck=bones[i].name.IndexOf("neck",System.StringComparison.OrdinalIgnoreCase)>=0;bones[i].localRotation=(hasLocomotion?bones[i].localRotation:rests[i])*Quaternion.Euler(Mathf.Sin(clock*2+i)*1.8f+(neck?blend*-12:0),neck?Mathf.Sin(clock*1.3f)*7+blend*16:0,Mathf.Sin(clock*rate+i)*2);}
  if(!hasLocomotion)for(int i=0;i<limbs.Count;i++){
   string n=limbs[i].name.ToLowerInvariant();float side=n.Contains("left")||n.EndsWith("_l")||n.StartsWith("l_")?1:-1;
   float stride=movement>.1f?Mathf.Sin(clock*rate+side*1.57f)*(hunting?25:12):Mathf.Sin(clock*1.1f+i)*2;
   limbs[i].localRotation=limbRests[i]*Quaternion.Euler(stride,0,side*(hunting?8:3));
  }
  stepClock-=dt;if(stepClock<=0&&distance<23&&movement>.1f){footsteps.pitch=hunting?1.2f:.8f;footsteps.PlayOneShot(waterClip);stepClock=Mathf.Clamp(2.5f/Mathf.Max(movement,.1f),.22f,1.2f);}
  return false;
 }
 void OnDestroy(){if(graph.IsValid())graph.Destroy();}
}
}


