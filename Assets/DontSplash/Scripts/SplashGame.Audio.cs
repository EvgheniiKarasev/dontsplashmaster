using UnityEngine;
using System.Collections.Generic;
namespace DontSplash {
public sealed partial class SplashGame {
 readonly PlayerGroundContact playerContact=new PlayerGroundContact();
 sealed class PlayerGroundContact {
  public bool grounded;
  bool armed;float airTime,apex;
  public void Reset(){grounded=armed=false;airTime=apex=0;}
  public bool Step(bool supported,float previousHeight,float height,float speed,float dt,bool water){
   bool landed=false;
   if(water){airTime=0;apex=height;}
   else if(!supported){airTime+=dt;apex=Mathf.Max(apex,Mathf.Max(previousHeight,height));}
   else{
    // Require a real flight and descent. Skin-width corrections, one-frame
    // contact flicker and initial spawn settling are not landings.
    landed=armed&&!grounded&&airTime>=.08f&&apex-height>=.08f&&speed<-2.5f;
    armed=true;airTime=0;apex=height;
   }
   grounded=supported;return landed;
  }
 }
 AudioSource impactAudio,uiAudio;
 readonly Dictionary<AudioClip,float> soundNextAllowed=new Dictionary<AudioClip,float>();
 readonly List<AudioClip> shortAudio=new List<AudioClip>();
 void PrepareGameplayAudio(){
  playerStepSound=ShortSound(playerStepSound,.4f,true);
  waterStepSound=ShortSound(waterStepSound,.38f,true);
  playerJumpSound=ShortSound(playerJumpSound,.3f);
  throwSound=ShortSound(throwSound,.25f);
  landingSound=ShortSound(landingSound,.32f);
  waterImpactSound=ShortSound(waterImpactSound,.65f);
  roomAmbienceSound=SmoothAmbience(roomAmbienceSound);
  impactAudio=gameObject.AddComponent<AudioSource>();impactAudio.playOnAwake=false;impactAudio.volume=.24f;
  uiAudio=gameObject.AddComponent<AudioSource>();uiAudio.playOnAwake=false;uiAudio.volume=.18f;
 }
 AudioClip ShortSound(AudioClip source,float seconds,bool normalize=false){
  if(!source)return null;
  var original=new float[source.samples*source.channels];if(!source.GetData(original,0))return source;
  float peak=0;foreach(float value in original)peak=Mathf.Max(peak,Mathf.Abs(value));
  int start=0;float threshold=Mathf.Max(.004f,peak*.08f);
  while(start<source.samples-1){bool audible=false;for(int c=0;c<source.channels;c++)audible|=Mathf.Abs(original[start*source.channels+c])>=threshold;if(audible)break;start++;}
  start=Mathf.Max(0,start-(int)(source.frequency*.008f));
  int frames=Mathf.Min(source.samples-start,Mathf.CeilToInt(seconds*source.frequency));
  var data=new float[frames*source.channels];float gain=peak>.65f||normalize?Mathf.Min(4,.65f/Mathf.Max(peak,.001f)):1;
  for(int f=0;f<frames;f++){
   float fade=Mathf.Min(Mathf.Clamp01(f/(source.frequency*.008f)),Mathf.Clamp01((frames-1-f)/(source.frequency*.065f)));
   for(int c=0;c<source.channels;c++)data[f*source.channels+c]=original[(start+f)*source.channels+c]*gain*fade;
  }
  var clip=AudioClip.Create(source.name+" / single event",frames,source.channels,source.frequency,false);clip.SetData(data,0);shortAudio.Add(clip);return clip;
 }
 AudioClip SmoothAmbience(AudioClip source){
  if(!source)return null;
  var original=new float[source.samples*source.channels];if(!source.GetData(original,0))return source;
  var filtered=new float[source.samples];float previous=0,high=0,low=0;
  float highDecay=Mathf.Exp(-2*Mathf.PI*95/source.frequency),lowBlend=1-Mathf.Exp(-2*Mathf.PI*1000/source.frequency);
  // Remove bass knocks and sharp clicks, retaining soft room air. Warm the
  // filters for one loop so the final pass starts in their steady state.
  for(int pass=0;pass<2;pass++)for(int i=0;i<filtered.Length;i++){
   float value=0;for(int c=0;c<source.channels;c++)value+=original[i*source.channels+c]/source.channels;
   high=highDecay*(high+value-previous);previous=value;low=Mathf.Lerp(low,high,lowBlend);filtered[i]=low;
  }
  int overlap=Mathf.Min((int)(source.frequency*.6f),source.samples/4),length=source.samples-overlap;var data=new float[length];
  double square=0;float peak=0;
  for(int i=0;i<length;i++){
   data[i]=i<overlap?Mathf.Lerp(filtered[length+i],filtered[i],i/(float)overlap):filtered[i];square+=data[i]*data[i];peak=Mathf.Max(peak,Mathf.Abs(data[i]));
  }
  float rms=(float)System.Math.Sqrt(square/length),gain=Mathf.Min(.14f/Mathf.Max(.001f,rms),.55f/Mathf.Max(.001f,peak));
  for(int i=0;i<length;i++)data[i]*=gain;
  var clip=AudioClip.Create("Room air / seamless",length,1,source.frequency,false);clip.SetData(data,0);shortAudio.Add(clip);return clip;
 }
 // One voice per role, never an unbounded stack of PlayOneShot voices.
 void Sfx(AudioClip clip){
  if(!clip||muted||SystemSuspended)return;
  float now=Time.unscaledTime;if(soundNextAllowed.TryGetValue(clip,out float next)&&now<next)return;
  soundNextAllowed[clip]=now+(clip==splashClip?.35f:.12f);
  var channel=clip==clickClip?uiAudio:clip==splashClip||clip==landingSound||clip==glassBreak?impactAudio:audioSource;
  channel.clip=clip;channel.pitch=1;channel.Play();
 }
 void StopGameplaySounds(){
  audioSource.Stop();impactAudio.Stop();playerFeet.Stop();soundNextAllowed.Clear();
 }
 void DisposeGameplayAudio(){foreach(var clip in shortAudio)if(clip)Destroy(clip);}
}
}
