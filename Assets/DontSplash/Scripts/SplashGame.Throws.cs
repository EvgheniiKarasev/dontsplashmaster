using UnityEngine;
namespace DontSplash {
public sealed partial class SplashGame {
 sealed class ThrownItem {public Transform t;public GameObject bottle,stone;public Vector3 velocity;public bool active,pebble;public float age,nextImpact;}
 readonly ThrownItem[] thrown=new ThrownItem[4];
 bool lookReady;float targetYaw,targetPitch,yawVelocity,pitchVelocity;
 // Modest 15% increase over the original input scale; retain short smoothing.
 const float MouseLookSensitivity=.09775f,TouchLookSensitivity=.138f,LookSmoothTime=.055f;
 void UpdateLook(float dt){
  if(!lookReady){targetYaw=yaw;targetPitch=pitch;yawVelocity=pitchVelocity=0;lookReady=true;}
  targetYaw+=look.x;targetPitch=Mathf.Clamp(targetPitch-look.y,-75,75);
  yaw=Mathf.SmoothDampAngle(yaw,targetYaw,ref yawVelocity,LookSmoothTime,Mathf.Infinity,dt);
  pitch=Mathf.SmoothDamp(pitch,targetPitch,ref pitchVelocity,LookSmoothTime,Mathf.Infinity,dt);
  body.transform.rotation=Quaternion.Euler(0,yaw,0);eye.transform.localRotation=Quaternion.Euler(pitch,0,0);
 }
 void InitializeThrows(){
  for(int i=0;i<thrown.Length;i++){
   var root=new GameObject("Pooled thrown item");root.transform.SetParent(transform);
   var model=bottleAsset?Instantiate(bottleAsset,root.transform):GameObject.CreatePrimitive(PrimitiveType.Capsule);
   model.transform.SetParent(root.transform,false);
   foreach(var c in model.GetComponentsInChildren<Collider>()){c.enabled=false;Destroy(c);}
   foreach(var r in model.GetComponentsInChildren<Rigidbody>())Destroy(r);
   foreach(var script in model.GetComponentsInChildren<MonoBehaviour>())script.enabled=false;
   var renderers=model.GetComponentsInChildren<Renderer>();
   if(renderers.Length>0){
    Bounds bounds=renderers[0].bounds;foreach(var r in renderers){bounds.Encapsulate(r.bounds);r.sharedMaterial=bottleMaterial;}
    model.transform.localScale*=.45f/Mathf.Max(bounds.size.y,.01f);
    bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
    model.transform.position-=bounds.center-root.transform.position;
   }
   var stone=GameObject.CreatePrimitive(PrimitiveType.Sphere);stone.name="Pebble";stone.transform.SetParent(root.transform,false);stone.transform.localScale=Vector3.one*.18f;
   var collider=stone.GetComponent<Collider>();if(collider){collider.enabled=false;Destroy(collider);}stone.GetComponent<Renderer>().sharedMaterial=dark;stone.SetActive(false);
   root.SetActive(false);thrown[i]=new ThrownItem{t=root.transform,bottle=model,stone=stone};
  }
 }
 void ResetThrows(){ResetFlares();foreach(var item in thrown)if(item!=null){item.active=false;item.t.gameObject.SetActive(false);}}
 void LaunchThrownItem(bool pebble){
  ThrownItem item=null;foreach(var candidate in thrown)if(!candidate.active){item=candidate;break;}
  if(item==null)item=thrown[0];
  item.active=true;item.pebble=pebble;item.age=0;item.nextImpact=0;
  item.t.gameObject.SetActive(true);item.t.localScale=Vector3.one;item.bottle.SetActive(!pebble);item.stone.SetActive(pebble);
  item.t.position=eye.transform.position+eye.transform.forward*.65f+eye.transform.right*.22f-Vector3.up*.22f;
  item.t.rotation=eye.transform.rotation;
  item.velocity=eye.transform.forward*(12*(1+rangeLevel*.2f))+Vector3.up*4;
 }
 sealed class FlyingFlare {public GameObject root;public Vector3 velocity;public float age;public bool active,landed,affected;}
 readonly FlyingFlare[] flyingFlares=new FlyingFlare[8];
 Material flareBody,flareTip;
 bool LaunchFlare(){
  int slot=-1;for(int i=0;i<flyingFlares.Length;i++)if(flyingFlares[i]==null||!flyingFlares[i].active){slot=i;break;}
  if(slot<0)return false;
  if(flyingFlares[slot]==null){
   if(!flareBody){flareBody=Mat(new Color(.65f,.08f,.035f));flareTip=Mat(new Color(1,.3f,.035f),true);}
   var root=new GameObject("Thrown flare");root.transform.SetParent(transform);
   for(int part=0;part<2;part++){
    var mesh=GameObject.CreatePrimitive(PrimitiveType.Cylinder);mesh.transform.SetParent(root.transform,false);
    mesh.transform.localScale=part==0?new Vector3(.11f,.23f,.11f):new Vector3(.13f,.06f,.13f);
    mesh.transform.localPosition=part==0?Vector3.zero:Vector3.up*.28f;
    mesh.GetComponent<Renderer>().sharedMaterial=part==0?flareBody:flareTip;
    var collider=mesh.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
   }
   var light=root.AddComponent<Light>();light.color=new Color(1,.22f,.035f);light.range=12;light.intensity=2;light.shadows=LightShadows.None;
   flyingFlares[slot]=new FlyingFlare{root=root};
  }
  var flare=flyingFlares[slot];flare.active=true;flare.landed=flare.affected=false;flare.age=0;
  flare.root.SetActive(true);flare.root.transform.position=eye.transform.position+eye.transform.forward*.8f+eye.transform.right*.22f-Vector3.up*.2f;
  flare.root.transform.rotation=eye.transform.rotation;
  flare.velocity=eye.transform.forward*(14*(1+rangeLevel*.2f))+Vector3.up*3;
  return true;
 }
 void ResetFlares(){foreach(var f in flyingFlares)if(f!=null){f.active=false;f.root.SetActive(false);}}
 void TickFlares(float dt){
  foreach(var f in flyingFlares){
   if(f==null||!f.active)continue;
   f.age+=dt;
   if(!f.landed){
    Vector3 old=f.root.transform.position;f.velocity+=Vector3.down*18*dt;
    Vector3 delta=f.velocity*dt,next=old+delta;
    bool hit=Physics.SphereCast(old,.1f,delta.normalized,out var contact,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
    if(hit){next=contact.point+contact.normal*.12f;f.landed=true;Sfx(landingSound);}
    // The creature's colliders are disabled, so test the swept flight separately.
    Vector3 center=monster.position+Vector3.up*1.5f;
    float along=delta.sqrMagnitude>.0001f?Mathf.Clamp01(Vector3.Dot(center-old,delta)/delta.sqrMagnitude):0;
    if(Vector3.Distance(old+delta*along,center)<1.5f){next=old+delta*along;f.landed=true;}
    if(next.y<=WaterY+.1f){next.y=WaterY+.1f;f.landed=true;Ripple(next);Sfx(splashClip);}
    f.root.transform.position=next;
    if(!f.landed)f.root.transform.Rotate(220*dt,70*dt,0,Space.Self);

   }
   bool inRange=Vector3.Distance(f.root.transform.position,monster.position+Vector3.up*1.5f)<=10f;
   if(!f.affected&&(f.landed||(f.age>.15f&&inRange))){
    f.affected=true;
    EmitFX(1,f.root.transform.position);
    if(inRange){stun=Mathf.Max(stun,5+flareLevel*.75f);Notice("Creature stunned");}
    f.active=false;f.root.SetActive(false);continue;
   }
   if(f.age>=7){f.active=false;f.root.SetActive(false);}
  }
 }
 void TickThrows(float dt){
  TickFlares(dt);
  foreach(var item in thrown){
   if(!item.active)continue;
   item.age+=dt;Vector3 old=item.t.position;
   item.velocity+=Vector3.down*18*dt;
   Vector3 next=old+item.velocity*dt,delta=next-old;
   bool hit=Physics.SphereCast(old,.07f,delta.normalized,out var contact,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
   if(hit){if(item.age>=item.nextImpact&&item.velocity.sqrMagnitude>4){Sfx(landingSound);item.nextImpact=item.age+.2f;}next=contact.point+contact.normal*.08f;item.velocity=Vector3.Reflect(item.velocity,contact.normal)*.35f;}
   item.t.position=next;item.t.Rotate(250*dt,80*dt,0,Space.Self);
   if(next.y<=WaterY){
    next.y=WaterY;Noise(next,item.pebble?4:9);Ripple(next);Sfx(splashClip);
    if(item.pebble){pebbleSkips=2;pebblePoint=next;pebbleDirection=Vector3.ProjectOnPlane(item.velocity,Vector3.up).normalized;pebbleTimer=.4f;}
    item.active=false;item.t.gameObject.SetActive(false);
   }else if(item.age>6){item.active=false;item.t.gameObject.SetActive(false);}
  }
 }
}
}
