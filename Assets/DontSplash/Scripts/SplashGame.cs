using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace DontSplash {
public sealed partial class SplashGame : MonoBehaviour {
 public GameObject monsterAsset, bottleAsset;
 public AnimationClip monsterAttack,monsterIdle,monsterRun; MonsterPerformance creature; float attackRecovery,hitShake;
 public Material tileMaterial,surfaceMaterial, monsterMaterial, bottleMaterial, waterMaterial;
 enum Mode {Menu,Play,Pause,Shop,Caught,Dead,Between,Win}
 Mode mode;
 CharacterController body; Camera eye; Transform monster,room; PortalService portal;
 Material tile,dark,water,glow,danger,brass; Texture2D grid;
 AudioSource audioSource,ambience; AudioClip splashClip,jumpClip,clickClip,alarmClip;
 readonly List<Transform> crushers=new List<Transform>(); readonly List<Step> steps=new List<Step>(); readonly List<Valve> valves=new List<Valve>(); readonly List<Transform> ripples=new List<Transform>();
 Vector3 sound,spawn; float heard,vertical,yaw,pitch,elapsed,wet,cooldown,simTime,coyote,jumpBuffer,stun,noticeTime,hold;
 int stage,leg,deaths,stones=5,flares=2,completed; bool touchMode,muted,assist,hadPointerLock; string notice="";
 Vector2 move,look,moveOrigin; bool jump,use,throwInput,flareInput;
 int moveFinger=-1,lookFinger=-1,useFinger=-1; float checkpointZ; int checkpointIndex;
 const int StageCount=9,LegCount=3,StepCount=32;
 const float StepSpacing=5.8f, EndZ=196;
 string[] names={"THE QUIET HALL","BROKEN WALKWAY","SINKING STEPS","SHIFTING TIDES","ROTATING WORKS","FALSE FLOORS","PRESSURE CHAMBER","BLACKOUT","THE LAST GATE"};
 string[] tips={"Water carries sound. Stay on the tiled islands.","Use the full width of a platform. Jump near its edge.","Orange tiles sink under your feet. Keep moving.","Ride the blue platforms. Wait for the right moment.","Bridges turn. Land near the centre to stay balanced.","Green marks the safe route. Red tiles drop away.","Hold USE at both valves, then reach the exit.","Your lamp is your guide. Save a flare for trouble.","Open both valves. Remember the emergency code:  4  1  7  3"};
 class Step {public Transform t;public Vector3 home;public int kind;public float phase,sink;public Vector3 size;}
 class Valve {public Transform t;public bool done; public float charge;}
 float uiScale,uw,uh; string code=""; bool codeOpen;
 void Start(){
  Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
  portal=gameObject.AddComponent<PortalService>();portal.Initialize();
  tile=tileMaterial?new Material(tileMaterial):Mat(new Color(.64f,.71f,.62f));dark=Mat(new Color(.055f,.12f,.13f));water=waterMaterial?new Material(waterMaterial):Mat(new Color(.025f,.24f,.26f));glow=Mat(new Color(.37f,1,.76f),true);danger=Mat(new Color(.9f,.35f,.14f));brass=Mat(new Color(.75f,.64f,.32f));
  grid=new Texture2D(64,64,TextureFormat.RGB24,false);var pixels=new Color[4096];
  for(int y=0;y<64;y++)for(int x=0;x<64;x++)pixels[y*64+x]=(x<2||y<2)?new Color(.46f,.5f,.46f):new Color(.9f,.93f,.87f);
  grid.SetPixels(pixels);grid.Apply();grid.wrapMode=TextureWrapMode.Repeat;tile.mainTexture=grid;tile.mainTextureScale=Vector2.one*4;
  RenderSettings.ambientLight=new Color(.48f,.59f,.55f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=8;RenderSettings.fogEndDistance=32;RenderSettings.fogColor=new Color(.035f,.11f,.12f);
  var sun=new GameObject("Ceiling bounce").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.85f;sun.transform.rotation=Quaternion.Euler(65,30,0);sun.shadows=LightShadows.None;
  var player=new GameObject("Player");body=player.AddComponent<CharacterController>();body.height=1.8f;body.radius=.28f;body.center=Vector3.up*.9f;body.stepOffset=.28f;
  eye=new GameObject("Camera").AddComponent<Camera>();eye.transform.SetParent(player.transform);eye.transform.localPosition=Vector3.up*1.6f;eye.nearClipPlane=.07f;eye.farClipPlane=115;eye.fieldOfView=76;eye.gameObject.AddComponent<AudioListener>();eye.backgroundColor=RenderSettings.fogColor;eye.clearFlags=CameraClearFlags.SolidColor;var cameraData=eye.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();cameraData.requiresDepthTexture=true;cameraData.requiresColorTexture=false;
  var lamp=eye.gameObject.AddComponent<Light>();lamp.type=LightType.Spot;lamp.range=24;lamp.spotAngle=75;lamp.intensity=2;lamp.color=new Color(1,.92f,.69f);lamp.shadows=LightShadows.None;
  audioSource=gameObject.AddComponent<AudioSource>();audioSource.volume=.4f;
  splashClip=Tone("Splash",110,.6f,true);jumpClip=Tone("Jump",250,.12f,false);clickClip=Tone("Valve",620,.16f,false);alarmClip=Tone("Threat",65,.5f,false);
  ambience=gameObject.AddComponent<AudioSource>();ambience.clip=Tone("Ventilation",52,3,true);ambience.loop=true;ambience.volume=.055f;
  touchMode=Application.isMobilePlatform;
  LoadEconomy();InitializeFX();InitializeThrows();BuildRoom();InitializeUI();portal.Loaded();
 }
 Material Mat(Color c,bool emission=false){var m=surfaceMaterial?new Material(surfaceMaterial):new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=c;if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*1.4f);}return m;}
 AudioClip Tone(string n,float f,float seconds,bool noise){int count=(int)(22050*seconds);float[] data=new float[count];uint state=17;for(int i=0;i<count;i++){state=state*1664525+1013904223;float random=(state>>8)/16777216f*2-1;float t=i/22050f;data[i]=(Mathf.Sin(t*f*6.28318f)*.3f+(noise?random*.2f:0))*Mathf.Sin(Mathf.PI*i/count);}var clip=AudioClip.Create(n,count,1,22050,false);clip.SetData(data,0);return clip;}
 void Sfx(AudioClip clip){if(!muted)audioSource.PlayOneShot(clip);}
 GameObject Shape(string n,Vector3 p,Vector3 size,Material m,bool solid=true,PrimitiveType shape=PrimitiveType.Cube){
  var g=GameObject.CreatePrimitive(shape);g.name=n;g.transform.SetParent(room);g.transform.position=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;
  if(!solid){var c=g.GetComponent<Collider>();if(c){c.enabled=false;Destroy(c);}}return g;
 }
 void Sign(string text,Vector3 p,float size=1){
  var g=new GameObject("Sign / "+text.Split('\n')[0]);g.transform.SetParent(room);
  g.transform.position=p;g.transform.rotation=Quaternion.identity;
  var t=g.AddComponent<TMPro.TextMeshPro>();
  t.font=uiHeadingFont?uiHeadingFont:uiBodyFont;t.text=text;
  t.alignment=TMPro.TextAlignmentOptions.Center;t.fontSize=4.8f;
  t.rectTransform.sizeDelta=new Vector2(32,8);g.transform.localScale=Vector3.one*size;
  t.textWrappingMode=TMPro.TextWrappingModes.NoWrap;t.overflowMode=TMPro.TextOverflowModes.Overflow;
  t.color=new Color(.85f,.91f,.71f);t.raycastTarget=false;
  t.renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;t.renderer.receiveShadows=false;
  roomLabels.Add(t);
 }

 void BuildRoom(){
  ResetThrows();
  if(room){room.gameObject.SetActive(false);Destroy(room.gameObject);}roomLabels.Clear();steps.Clear();valves.Clear();crushers.Clear();ripples.Clear();room=new GameObject("Chamber "+stage+"."+leg).transform;
  roomSplashed=false;pebbleSkips=0;bonusCoins.Clear();checkpointZ=0;checkpointIndex=-1;codeOpen=false;code="";hold=0;heard=0;stun=0;wet=0;simTime=0;
  ApplyAtmosphere();
  Shape("Water / Stylized Water 2",new Vector3(0,WaterY,EndZ/2),new Vector3(HallWidth*.2f,1,(EndZ+20)/10),water,false,PrimitiveType.Plane);
  Shape("Pool bottom",new Vector3(0,-2.5f,EndZ/2),new Vector3(HallWidth*2,1,EndZ+20),dark);
  BuildArchitecture();BuildRoutes();BuildArrivalRoom();
  Shape("Exit landing",new Vector3(0,.1f,EndZ),new Vector3(HallWidth*2-1,1,14),tile);
  Shape("Door left",new Vector3(-3,2,EndZ+2),new Vector3(.35f,4,.4f),glow);
  Shape("Door right",new Vector3(3,2,EndZ+2),new Vector3(.35f,4,.4f),glow);
  Shape("Door top",new Vector3(0,4,EndZ+2),new Vector3(6,.35f,.4f),glow);
  Sign("EXIT  /  "+(leg+1)+" OF 3",new Vector3(0,3,EndZ+1),.8f);
  var m=Instantiate(monsterAsset,room);monster=new GameObject("Creature navigation").transform;monster.SetParent(room);m.transform.SetParent(monster,false);
  foreach(var c in m.GetComponentsInChildren<Collider>()){c.enabled=false;Destroy(c);}
  var renderers=m.GetComponentsInChildren<Renderer>();Bounds b=new Bounds(monster.position,Vector3.zero);foreach(var r in renderers){b.Encapsulate(r.bounds);if(monsterMaterial)r.sharedMaterial=monsterMaterial;}
  if(b.size.y>.01f)m.transform.localScale*=3.4f/b.size.y;
  monster.position=new Vector3(7,-.5f,18);
  creature=monster.gameObject.AddComponent<MonsterPerformance>();creature.Initialize(m.transform,monsterAttack,monsterIdle,monsterRun);
  spawn=new Vector3(0,.65f,-18);Respawn();stones=5;flares=2;Notice(tips[stage],9);
  DressEntranceAndExit();
 }
 void MakeValve(Vector3 p){
  Shape("Valve pedestal",p+Vector3.up*.45f,new Vector3(.5f,.9f,.5f),dark);
  var g=Shape("Valve",p+Vector3.up*1.1f,new Vector3(.65f,.12f,.65f),brass,false,PrimitiveType.Cylinder);
  g.transform.rotation=Quaternion.Euler(90,0,0); for(int j=0;j<4;j++){var spoke=Shape("Valve spoke",g.transform.position,new Vector3(.8f,.08f,.09f),brass,false);spoke.transform.rotation=Quaternion.Euler(0,0,j*45);spoke.transform.SetParent(g.transform,true);} valves.Add(new Valve{t=g.transform});Sign("HOLD E / USE",p+Vector3.up*2,.42f);
 }
 void Notice(string text,float time=4){notice=text;noticeTime=time;}
 void Respawn(){
  if(creature)creature.ResetPose();attackRecovery=0;hitShake=0;
  body.enabled=false;body.transform.position=spawn;body.enabled=true;vertical=-2;wet=0;heard=0;jump=false;lookReady=false;moveFinger=lookFinger=useFinger=-1;move=look=Vector2.zero;throwInput=flareInput=jump=false;use=false;
  yaw=0;pitch=0;eye.fieldOfView=76;eye.transform.localRotation=Quaternion.identity;ParkCreature();
  foreach(var p in steps){p.sink=0;p.t.position=p.home;p.t.rotation=Quaternion.identity;}
 }
 void CursorState(bool locked){hadPointerLock=false;Cursor.lockState=locked&&!touchMode?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!locked||touchMode;}
 void StartRun(bool saved){
  stage=saved?Mathf.Clamp(PlayerPrefs.GetInt("Chamber"),0,8):0;leg=saved?Mathf.Clamp(PlayerPrefs.GetInt("Leg"),0,2):0;
  elapsed=saved?PlayerPrefs.GetFloat("Time"):0;deaths=0;completed=stage*3+leg;
  BuildRoom();mode=Mode.Play;CursorState(true);portal.StartPlay();if(!muted)ambience.Play();
 }
 void Pause(){if(mode!=Mode.Play)return;mode=Mode.Pause;portal.StopPlay();CursorState(false);AudioListener.pause=true;moveFinger=lookFinger=useFinger=-1;move=look=Vector2.zero;throwInput=flareInput=jump=false;use=false;}
 void Resume(){if(portal.Busy)return;portal.Break(()=>{mode=Mode.Play;CursorState(true);portal.StartPlay();});}
 void Die(){if(mode==Mode.Play)BeginCatch();}
 void Save(){PlayerPrefs.SetInt("Chamber",stage);PlayerPrefs.SetInt("Leg",leg);PlayerPrefs.SetFloat("Time",elapsed);PlayerPrefs.SetInt("HasSave",1);PlayerPrefs.Save();}
 void Finish(){
  RewardRoom();portal.StopPlay();CursorState(false);completed++;Sfx(clickClip);
  if(stage==8&&leg==2){mode=Mode.Win;PlayerPrefs.DeleteKey("HasSave");PlayerPrefs.Save();}
  else{mode=Mode.Between;leg++;if(leg>=LegCount){stage++;leg=0;}Save();}
 }
 void Update(){
  if(Touchscreen.current!=null&&Touchscreen.current.primaryTouch.press.isPressed)touchMode=true;
  var k=Keyboard.current;
  if(k!=null&&k.escapeKey.wasPressedThisFrame){if(mode==Mode.Play)Pause();else if(mode==Mode.Pause)Resume();else if(mode==Mode.Shop)CloseShop();}
  if(mode==Mode.Caught){TickCatch();return;}
  if(mode!=Mode.Play||portal.Busy)return;
  if(Cursor.lockState==CursorLockMode.Locked)hadPointerLock=true; if(!touchMode&&hadPointerLock&&Cursor.lockState!=CursorLockMode.Locked){hadPointerLock=false;Pause();return;}
  float dt=Mathf.Min(Time.deltaTime,.05f);elapsed+=dt;simTime+=dt;noticeTime-=dt;cooldown-=dt;stun-=dt;
  ReadInput();TickEconomy(dt);if(mode!=Mode.Play)return;
  UpdateLook(dt);TickThrows(dt);
  if(jump)jumpBuffer=.16f;else jumpBuffer-=dt;jump=false;
  bool grounded=body.isGrounded;if(grounded)coyote=.12f;else coyote-=dt;
  foreach(var p in steps){
   if(p.kind<1||p.kind>4)continue;
   if((p.t.position-body.transform.position).sqrMagnitude>1600)continue;
   Vector3 old=p.t.position;Vector3 local=p.t.InverseTransformPoint(body.transform.position);
   bool on=grounded&&Mathf.Abs(local.x)<.55f&&Mathf.Abs(local.z)<.55f&&body.transform.position.y>old.y&&body.transform.position.y<old.y+.8f;
   Quaternion oldRotation=p.t.rotation;
   if(p.kind==1)p.t.position=p.home+Vector3.right*Mathf.Sin(simTime*.9f+p.phase)*1.1f;
   if(p.kind==3)p.t.rotation=Quaternion.Euler(0,Mathf.Sin(simTime*.7f+p.phase)*42,0);
   if(p.kind==2||p.kind==4){p.sink=Mathf.Clamp(p.sink+(on?dt*(p.kind==4?5:1): -dt*.7f),0,2.2f);p.t.position=p.home-Vector3.up*p.sink;}
   if(on){body.Move(p.t.position-old);if(p.kind==3){Vector3 offset=body.transform.position-p.t.position;body.Move((p.t.rotation*Quaternion.Inverse(oldRotation))*offset-offset);}}
  }
  if(grounded&&vertical<0)vertical=-2;
  if(coyote>0&&jumpBuffer>0){vertical=JumpSpeed;coyote=0;jumpBuffer=0;Sfx(jumpClip);}
  vertical-=22*dt;Vector3 v=body.transform.TransformDirection(new Vector3(move.x,0,move.y))*RunSpeed;
  body.Move((v+Vector3.up*vertical)*dt);
  bool inWater=body.transform.position.y<WaterY-.05f;
  if(inWater){roomSplashed=true;if(wet==0){Noise(body.transform.position,9);Sfx(splashClip);Ripple(body.transform.position);}wet+=dt;if(body.transform.position.y<WaterY-.12f){body.Move(Vector3.up*(WaterY-.12f-body.transform.position.y));vertical=Mathf.Max(vertical,0);}if(wet>(assist?7:5)&&stun<=0)Die();if(jumpBuffer>0){vertical=9.5f;jumpBuffer=0;Noise(body.transform.position,8);}}else wet=0;
  if(body.transform.position.y<-4)Die();
  if(mode!=Mode.Play)return;
  for(int i=0;i<steps.Count;i++){var p=steps[i];if(p.size.x<6)continue;if(body.isGrounded&&Mathf.Abs(body.transform.position.z-p.home.z)<1.8f&&Mathf.Abs(body.transform.position.x-p.home.x)<p.size.x*.5f&&p.home.z>checkpointZ){checkpointZ=p.home.z;spawn=new Vector3(body.transform.position.x,p.home.y+.55f,p.home.z);checkpointIndex=i;stones=Mathf.Max(stones,3);RewardCheckpoint();Sfx(clickClip);EmitFX(2,spawn);}}
  Valve nearest=null;float distance=2.5f;
  foreach(var valve in valves){float d=Vector3.Distance(body.transform.position+Vector3.up,valve.t.position);if(d<distance&&!valve.done){distance=d;nearest=valve;}}
  hold=0;
  if(nearest!=null){hold=nearest.charge/2.5f;if(use){nearest.charge+=dt;nearest.t.Rotate(0,dt*150,0,Space.Self);if(nearest.charge>=2.5f){nearest.done=true;nearest.t.GetComponent<Renderer>().sharedMaterial=glow;Sfx(clickClip);Notice("VALVE OPEN  /  pressure released");}}}
  foreach(var crusher in crushers){Vector3 cp=crusher.position;cp.y=3.7f+Mathf.Sin(simTime*1.2f+cp.z)*2.2f;crusher.position=cp;Vector3 d=body.transform.position-cp;if(Mathf.Abs(d.x)<1.9f&&Mathf.Abs(d.z)<.8f&&body.transform.position.y+1.7f>cp.y-1.5f&&body.transform.position.y<cp.y+1.5f)Die();} if(throwInput)Throw();if(flareInput)Flare();throwInput=flareInput=false;
  if(mode!=Mode.Play)return;
  TickCreature(dt);
  if(mode!=Mode.Play)return;
  for(int i=ripples.Count-1;i>=0;i--){var r=ripples[i];r.localScale+=new Vector3(dt*4,0,dt*4);if(r.localScale.x>9){Destroy(r.gameObject);ripples.RemoveAt(i);}}
  if(body.transform.position.z>EndZ-1){
   bool all=true;foreach(var valve in valves)all&=valve.done;
   if(!all)Notice("EXIT LOCKED  /  open both valves at the safe islands",1);
   else if(stage==8&&leg==2){codeOpen=true;mode=Mode.Pause;portal.StopPlay();CursorState(false);}
   else Finish();
  }
 }
 void Noise(Vector3 p,float seconds){RevealCreature(p);bool fresh=heard<=0||(sound-p).sqrMagnitude>9;sound=p;heard=seconds;if(fresh&&creature&&stun<=0)creature.Hear();}
 void Ripple(Vector3 p,bool splash=true){p.y=WaterY+.035f;if(splash)EmitFX(0,p);}
 void Throw(){
  if(cooldown>0)return;
  bool pebble=stones+reserveBottles<=0;
  if(!pebble){if(stones>0)stones--;else{reserveBottles--;SaveEconomy();}}
  cooldown=pebble?2:.8f;
  LaunchThrownItem(pebble);
 }
 void Flare(){
  if(flares+reserveFlares<=0||cooldown>0)return;
  if(flares>0)flares--;else{reserveFlares--;SaveEconomy();}
  cooldown=1;stun=5+flareLevel*.75f;
  Notice("FLARE  /  creature stunned for "+stun.ToString("0.0")+" seconds");Sfx(clickClip);EmitFX(1,monster.position+Vector3.up*1.4f);Ripple(monster.position,false);
 }
 void ReadInput(){
  move=look=Vector2.zero;use=false;var k=Keyboard.current;var m=Mouse.current;
  if(k!=null){move=new Vector2((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0),(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0));jump|=k.spaceKey.wasPressedThisFrame;use=k.eKey.isPressed;throwInput|=k.qKey.wasPressedThisFrame;flareInput|=k.fKey.wasPressedThisFrame;}
  if(m!=null&&Cursor.lockState==CursorLockMode.Locked)look=m.delta.ReadValue()*.085f;
  if(Touchscreen.current!=null)foreach(var t in Touchscreen.current.touches){
   int id=t.touchId.ReadValue();Vector2 p=t.position.ReadValue();Vector2 ui=new Vector2((p.x-Screen.safeArea.x)/uiScale,(Screen.safeArea.yMax-p.y)/uiScale);
   if(t.press.wasPressedThisFrame){
    if(TouchHit(JumpRect,ui))jump=true;
    else if(TouchHit(ThrowRect,ui))throwInput=true;
    else if(TouchHit(FlareRect,ui))flareInput=true;
    else if(TouchHit(UseRect,ui)){useFinger=id;use=true;}
    else if(ui.x<uw*.42f&&ui.y>145&&moveFinger<0){moveFinger=id;moveOrigin=p;}
    else if(ui.x>=uw*.42f&&ui.y>145&&lookFinger<0)lookFinger=id;
   }
   if(t.press.isPressed){if(id==moveFinger)move=Vector2.ClampMagnitude((p-moveOrigin)/(70*uiScale),1);if(id==lookFinger)look=t.delta.ReadValue()*.12f;if(id==useFinger&&TouchHit(UseRect,ui))use=true;}
   if(t.press.wasReleasedThisFrame){if(id==moveFinger)moveFinger=-1;if(id==lookFinger)lookFinger=-1;if(id==useFinger)useFinger=-1;}
  }
  move=Vector2.ClampMagnitude(move,1);
 }
 Rect JumpRect=>new Rect(uw-135,uh-128,96,96);
 Rect ThrowRect=>new Rect(uw-153,uh-350,132,132);
 Rect FlareRect=>new Rect(uw-245,uh-124,76,76);
 Rect UseRect=>new Rect(uw-245,uh-220,76,76);
 void OnApplicationFocus(bool focus){if(!focus)Pause();}
 void OnApplicationPause(bool pause){if(pause)Pause();}
 void OnDestroy(){AudioListener.pause=false;Cursor.lockState=CursorLockMode.None;}
 string TimeText(){int t=(int)elapsed;return (t/60).ToString("00")+":"+(t%60).ToString("00");}
}
}








