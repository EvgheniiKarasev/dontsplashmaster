using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using System.Collections.Generic;
namespace DontSplash {
/// <summary>Uses the supplied creature rig and attack take, with inexpensive procedural idle motion.</summary>
public sealed class MonsterPerformance : MonoBehaviour {
 Transform[] rig; Quaternion[] rigRotations; Vector3[] rigPositions;
 Transform face; readonly List<Transform> limbs=new List<Transform>();readonly List<Quaternion> limbRests=new List<Quaternion>();
 public Vector3 FacePosition=>face?face.position:transform.position+Vector3.up*2.65f;
 Transform visual; Vector3 visualHome; Quaternion visualRotation;
 readonly List<Transform> bones=new List<Transform>();
 readonly List<Quaternion> rests=new List<Quaternion>();
 Animator animator; PlayableGraph graph; AnimationClipPlayable attackPlayable,idlePlayable,runPlayable;AnimationMixerPlayable mixer;float idleDuration=1,runDuration=1,runBlend;bool hasLocomotion;
 AudioSource breathing,voice,footsteps; static AudioClip breathClip,alertClip,strikeClip,hitClip,waterClip;
 float gaitClock;
 float clock,stepClock,alertCooldown,attackClock=-1,attackDuration=1,blend;
 bool impactSent,stunnedBefore; public bool Attacking=>attackClock>=0;
 public void Initialize(Transform model,AnimationClip attack,AnimationClip idle=null,AnimationClip run=null) {
  visual=model;visualHome=model.localPosition;visualRotation=model.localRotation;rig=model.GetComponentsInChildren<Transform>();rigRotations=new Quaternion[rig.Length];rigPositions=new Vector3[rig.Length];for(int i=0;i<rig.Length;i++){rigRotations[i]=rig[i].localRotation;rigPositions[i]=rig[i].localPosition;}
  foreach(var b in model.GetComponentsInChildren<Transform>()){
   string n=b.name.ToLowerInvariant();
   if(n.Contains("head")&&!n.Contains("end"))face=b;
   if(n.Contains("thigh")||n.Contains("upperarm")||n.Contains("upper_arm")||n.Contains("calf")||n.Contains("forearm")){limbs.Add(b);limbRests.Add(b.localRotation);}
   if(n=="spine1"||n=="spine2"||n=="spine3"||n=="neck1"||n=="neck1_001"){bones.Add(b);rests.Add(b.localRotation);}
  }
  animator=model.GetComponent<Animator>();if(!animator)animator=model.gameObject.AddComponent<Animator>();
  animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
  if(attack){
   graph=PlayableGraph.Create("Creature attack");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
   attackPlayable=AnimationClipPlayable.Create(graph,attack);attackPlayable.SetApplyFootIK(false);
   var output=AnimationPlayableOutput.Create(graph,"Attack",animator);mixer=AnimationMixerPlayable.Create(graph,3);
   graph.Connect(attackPlayable,0,mixer,2);mixer.SetInputWeight(2,1);
   if(idle&&run){idlePlayable=AnimationClipPlayable.Create(graph,idle);runPlayable=AnimationClipPlayable.Create(graph,run);graph.Connect(idlePlayable,0,mixer,0);graph.Connect(runPlayable,0,mixer,1);idleDuration=idle.length;runDuration=run.length;hasLocomotion=true;}
   output.SetSourcePlayable(mixer);
   attackDuration=Mathf.Max(.65f,attack.length);graph.Play();
  }
  if(!breathClip){breathClip=Sound("Creature breathing",3.2f,48,.55f);alertClip=Sound("Listening growl",.85f,72,.8f);strikeClip=Sound("Attack roar",.7f,115,1);hitClip=Sound("Impact",.28f,35,1);waterClip=Sound("Water step",.32f,90,.75f);}
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
  float envelope=Mathf.Sin(progress*Mathf.PI);
  float turn=Mathf.Sin(progress*Mathf.PI*2)*24*envelope;
  for(int i=0;i<bones.Count;i++){
   if(bones[i].name.IndexOf("neck",System.StringComparison.OrdinalIgnoreCase)>=0)
    bones[i].localRotation*=Quaternion.Euler(-4*envelope,turn*.4f,Mathf.Sin(progress*Mathf.PI)*5);
  }
  if(face)face.localRotation*=Quaternion.Euler(-3*envelope,turn*.2f,-Mathf.Sin(progress*Mathf.PI*2)*4*envelope);
 }
 public void PlayImpact(){voice.pitch=1;voice.PlayOneShot(hitClip,.6f);}
 public void BeginAttack(){
  if(Attacking)return;attackClock=0;impactSent=false;
  if(graph.IsValid())attackPlayable.SetTime(0);
  voice.pitch=1;voice.PlayOneShot(strikeClip,.8f);
 }
 public void ResetPose(){attackClock=-1;impactSent=false;blend=0;alertCooldown=0;stepClock=0;stunnedBefore=false;voice.Stop();footsteps.Stop();breathing.Stop();Restore();}
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
   if(graph.IsValid()){mixer.SetInputWeight(0,0);mixer.SetInputWeight(1,0);mixer.SetInputWeight(2,1);attackPlayable.SetTime(attackClock/1.05f*attackDuration);graph.Evaluate(0);}
   else{Restore();visual.localRotation=visualRotation*Quaternion.Euler(Mathf.Sin(attackClock*3)*-24,0,0);}
   visual.localPosition=visualHome+Vector3.forward*(Mathf.Sin(Mathf.Clamp01(attackClock/1.05f)*Mathf.PI)*.55f);
   bool impact=!impactSent&&attackClock>=.48f;
   if(impact)impactSent=true;
   if(attackClock>=1.05f){attackClock=-1;Restore();}
   return impact;
  }
  if(!hasLocomotion)Restore();else{visual.localPosition=visualHome;visual.localRotation=visualRotation;}
  gaitClock+=dt*Mathf.Clamp(movement/5f,0,2.4f);
  if(hasLocomotion){
   runBlend=Mathf.MoveTowards(runBlend,Mathf.Clamp01(movement/1.5f),dt*5);
   mixer.SetInputWeight(0,1-runBlend);mixer.SetInputWeight(1,runBlend);mixer.SetInputWeight(2,0);
   idlePlayable.SetTime(clock%idleDuration);runPlayable.SetTime(gaitClock%runDuration);graph.Evaluate(0);
  }
  float rate=movement>.1f?(hunting?8:3):1.4f;
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


