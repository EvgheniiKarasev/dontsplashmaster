using UnityEngine;
namespace DontSplash {
public sealed partial class SplashGame {
 sealed class ThrownItem {public Transform t;public GameObject bottle,stone;public Vector3 velocity;public bool active,pebble;public float age;}
 readonly ThrownItem[] thrown=new ThrownItem[4];
 bool lookReady;float targetYaw,targetPitch,yawVelocity,pitchVelocity;
 void UpdateLook(float dt){
  if(!lookReady){targetYaw=yaw;targetPitch=pitch;yawVelocity=pitchVelocity=0;lookReady=true;}
  targetYaw+=look.x;targetPitch=Mathf.Clamp(targetPitch-look.y,-75,75);
  yaw=Mathf.SmoothDampAngle(yaw,targetYaw,ref yawVelocity,.055f,Mathf.Infinity,dt);
  pitch=Mathf.SmoothDamp(pitch,targetPitch,ref pitchVelocity,.055f,Mathf.Infinity,dt);
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
 void ResetThrows(){foreach(var item in thrown)if(item!=null){item.active=false;item.t.gameObject.SetActive(false);}}
 void LaunchThrownItem(bool pebble){
  ThrownItem item=null;foreach(var candidate in thrown)if(!candidate.active){item=candidate;break;}
  if(item==null)item=thrown[0];
  item.active=true;item.pebble=pebble;item.age=0;
  item.t.gameObject.SetActive(true);item.t.localScale=Vector3.one;item.bottle.SetActive(!pebble);item.stone.SetActive(pebble);
  item.t.position=eye.transform.position+eye.transform.forward*.65f+eye.transform.right*.22f-Vector3.up*.22f;
  item.t.rotation=eye.transform.rotation;
  item.velocity=eye.transform.forward*(12*(1+rangeLevel*.2f))+Vector3.up*4;
 }
 void TickThrows(float dt){
  foreach(var item in thrown){
   if(!item.active)continue;
   item.age+=dt;Vector3 old=item.t.position;
   item.velocity+=Vector3.down*18*dt;
   Vector3 next=old+item.velocity*dt,delta=next-old;
   bool hit=Physics.SphereCast(old,.07f,delta.normalized,out var contact,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
   if(hit){next=contact.point+contact.normal*.08f;item.velocity=Vector3.Reflect(item.velocity,contact.normal)*.35f;}
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
