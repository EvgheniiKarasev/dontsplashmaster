using UnityEngine;
namespace DontSplash {
public sealed partial class SplashGame {
 sealed class ThrownItem {public Transform t;public GameObject bottle,vrBottle,stone;public Vector3 velocity,angularVelocity;public bool active,pebble,held,physicalThrow;public float age,nextImpact;}
 readonly ThrownItem[] thrown=new ThrownItem[4];
 Mesh vrBottleMesh;
 Mesh CreateVRBottleMesh(){
  // 24 cm tall after the VR scale, 6.6 cm body, 2.7 cm neck.
  // The source prop's oversized glass shell is unsuitable for a palm grip.
  float[] heights={-.225f,-.215f,-.19f,.035f,.065f,.10f,.195f,.20f,.218f,.225f};
  float[] radii={.049f,.06f,.062f,.062f,.052f,.025f,.025f,.028f,.028f,.024f};
  const int sides=24;var vertices=new Vector3[heights.Length*sides+2];var triangles=new System.Collections.Generic.List<int>();
  for(int ring=0;ring<heights.Length;ring++)for(int side=0;side<sides;side++){
   float angle=side*2*Mathf.PI/sides;int current=ring*sides+side;vertices[current]=new Vector3(Mathf.Cos(angle)*radii[ring],heights[ring],Mathf.Sin(angle)*radii[ring]);
   if(ring==0)continue;int next=ring*sides+(side+1)%sides;
   triangles.Add(current-sides);triangles.Add(current);triangles.Add(next);triangles.Add(current-sides);triangles.Add(next);triangles.Add(next-sides);
  }
  int bottom=vertices.Length-2,top=vertices.Length-1;vertices[bottom]=Vector3.up*heights[0];vertices[top]=Vector3.up*heights[heights.Length-1];
  for(int side=0;side<sides;side++){int next=(side+1)%sides;triangles.Add(bottom);triangles.Add(side);triangles.Add(next);int ring=(heights.Length-1)*sides;triangles.Add(top);triangles.Add(ring+next);triangles.Add(ring+side);}
  var mesh=new Mesh{name="Quest bottle with narrow neck"};mesh.vertices=vertices;mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }
 bool lookReady;float targetYaw,targetPitch,yawVelocity,pitchVelocity;
 // Modest 15% increase over the original input scale; retain short smoothing.
 const float MouseLookSensitivity=.1124125f,TouchLookSensitivity=.1587f,LookSmoothTime=.055f;
 void UpdateLook(float dt){
  if(vr)return;
  if(!lookReady){targetYaw=yaw;targetPitch=pitch;yawVelocity=pitchVelocity=0;lookReady=true;}
  targetYaw+=look.x;targetPitch=Mathf.Clamp(targetPitch-look.y,-75,75);
  yaw=Mathf.SmoothDampAngle(yaw,targetYaw,ref yawVelocity,LookSmoothTime,Mathf.Infinity,dt);
  pitch=Mathf.SmoothDamp(pitch,targetPitch,ref pitchVelocity,LookSmoothTime,Mathf.Infinity,dt);
  body.transform.rotation=Quaternion.Euler(0,yaw,0);eye.transform.localRotation=Quaternion.Euler(pitch,0,0);
 }
 void InitializeThrows(){
  vrBottleMesh=CreateVRBottleMesh();
  for(int i=0;i<thrown.Length;i++){
   var root=new GameObject("Pooled thrown item");root.transform.SetParent(transform);
   var model=bottleAsset?Instantiate(bottleAsset,root.transform):GameObject.CreatePrimitive(PrimitiveType.Capsule);
   model.transform.SetParent(root.transform,false);
   foreach(var c in model.GetComponentsInChildren<Collider>()){c.enabled=false;Destroy(c);}
   foreach(var r in model.GetComponentsInChildren<Rigidbody>())Destroy(r);
   foreach(var script in model.GetComponentsInChildren<MonoBehaviour>())script.enabled=false;
   var renderers=model.GetComponentsInChildren<Renderer>();
   if(renderers.Length>0){
    Bounds bounds=renderers[0].bounds;foreach(var r in renderers){bounds.Encapsulate(r.bounds);var materials=r.sharedMaterials;for(int slot=0;slot<materials.Length;slot++)materials[slot]=bottleMaterial;r.sharedMaterials=materials;}
    model.transform.localScale*=.45f/Mathf.Max(bounds.size.y,.01f);
    bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
    model.transform.position-=bounds.center-root.transform.position;
   }
   var stone=GameObject.CreatePrimitive(PrimitiveType.Sphere);stone.name="Pebble";stone.transform.SetParent(root.transform,false);stone.transform.localScale=Vector3.one*.18f;
   var collider=stone.GetComponent<Collider>();if(collider){collider.enabled=false;Destroy(collider);}stone.GetComponent<Renderer>().sharedMaterial=dark;stone.SetActive(false);
   var questBottle=new GameObject("Quest bottle");questBottle.transform.SetParent(root.transform,false);questBottle.AddComponent<MeshFilter>().sharedMesh=vrBottleMesh;questBottle.AddComponent<MeshRenderer>().sharedMaterial=bottleMaterial;questBottle.SetActive(false);
   root.SetActive(false);thrown[i]=new ThrownItem{t=root.transform,bottle=model,vrBottle=questBottle,stone=stone};
  }
 }
 void ResetThrows(){CancelVRThrows();ResetFlares();ResetBottleShards();foreach(var item in thrown)if(item!=null){item.held=item.active=false;item.t.gameObject.SetActive(false);}}
 ThrownItem LaunchThrownItem(bool pebble){
  ThrownItem item=null;foreach(var candidate in thrown)if(!candidate.active){item=candidate;break;}
  if(item==null)item=thrown[0];
  item.active=true;item.held=item.physicalThrow=false;item.pebble=pebble;item.age=0;item.nextImpact=0;
  item.t.gameObject.SetActive(true);item.t.localScale=Vector3.one*(vr?(pebble?1f/3f:.24f/.45f):1);item.bottle.SetActive(!pebble&&!vr);item.vrBottle.SetActive(!pebble&&vr);item.stone.SetActive(pebble);
  item.t.position=vr?ThrowOrigin.position+ThrowOrigin.forward*.2f:eye.transform.position+eye.transform.forward*.65f+eye.transform.right*.22f-Vector3.up*.22f;
  item.t.rotation=ThrowOrigin.rotation;
  item.velocity=ThrowOrigin.forward*(12*(1+rangeLevel*.2f))+Vector3.up*4;
  return item;
 }
 sealed class FlyingFlare {public GameObject root;public Light light;public MeshRenderer ember,smoke;public Vector3 velocity,angularVelocity;public float age;public bool active,landed,affected,held,physicalThrow;}
 readonly FlyingFlare[] flyingFlares=new FlyingFlare[8];
 Material flareBody,flareTip;
 bool LaunchFlare()=>PrepareFlare()!=null;
 FlyingFlare PrepareFlare(){
  int slot=-1;for(int i=0;i<flyingFlares.Length;i++)if(flyingFlares[i]==null||!flyingFlares[i].active){slot=i;break;}
  if(slot<0)return null;
  if(flyingFlares[slot]==null){
   if(!flareBody){flareBody=Mat(new Color(.3f,.075f,.035f));flareTip=Mat(new Color(.5f,.18f,.045f),true);}
   var root=new GameObject("Thrown flare");root.transform.SetParent(transform);
   for(int part=0;part<2;part++){
    var mesh=GameObject.CreatePrimitive(PrimitiveType.Cylinder);mesh.transform.SetParent(root.transform,false);
    mesh.transform.localScale=part==0?new Vector3(.07f,.23f,.07f):new Vector3(.085f,.06f,.085f);
    mesh.transform.localPosition=part==0?Vector3.zero:Vector3.up*.28f;
    mesh.GetComponent<Renderer>().sharedMaterial=part==0?flareBody:flareTip;
    var collider=mesh.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
   }
   var emitter=new GameObject("Flare light");emitter.transform.SetParent(root.transform,false);emitter.transform.localPosition=Vector3.up*.28f;
   var light=emitter.AddComponent<Light>();light.color=new Color(1,.43f,.16f);light.range=3;light.intensity=.7f;light.shadows=LightShadows.None;
   flyingFlares[slot]=new FlyingFlare{root=root,light=light,ember=EffectSprite(root.transform,"Soft ember",softFX),smoke=EffectSprite(root.transform,"Tip smoke",smokeFX)};
  }
  var flare=flyingFlares[slot];flare.active=true;flare.held=flare.physicalThrow=flare.landed=flare.affected=false;flare.age=0;
  flare.root.SetActive(true);flare.root.transform.position=vr?ThrowOrigin.position+ThrowOrigin.forward*.2f:eye.transform.position+eye.transform.forward*.8f+eye.transform.right*.22f-Vector3.up*.2f;
  flare.root.transform.rotation=ThrowOrigin.rotation;
  flare.root.transform.localScale=Vector3.one*(vr?.45f:1);
  flare.velocity=ThrowOrigin.forward*(14*(1+rangeLevel*.2f))+Vector3.up*3;
  return flare;
 }
 void ResetFlares(){foreach(var f in flyingFlares)if(f!=null){f.active=false;f.root.SetActive(false);}}
 void TickFlares(float dt){
  foreach(var f in flyingFlares){
   if(f==null||!f.active||f.held)continue;
   f.light.intensity=.7f;f.light.range=3;UpdateFlareBurn(f);
   f.age+=dt;
   if(!f.landed){
    Vector3 old=f.root.transform.position;f.velocity+=Vector3.down*18*dt;
    Vector3 delta=f.velocity*dt,next=old+delta;
    bool hit=ProjectileContact(old,vr?.035f:.1f,delta,out var contact);
    if(hit){next=contact.point+contact.normal*.12f;f.landed=true;Sfx(landingSound);}
    // The creature's colliders are disabled, so test the swept flight separately.
    Vector3 center=monster.position+Vector3.up*1.5f;
    float along=delta.sqrMagnitude>.0001f?Mathf.Clamp01(Vector3.Dot(center-old,delta)/delta.sqrMagnitude):0;
    if(Vector3.Distance(old+delta*along,center)<1.5f){next=old+delta*along;f.landed=true;}
    if(!hit&&TryWaterImpact(old,next,out var splash)){next=splash;f.landed=true;Ripple(next);Sfx(splashClip);}
    f.root.transform.position=next;
    if(!f.landed){if(f.physicalThrow)RotateThrown(f.root.transform,f.angularVelocity,dt);else f.root.transform.Rotate(220*dt,70*dt,0,Space.Self);}

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
  TickBottleShards(dt);
  TickFlares(dt);
  foreach(var item in thrown){
   if(!item.active||item.held)continue;
   item.age+=dt;Vector3 old=item.t.position;
   item.velocity+=Vector3.down*18*dt;
   Vector3 next=old+item.velocity*dt,delta=next-old;
   bool hit=ProjectileContact(old,vr?.03f:.07f,delta,out var contact);
   bool splash=TryWaterImpact(old,next,out var waterPoint)&&(!hit||Vector3.Distance(old,waterPoint)<contact.distance);
   if(hit&&!splash){
    if(!item.pebble){BreakBottle(contact.point+contact.normal*.06f);item.active=false;item.t.gameObject.SetActive(false);continue;}
    if(item.age>=item.nextImpact&&item.velocity.sqrMagnitude>4){Sfx(landingSound);item.nextImpact=item.age+.2f;}next=contact.point+contact.normal*.08f;item.velocity=Vector3.Reflect(item.velocity,contact.normal)*.35f;
   }
   item.t.position=next;if(item.physicalThrow)RotateThrown(item.t,item.angularVelocity,dt);else item.t.Rotate(250*dt,80*dt,0,Space.Self);
   if(splash){
    next=waterPoint;Noise(next,item.pebble?4:9);Ripple(next);Sfx(splashClip);
    item.active=false;item.t.gameObject.SetActive(false);
   }else if(item.age>6){item.active=false;item.t.gameObject.SetActive(false);}
  }
 }
 void RotateThrown(Transform item,Vector3 spin,float dt){if(spin.sqrMagnitude>.0001f)item.rotation=Quaternion.AngleAxis(spin.magnitude*Mathf.Rad2Deg*dt,spin.normalized)*item.rotation;}
 readonly RaycastHit[] projectileHits=new RaycastHit[32];
 readonly Collider[] projectileOverlaps=new Collider[32];
 bool ProjectileContact(Vector3 position,float radius,Vector3 delta,out RaycastHit contact){
  contact=default;
  int overlaps=Physics.OverlapSphereNonAlloc(position,radius,projectileOverlaps,~0,QueryTriggerInteraction.Ignore);
  for(int i=0;i<overlaps;i++){
   var collider=projectileOverlaps[i];if(collider==body||collider.transform.IsChildOf(body.transform))continue;
   Vector3 point=collider.ClosestPoint(position),normal=(position-point).normalized;
   contact.point=point;contact.normal=normal.sqrMagnitude>.01f?normal:Vector3.up;contact.distance=0;return true;
  }
  int count=Physics.SphereCastNonAlloc(position,radius,delta.normalized,projectileHits,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
  float distance=float.PositiveInfinity;bool found=false;
  for(int i=0;i<count;i++){
   var hit=projectileHits[i];if(hit.collider==body||hit.collider.transform.IsChildOf(body.transform)||hit.distance>=distance)continue;
   contact=hit;distance=hit.distance;found=true;
  }
  return found;
 }
 bool TryWaterImpact(Vector3 from,Vector3 to,out Vector3 point){
  point=to;if(from.y<WaterY||to.y>WaterY||from.y<=to.y)return false;
  point=Vector3.Lerp(from,to,(from.y-WaterY)/(from.y-to.y));return IsOpenWater(point);
 }
 bool IsOpenWater(Vector3 point){
  if(Mathf.Abs(point.x)>=HallWidth||point.z<3.5f||point.z>EndZ-7)return false;
  // Exclude platforms and shore: the water mesh also extends underneath them.
  return !Physics.Raycast(new Vector3(point.x,WaterY+3,point.z),Vector3.down,3.04f,~0,QueryTriggerInteraction.Ignore);
 }

 sealed class BottleShard {public Transform t;public Vector3 velocity,spin;public float remaining;}
 readonly BottleShard[] bottleShards=new BottleShard[36];
 Mesh bottleShardMesh;AudioClip glassBreak;
 int nextBottleShard;
 void BreakBottle(Vector3 point){
  if(!glassBreak)glassBreak=Tone("Bottle crack",2400,.16f,true);Sfx(glassBreak);
  if(!bottleShardMesh){
   bottleShardMesh=new Mesh{name="Bottle shard"};
   bottleShardMesh.vertices=new[]{new Vector3(-.5f,0,0),new Vector3(.5f,0,0),new Vector3(.1f,1,.15f)};
   bottleShardMesh.triangles=new[]{0,1,2,2,1,0};bottleShardMesh.RecalculateNormals();
  }
  for(int i=0;i<12;i++){
   int slot=nextBottleShard++%bottleShards.Length;var shard=bottleShards[slot];
   if(shard==null){
    var root=new GameObject("Bottle fragment");root.transform.SetParent(transform);
    root.AddComponent<MeshFilter>().sharedMesh=bottleShardMesh;root.AddComponent<MeshRenderer>().sharedMaterial=bottleMaterial;
    shard=bottleShards[slot]=new BottleShard{t=root.transform};
   }
   shard.remaining=1.4f;shard.t.gameObject.SetActive(true);shard.t.position=point;
   shard.t.rotation=Random.rotation;shard.t.localScale=Vector3.one*Random.Range(.055f,.11f);
   shard.velocity=Random.onUnitSphere*Random.Range(.5f,2.2f)+Vector3.up*1.2f;shard.spin=Random.onUnitSphere*12;
  }
 }
 void TickBottleShards(float dt){
  foreach(var shard in bottleShards){
   if(shard==null||shard.remaining<=0)continue;
   shard.remaining-=dt;if(shard.remaining<=0){shard.t.gameObject.SetActive(false);continue;}
   shard.velocity+=Vector3.down*12*dt;var delta=shard.velocity*dt;
   if(Physics.Raycast(shard.t.position,delta.normalized,out var hit,delta.magnitude,~0,QueryTriggerInteraction.Ignore)){shard.t.position=hit.point+hit.normal*.008f;shard.velocity=Vector3.zero;shard.spin=Vector3.zero;}
   else shard.t.position+=delta;
   RotateThrown(shard.t,shard.spin,dt);if(shard.remaining<.3f)shard.t.localScale*=Mathf.Max(0,1-dt*10);
  }
 }
 void ResetBottleShards(){foreach(var shard in bottleShards)if(shard!=null){shard.remaining=0;shard.t.gameObject.SetActive(false);}}
}
}
