using UnityEngine;
using System.Collections.Generic;
namespace DontSplash {
public sealed partial class SplashGame {
 public GameObject[] fxAssets;public Material fxMaterial;
 class Burst {public GameObject go;public ParticleSystem[] systems;public int kind;public float remaining;}
 readonly List<Burst> bursts=new List<Burst>();
 readonly Dictionary<Material,Material> fxMaterials=new Dictionary<Material,Material>();
 void InitializeFX(){
  if(fxAssets==null||!fxMaterial)return;
  for(int kind=0;kind<fxAssets.Length;kind++){
   if(!fxAssets[kind])continue;
   for(int copy=0;copy<2;copy++){
    var go=Instantiate(fxAssets[kind],transform);go.name="Pooled Lana FX "+kind;go.transform.localScale=Vector3.one*(kind==0?.22f:kind==1?.35f:.25f);
    foreach(var script in go.GetComponentsInChildren<MonoBehaviour>())script.enabled=false;
    foreach(var light in go.GetComponentsInChildren<Light>())light.enabled=false;
    var systems=go.GetComponentsInChildren<ParticleSystem>();
    foreach(var ps in systems){
     ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.loop=false;main.playOnAwake=false;main.stopAction=ParticleSystemStopAction.None;main.maxParticles=Mathf.Min(main.maxParticles,16);main.useUnscaledTime=false;
     var collision=ps.collision;collision.enabled=false;var lights=ps.lights;lights.enabled=false;var trails=ps.trails;trails.enabled=false;
    }
    foreach(var renderer in go.GetComponentsInChildren<ParticleSystemRenderer>()){
     var source=renderer.sharedMaterial;if(!source)continue;
     if(!fxMaterials.TryGetValue(source,out var mat)){mat=new Material(fxMaterial);mat.mainTexture=source.mainTexture;fxMaterials[source]=mat;}
     renderer.sharedMaterial=mat;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    go.SetActive(false);bursts.Add(new Burst{go=go,systems=systems,kind=kind});
   }
  }
 }
 void EmitFX(int kind,Vector3 point){
  Burst selected=null;
  foreach(var b in bursts)if(b.kind==kind&&(selected==null||b.remaining<selected.remaining))selected=b;
  if(selected==null)return;
  selected.go.SetActive(true);selected.go.transform.position=point;selected.remaining=2.5f;
  foreach(var ps in selected.systems){ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);ps.Play(false);}
 }
 void TickFX(){
  foreach(var b in bursts){
   if(b.remaining<=0)continue;
   if(SystemSuspended||mode!=Mode.Play){foreach(var ps in b.systems)if(ps.isPlaying)ps.Pause(false);continue;}
   foreach(var ps in b.systems)if(ps.isPaused)ps.Play(false);
   b.remaining-=Time.deltaTime;if(b.remaining<=0)b.go.SetActive(false);
  }
 }
}
}
