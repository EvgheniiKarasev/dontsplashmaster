using UnityEngine;
namespace DontSplash {
public sealed partial class SplashGame {
 const int RouteCount=9;
 float LaneX(int lane)=>Mathf.Lerp(-HallWidth+12,HallWidth-12,lane/(float)(RouteCount-1));
 bool ThroughRoute(int lane,int section)=>(lane+stage+leg+section)%4==0;
 void BuildRoutes(){
  for(int i=0;i<StepCount;i++){
   float z=8+i*StepSpacing;bool safe=i==9||i==20||i==31;
   if(safe){
    Vector3 size=new Vector3(HallWidth*2-5,.65f,4);
    var island=Shape("Cross-pool checkpoint",new Vector3(0,.1f,z),size,tile);
    steps.Add(new Step{t=island.transform,home=island.transform.position,size=size});
    DressCheckpoint(z,i);if(i<31)MakeValve(new Vector3(2,.5f,z));
    for(int lane=0;lane<RouteCount;lane++)Sign(i<31?(LaneX(lane)<0?"VALVE  >":LaneX(lane)>0?"<  VALVE":"VALVE / HOLD USE"):"EXIT AHEAD",new Vector3(LaneX(lane),1.8f,z),.5f);
   }else{
    int section=i<9?0:i<20?1:2,first=section==0?0:section==1?10:21;
    for(int lane=0;lane<RouteCount;lane++){
     // Identical entrances: dead ends are discovered through exploration, and remain reversible.
     int length=3+(lane*3+stage+leg+section)%4;
     if(!ThroughRoute(lane,section)&&i-first>=length)continue;
     float x=LaneX(lane)+Mathf.Sin((i-first)*.55f+lane*.7f+leg)*2.1f;
     int kind=stage==2?2:stage==3?1:stage==4?3:stage==5?(i%4==1?4:0):stage>=6?(i%4==1?2:i%4==2?1:0):0;
     Vector3 size=new Vector3(stage==1?2.5f:2.8f,.65f,2.8f);
     if(kind==3)size=new Vector3(4.1f,.65f,2.6f);
     var platform=Shape("Route "+(lane+1)+" / step "+i,new Vector3(x,.1f,z),size,kind==2?danger:kind==1?accent:tile);
     steps.Add(new Step{t=platform.transform,home=platform.transform.position,kind=kind,phase=i*.8f,size=size});
     if(!ThroughRoute(lane,section)&&i-first==length-1)PlaceBonusCoin(new Vector3(x,1.05f,z),section+"."+lane);
     var trim=Shape("Worn edge",new Vector3(x,.44f,z-size.z*.5f+.1f),new Vector3(size.x,.025f,.09f),accent,false);trim.transform.SetParent(platform.transform,true);
     if(stage==6&&(i==5||i==16||i==27)){var crusher=Shape("Pressure piston",new Vector3(x,5,z),new Vector3(3.4f,3,1.2f),danger,false);crushers.Add(crusher.transform);}
    }
   }
   if(i%3==0)BuildBay(i,z);
  }
 }
 public GameObject bakedLobby;
 void BuildPoolColumns(){
  for(int lane=0;lane<RouteCount-1;lane++){
   float x=(LaneX(lane)+LaneX(lane+1))*.5f;
   for(int row=0;row<7;row++){
    float z=20+row*26;
    // Checkpoint strips stay unobstructed.
    if(Mathf.Abs(z-(8+9*StepSpacing))<5||Mathf.Abs(z-(8+20*StepSpacing))<5||Mathf.Abs(z-(8+31*StepSpacing))<5)continue;
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
  Prop(0,new Vector3(-8,.5f,-9),1.8f,90);Prop(1,new Vector3(8,.5f,-9),.8f,0);
  BuildShopStalls();
  for(int lane=0;lane<RouteCount;lane++)Sign("PATH "+(lane+1),new Vector3(LaneX(lane),.9f,2.6f),.4f);
 }
 bool creatureRevealed,catchStrikeStarted,catchHit;float catchClock,catchFadeStarted;
 float CatchBlackout=>catchHit?Mathf.SmoothStep(0,1,(catchClock-catchFadeStarted)/.65f):0;Vector3 catchStart;Quaternion catchRotation;
 void ParkCreature(){creatureRevealed=false;monster.position=new Vector3(body.transform.position.x,WaterY-.6f,body.transform.position.z+RenderSettings.fogEndDistance+15);}
 void RevealCreature(Vector3 noise){
  if(creatureRevealed||!monster)return;
  creatureRevealed=true;
  // Emerge from beyond the fog boundary, never spawn visibly next to a quiet player.
  Vector3 away=Vector3.ProjectOnPlane(noise-body.transform.position,Vector3.up).normalized;
  if(away.sqrMagnitude<.1f)away=Vector3.forward;
  Vector3 p=body.transform.position+away*(RenderSettings.fogEndDistance+4);p.y=WaterY-.6f;
  monster.position=p;monster.LookAt(new Vector3(noise.x,p.y,noise.z));
 }
 void TickCreature(float dt){
  heard-=dt;attackRecovery-=dt;
  if(wet>0&&heard<=0){Noise(body.transform.position,3);}
  if(!creatureRevealed){ParkCreature();creature.Tick(dt,false,false,false,100,0);return;}
  Vector3 target=heard>0?sound:body.transform.position+Vector3.forward*(RenderSettings.fogEndDistance+12);
  target.y=WaterY-.6f;Vector3 old=monster.position;
  if(stun<=0){monster.position=Vector3.MoveTowards(old,target,(heard>0?11:5)*dt);Vector3 dir=target-monster.position;if(dir.sqrMagnitude>.05f)monster.rotation=Quaternion.Slerp(monster.rotation,Quaternion.LookRotation(dir),dt*7);}
  float distance=Vector3.Distance(monster.position,body.transform.position);
  creature.Tick(dt,true,heard>0,stun>0,distance,Vector3.Distance(old,monster.position)/Mathf.Max(dt,.001f));
  if(wet>0&&stun<=0&&distance<2.5f){BeginCatch();return;}
  if(heard<=0&&distance>RenderSettings.fogEndDistance+3)ParkCreature();
 }
 void BeginCatch(){
  mode=Mode.Caught;catchClock=0;catchStrikeStarted=catchHit=false;catchFadeStarted=0;catchStart=eye.transform.position;catchRotation=eye.transform.rotation;
  move=look=Vector2.zero;throwInput=flareInput=false;moveFinger=lookFinger=-1;
  Vector3 direction=Vector3.ProjectOnPlane(monster.position-body.transform.position,Vector3.up).normalized;
  if(direction.sqrMagnitude<.1f)direction=body.transform.forward;
  monster.position=body.transform.position+direction*2.2f;monster.position=new Vector3(monster.position.x,WaterY-.6f,monster.position.z);
  monster.rotation=Quaternion.LookRotation(-direction);
  creature.ResetPose();creature.Hear();CursorState(true);
 }
 void TickCatch(){
  if(portal.Busy)return;
  float dt=Time.deltaTime;catchClock+=dt;
  const float inspectDuration=1.65f;
  if(!catchStrikeStarted&&catchClock>=inspectDuration){catchStrikeStarted=true;creature.BeginAttack();}
  bool impact=false;
  if(!catchHit){
   impact=creature.Tick(dt,true,catchStrikeStarted,false,1,0);
   if(!catchStrikeStarted)creature.InspectVictim(Mathf.Clamp01(catchClock/inspectDuration));
  }
  if(!catchHit&&(impact||catchClock>=inspectDuration+.6f)){
   catchHit=true;catchFadeStarted=catchClock;creature.PlayImpact();
  }
  Vector3 face=creature.FacePosition;
  Vector3 cameraPosition=face+monster.forward*1.5f+Vector3.up*.08f;
  float t=Mathf.SmoothStep(0,1,catchClock/.16f);
  eye.transform.position=Vector3.Lerp(catchStart,cameraPosition,t);
  eye.transform.rotation=Quaternion.Slerp(catchRotation,Quaternion.LookRotation(face-eye.transform.position),t);
  eye.fieldOfView=Mathf.Lerp(76,64,t);
  if(catchHit){
   float recoil=Mathf.Exp(-(catchClock-catchFadeStarted)*9);
   eye.transform.position-=eye.transform.forward*.12f*recoil;
   eye.transform.rotation*=Quaternion.Euler(-5*recoil,0,3*recoil);
  }
  if(catchHit&&catchClock-catchFadeStarted>=.65f){deaths++;mode=Mode.Dead;portal.StopPlay();CursorState(false);}
 }
}
}
