using UnityEngine;
namespace DontSplash {
public sealed partial class SplashGame {
 const int RouteCount=6;
 float LaneX(int lane)=>Mathf.Lerp(-HallWidth+12,HallWidth-12,lane/(float)(RouteCount-1));
 bool ThroughRoute(int lane,int section)=>(lane+stage+leg+section)%4==0;
 // Route order follows waypoints, not increasing Z rows.
 void BuildRoutes(){
  bool simple=stage*LegCount+leg<2;
  if(FirstChamber){
   for(int i=0;i<4;i++){
    Vector3 pos=new Vector3(0,.1f,7+i*5),size=new Vector3(4,.65f,3.5f);
    var platform=Shape("First jumps "+(i+1),pos,size,tile);
    steps.Add(new Step{t=platform.transform,home=pos,size=size});
    var edge=Shape("Entrance highlight",pos+new Vector3(0,.34f,-1.65f),new Vector3(4,.025f,.12f),brass,false);
    edge.transform.SetParent(platform.transform,true);
   }
   PlaceBonusCoin(new Vector3(0,1.05f,17),"FirstJumps");
  }
  for(int section=0;section<(simple?1:3);section++){
   float start=FirstChamber?32:section==0?8:8+(section==1?9:20)*StepSpacing+5;
   float end=simple?EndZ-8:8+(section==0?9:section==1?20:31)*StepSpacing-5;
   for(int lane=0;lane<RouteCount;lane++){
    if(FirstChamber&&lane!=2&&lane!=3)continue;
    bool complete=FirstChamber?lane==2:ThroughRoute(lane,section);
    float finish=complete?end:Mathf.Lerp(start,end,.28f+((lane*3+leg)%5)*.1f);
    float side=(lane+leg+section)%2==0?1:-1;
    float center=LaneX(lane);
    var points=new System.Collections.Generic.List<Vector3>();
    if(FirstChamber)points.Add(new Vector3(lane==2?-4.5f:4.5f,.1f,25));
    points.Add(new Vector3(center-side*6,.1f,start));
    // Wide doglegs include a genuine backward leg before progressing again.
    for(float z=start;z+38<finish;z+=42){
     points.Add(new Vector3(center-side*6,.1f,z+13));
     points.Add(new Vector3(center,.1f,z+17));
     points.Add(new Vector3(center+side*6,.1f,z+12));
     points.Add(new Vector3(center+side*6,.1f,z+30));
     points.Add(new Vector3(center-side*6,.1f,z+38));
    }
    Vector3 endpoint=new Vector3(center-side*6,.1f,finish);
    // A short final leg used to create almost coincident landing tiles.
    if(points.Count>1&&Vector3.Distance(points[points.Count-1],endpoint)<4f)points.RemoveAt(points.Count-1);
    points.Add(endpoint);
    int index=0;
    for(int segment=0;segment<points.Count-1;segment++){
     Vector3 from=points[segment],to=points[segment+1];
     int count=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(from,to)/5.8f));
     for(int k=segment==0?0:1;k<=count;k++){
      Vector3 pos=Vector3.Lerp(from,to,k/(float)count);
      float width=Mathf.Lerp(2.2f,2.8f,(Mathf.Sin(index*1.7f+lane+leg)+1)*.5f);
      Vector3 size=new Vector3(width,.65f,2.4f);
      // Turn landings remain stable; hazards occupy the connecting legs.
      int kind=k==0||k==count?0:stage==2?2:stage==3?1:stage==4?3:stage==5&&index%4==1?4:stage>=6&&index%4==1?2:0;
      if(MovingIntroduction&&k>0&&k<count&&index%3==1){kind=1;size.x=3.4f;}
      var platform=Shape("Route "+(lane+1)+" step "+index,pos,size,kind==2?danger:kind==1?accent:tile);
      steps.Add(new Step{t=platform.transform,home=pos,size=size,kind=kind,phase=index*.8f});
      var trim=Shape("Worn edge",pos+new Vector3(0,.34f,-size.z*.5f+.1f),new Vector3(size.x,.025f,.09f),accent,false);
      trim.transform.SetParent(platform.transform,true);
      if(stage==6&&index%11==5){var crusher=Shape("Pressure piston",pos+Vector3.up*4.9f,new Vector3(3.4f,3,1.2f),danger,false);crushers.Add(crusher.transform);}
      index++;
     }
    }
    if(!complete)PlaceBonusCoin(points[points.Count-1]+Vector3.up*.95f,section+"."+lane);
   }
   if(!simple){
    int row=section==0?9:section==1?20:31;float z=8+row*StepSpacing;
    Vector3 size=new Vector3(HallWidth*2-5,.65f,4);
    var island=Shape("Cross-pool island",new Vector3(0,.1f,z),size,tile);
    steps.Add(new Step{t=island.transform,home=island.transform.position,size=size});
    DressCheckpoint(z,row);if(section<2)MakeValve(new Vector3(2,.5f,z));
   }
  }
  for(int i=0;i<StepCount&&8+i*StepSpacing<EndZ;i+=3)BuildBay(i,8+i*StepSpacing);
  BuildPoolColumns();
 }
 public GameObject bakedLobby;
 void BuildPoolColumns(){
  for(int lane=0;lane<RouteCount-1;lane++){
   float x=(LaneX(lane)+LaneX(lane+1))*.5f;
   for(int row=0;row<7;row++){
    float z=20+row*26;
    if(z>EndZ-10)continue;
    // Checkpoint strips stay unobstructed.
    if(Mathf.Abs(z-(8+9*StepSpacing))<5||Mathf.Abs(z-(8+20*StepSpacing))<5||Mathf.Abs(z-(8+31*StepSpacing))<5)continue;
    bool obstructed=false;
    foreach(var step in steps){
     if(step.size.x>10)continue;
     // Include landing and jump clearance around each column.
     if(Mathf.Abs(step.home.x-x)<step.size.x*.5f+3.2f&&Mathf.Abs(step.home.z-z)<step.size.z*.5f+3.2f){obstructed=true;break;}
    }
    if(obstructed)continue;
    float height=HallHeight+2;
    Shape("Pool column",new Vector3(x,(HallHeight-2)*.5f,z),new Vector3(3.8f,height,3.8f),tile);
    Shape("Column capital",new Vector3(x,HallHeight-.45f,z),new Vector3(4.5f,.65f,4.5f),tile);
    Shape("Column water stain",new Vector3(x,WaterY-.13f,z),new Vector3(3.83f,.3f,3.83f),accent,false);
   }
  }
 }
 void BuildArrivalRoom(){
  Shape("Wide starting shore",new Vector3(0,0,-1.75f),new Vector3(HallWidth*2-1,1,10.5f),tile);
  if(bakedLobby)Instantiate(bakedLobby,room);
  else {
  Shape("Arrival floor",new Vector3(0,0,-18),new Vector3(22,1,22),tile);
  Shape("Arrival ceiling",new Vector3(0,7,-18),new Vector3(23,.5f,23),tile);
  Shape("Arrival back wall",new Vector3(0,3.5f,-29),new Vector3(23,7,.5f),tile);
  for(int side=-1;side<=1;side+=2){
   Shape("Arrival side",new Vector3(side*11,3.5f,-18),new Vector3(.5f,7,22),tile);
   Shape("Doorway side",new Vector3(side*7.6f,3.5f,-7),new Vector3(7,7,.6f),tile);

  }
  Shape("Doorway lintel",new Vector3(0,6,-7),new Vector3(8.2f,2,.7f),tile);
  }
  for(int side=-1;side<=1;side+=2)Shape("Shore back wall",new Vector3(side*(HallWidth+11)/2,4,-7),new Vector3(HallWidth-11,8,.6f),tile);
  for(int x=-1;x<=1;x++)for(int z=0;z<2;z++){
   Shape("Fluorescent housing",new Vector3(x*7,6.7f,-12-z*11),new Vector3(3.3f,.2f,1),metal,false);
   Shape("Warm fluorescent",new Vector3(x*7,6.55f,-12-z*11),new Vector3(3,.04f,.8f),glow,false);
  }
  Shape("Entrance lamp housing",new Vector3(0,5,-7.6f),new Vector3(4.4f,.2f,.8f),metal,false);
  Shape("Entrance warm light",new Vector3(0,4.88f,-7.6f),new Vector3(4,.04f,.6f),glow,false);
  Sign("DONT SPLASH.\nIT HEARS THE WATER.",new Vector3(0,3.6f,-7.4f),1);
  Sign("Choose a path. Some end in the water.\nTurn back before you run out of platforms.",new Vector3(0,1.9f,-7.45f),.5f);
  BuildShopStalls();
  BuildShoreGuide();
 }
 GameObject shoreGuide;bool shoreGuideReached;
 void BuildShoreGuide(){
  if(!FirstChamber||shoreGuideReached)return;
  var sprite=Resources.Load<Sprite>("UI/ShoreArrow");
  if(!sprite)return;
  shoreGuide=new GameObject("First jump floor guide");shoreGuide.transform.SetParent(room,false);
  for(float z=-15;z<=2;z+=2.8f){
   var arrow=new GameObject("Guide arrow",typeof(SpriteRenderer));
   arrow.transform.SetParent(shoreGuide.transform,false);
   arrow.transform.position=new Vector3(0,.515f,z);
   arrow.transform.rotation=Quaternion.Euler(90,0,0);
   arrow.transform.localScale=Vector3.one*(1.4f/Mathf.Max(sprite.bounds.size.y,.01f));
   var renderer=arrow.GetComponent<SpriteRenderer>();renderer.sprite=sprite;
   renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
  }
 }
 void TickShoreGuide(){
  if(!shoreGuide||shoreGuideReached)return;
  Vector3 position=body.transform.position;
  if(body.isGrounded&&Mathf.Abs(position.x)<2.2f&&position.z>=1.5f&&position.z<=4.2f){
   shoreGuideReached=true;shoreGuide.SetActive(false);

  }
 }
 bool creatureRevealed,catchStrikeStarted,catchHit;float catchClock,catchFadeStarted;
 float CatchBlackout=>catchHit?Mathf.SmoothStep(0,1,(catchClock-catchFadeStarted)/.65f):0;Vector3 catchStart;Quaternion catchRotation;
 Vector3 patrolCenter;float pursuit;
 Vector3 vrCatchDirection;
 void ParkCreature(){creatureRevealed=true;pursuit=0;patrolCenter=new Vector3(0,WaterY-.6f,EndZ*.5f);monster.position=patrolCenter;}
 void RevealCreature(Vector3 noise){
  if(creatureRevealed||!monster)return;
  creatureRevealed=true;
  // Emerge from beyond the fog boundary, never spawn visibly next to a quiet player.
  Vector3 away=Vector3.ProjectOnPlane(noise-body.transform.position,Vector3.up).normalized;
  if(away.sqrMagnitude<.1f)away=Vector3.forward;
  Vector3 p=body.transform.position+away*(RenderSettings.fogEndDistance+4);p.y=WaterY-.6f;
  monster.position=p;monster.LookAt(new Vector3(noise.x,p.y,noise.z));
 }
 const float CreatureStandOff=1.55f;
 void TickCreature(float dt){
  heard-=dt;attackRecovery-=dt;pursuit=Mathf.Max(0,pursuit-dt);if(wet>0)pursuit=1.4f;
  if(wet>0&&heard<=0){Noise(body.transform.position,3);}
  if(!creatureRevealed){ParkCreature();creature.Tick(dt,false,false,false,100,0);return;}
  Vector3 target=pursuit>0?body.transform.position:heard>0?sound:patrolCenter+new Vector3(Mathf.Sin(simTime*.24f)*10,0,Mathf.Cos(simTime*.24f)*6);
  target.y=WaterY-.6f;Vector3 old=monster.position;
  Vector3 next=stun<=0?Vector3.MoveTowards(old,target,(heard>0||pursuit>0?18:5)*dt):old;
  // Keep the entire approach outside the camera's personal space, even on dry tiles.
  Vector3 separation=Vector3.ProjectOnPlane(next-body.transform.position,Vector3.up);
  if(separation.sqrMagnitude<CreatureStandOff*CreatureStandOff){
   Vector3 away=Vector3.ProjectOnPlane(old-body.transform.position,Vector3.up);
   if(away.sqrMagnitude<.001f)away=Vector3.ProjectOnPlane(body.transform.forward,Vector3.up);
   next=body.transform.position+away.normalized*CreatureStandOff;
   next.y=WaterY-.6f;
  }
  monster.position=next;
  if(stun<=0){Vector3 dir=next-old;if(dir.sqrMagnitude>.000001f)monster.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(dir,Vector3.up));}
  float horizontalDistance=Vector3.ProjectOnPlane(monster.position-body.transform.position,Vector3.up).magnitude;
  float distance=Vector3.Distance(monster.position,body.transform.position);
  creature.Tick(dt,true,heard>0||pursuit>0,stun>0,distance,Vector3.Distance(old,monster.position)/Mathf.Max(dt,.001f));
  if((wet>0||(pursuit>0&&!body.isGrounded))&&stun<=0&&horizontalDistance<=CreatureStandOff+.1f){BeginCatch();return;}

 }
 void BeginCatch(){
  EndAttempt(false);
  mode=Mode.Caught;catchClock=0;catchStrikeStarted=catchHit=false;catchFadeStarted=0;catchStart=eye.transform.position;catchRotation=eye.transform.rotation;
  move=look=Vector2.zero;throwInput=flareInput=false;moveFinger=lookFinger=-1;
  Vector3 direction=Vector3.ProjectOnPlane(monster.position-body.transform.position,Vector3.up).normalized;
  if(direction.sqrMagnitude<.1f)direction=body.transform.forward;
  monster.position=body.transform.position+direction*CreatureStandOff;monster.position=new Vector3(monster.position.x,WaterY-.6f,monster.position.z);
  monster.rotation=Quaternion.LookRotation(-direction);
  creature.ResetPose();creature.Hear();CursorState(true);
  if(vr){
   vrCatchDirection=Vector3.ProjectOnPlane(eye.transform.forward,Vector3.up).normalized;
   if(vrCatchDirection.sqrMagnitude<.01f)vrCatchDirection=body.transform.forward;
   AlignVRCatch();
  }
 }
 void AlignVRCatch(){
  Vector3 head=eye.transform.position;
  monster.rotation=Quaternion.LookRotation(-vrCatchDirection,Vector3.up);
  float approach=catchStrikeStarted?Mathf.SmoothStep(0,1,creature.StrikeProgress):0;
  Vector3 faceTarget=head+vrCatchDirection*Mathf.Lerp(1.25f,.95f,approach);
  // Correct the sampled pose, including its vertical and sideways root motion.
  // The player keeps control of their head throughout the sequence.
  monster.position+=faceTarget-creature.FacePosition;
  creature.AimAtVictim(head,catchStrikeStarted);
 }
 void TickCatch()=>StepCatch(Time.deltaTime);
 void StepCatch(float dt){
  catchClock+=dt;
  float inspectDuration=deaths==0?2f:1.3f;
  if(!catchStrikeStarted&&catchClock>=inspectDuration){catchStrikeStarted=true;creature.BeginAttack();}
  bool impact=false;
  if(!catchHit){
   impact=creature.Tick(dt,true,catchStrikeStarted,false,1,0);
   if(!catchStrikeStarted)creature.InspectVictim(Mathf.Clamp01(catchClock/inspectDuration));
  }
  if(!catchHit&&(impact||catchClock>=inspectDuration+.85f)){
   catchHit=true;catchFadeStarted=catchClock;creature.PlayImpact();
  }
  if(vr)AlignVRCatch();
  if(!vr){
  Vector3 face=creature.FacePosition;
  Vector3 cameraPosition=face+monster.forward*1.25f+Vector3.up*.08f;
  float t=Mathf.SmoothStep(0,1,catchClock/.16f);
  eye.transform.position=Vector3.Lerp(catchStart,cameraPosition,t);
  eye.transform.rotation=Quaternion.Slerp(catchRotation,Quaternion.LookRotation(face-eye.transform.position),t);
  eye.fieldOfView=Mathf.Lerp(76,64,t);
  if(catchHit){
   float recoil=Mathf.Exp(-(catchClock-catchFadeStarted)*9);
   eye.transform.position-=eye.transform.forward*.12f*recoil;
   eye.transform.rotation*=Quaternion.Euler(-5*recoil,0,3*recoil);
  }
  }
  if(catchHit&&catchClock-catchFadeStarted>=.65f){deaths++;mode=Mode.Dead;CursorState(false);}
 }
}
}
