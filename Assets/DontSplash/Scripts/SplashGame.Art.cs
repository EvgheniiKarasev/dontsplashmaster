using UnityEngine;
using System.Collections.Generic;
namespace DontSplash {
public sealed partial class SplashGame {
 public GameObject[] dressingAssets;
 readonly List<TMPro.TMP_Text> roomLabels=new List<TMPro.TMP_Text>();
 float labelRefresh;
 void LateUpdate(){SyncUI();LateVR();UpdateTouchControls();TickFX();if(!eye)return;hitShake=Mathf.Max(0,hitShake-Time.unscaledDeltaTime);if(!vr&&mode!=Mode.Caught)eye.transform.localPosition=Vector3.up*1.6f+new Vector3(Mathf.Sin(Time.unscaledTime*65),Mathf.Sin(Time.unscaledTime*83),0)*hitShake*.16f;if(creature&&(SystemSuspended||(mode!=Mode.Play&&mode!=Mode.Caught)))creature.Tick(0,false,false,false,0);if(Time.unscaledTime<labelRefresh)return;labelRefresh=Time.unscaledTime+.1f;foreach(var text in roomLabels){if(!text)continue;float d=Vector3.Distance(eye.transform.position,text.transform.position);Color c=text.color;c.a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(RenderSettings.fogStartDistance,RenderSettings.fogEndDistance-2,d));if(Mathf.Abs(text.color.a-c.a)>.015f)text.color=c;}}
 readonly Dictionary<Material,Material> convertedProps=new Dictionary<Material,Material>();
 Material accent,concrete,metal; Light ceilingLight;
 readonly Color[] wallColors={
  new Color(.72f,.74f,.59f),new Color(.53f,.59f,.57f),new Color(.45f,.64f,.56f),
  new Color(.35f,.56f,.64f),new Color(.48f,.45f,.37f),new Color(.65f,.66f,.59f),
  new Color(.55f,.43f,.34f),new Color(.26f,.35f,.37f),new Color(.61f,.64f,.57f)};
 readonly Color[] accents={
  new Color(.28f,.68f,.54f),new Color(.9f,.64f,.29f),new Color(.32f,.76f,.63f),
  new Color(.25f,.67f,.9f),new Color(.87f,.56f,.2f),new Color(.65f,.71f,.4f),
  new Color(.96f,.3f,.14f),new Color(.3f,.65f,.66f),new Color(.5f,.94f,.75f)};
 float HallWidth=>84;
 float HallHeight=>stage==2?13:stage==4?11:stage==7?5.7f:8;
 void ApplyAtmosphere(){
  if(!accent){accent=Mat(Color.white);concrete=Mat(new Color(.19f,.24f,.23f));metal=Mat(new Color(.11f,.16f,.17f));}
  int theme=(stage*LegCount+leg)%wallColors.Length;
  var pixels=new Color[4096];int cell=theme%3==0?16:theme%3==1?8:32;
  for(int y=0;y<64;y++)for(int x=0;x<64;x++){
   int offset=theme%3==2&&(y/cell)%2==1?cell/2:0;
   bool seam=(x+offset)%cell==0||y%cell==0;
   pixels[y*64+x]=Color.white*(seam?.48f:(((x+offset)/cell+y/cell)%2==0?1f:.86f));
  }
  grid.SetPixels(pixels);grid.Apply();
  tile.color=wallColors[theme];accent.color=MovingIntroduction?new Color(.25f,.67f,.9f):accents[theme];glow.color=new Color(.8f,.74f,.43f);glow.SetColor("_EmissionColor",new Color(.55f,.47f,.22f)*.35f);
  RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
  RenderSettings.fogStartDistance=stage==7?5:12;
  RenderSettings.fogEndDistance=stage==7?22:stage==2?43:38;
  eye.farClipPlane=RenderSettings.fogEndDistance+8;
  Color mist=stage==7?new Color(.16f,.17f,.1f):new Color(.64f,.63f,.46f);
  RenderSettings.fogColor=mist;eye.backgroundColor=mist;if(water.HasProperty("_MistColor")){water.SetColor("_MistColor",mist);water.SetFloat("_FogNear",RenderSettings.fogStartDistance);water.SetFloat("_FogFar",RenderSettings.fogEndDistance);}
  RenderSettings.ambientLight=new Color(.65f,.6f,.38f)*(stage==7?.35f:.85f);
  if(!ceilingLight)ceilingLight=GameObject.Find("Ceiling bounce").GetComponent<Light>();
  ceilingLight.intensity=stage==7?.3f:.75f;ceilingLight.color=new Color(1,.94f,.72f);
  water.SetColor("_BaseColor",new Color(.07f,.30f,.12f,.98f));if(water.HasProperty("_ShallowColor"))water.SetColor("_ShallowColor",new Color(.28f,.52f,.24f,.88f));
 }
 float RouteX(int i){
  float phase=leg*.65f;
  // Each route stays within a jumpable lateral change; safe islands reset its rhythm.
  if(stage==1)return Mathf.Sin(i*.5f+phase)*3.2f;
  if(stage==3)return Mathf.Sin(i*.65f+phase)*2.6f;
  if(stage==4)return Mathf.Sin(i*.45f+phase)*3.4f;
  if(stage==7)return Mathf.Sin(i*.65f+phase)*1.5f;
  return Mathf.Sin(i*.72f+phase)*2.3f;
 }
 void BuildArchitecture(){
  float w=HallWidth,h=HallHeight;

  Shape("West wall",new Vector3(-w,h*.5f-1,EndZ/2),new Vector3(.6f,h+3,EndZ+20),tile);
  Shape("East wall",new Vector3(w,h*.5f-1,EndZ/2),new Vector3(.6f,h+3,EndZ+20),tile);
  Shape("Ceiling",new Vector3(0,h,EndZ/2),new Vector3(w*2,.5f,EndZ+20),tile);

  Shape("Far wall",new Vector3(0,h*.5f,EndZ+10),new Vector3(w*2,h+3,.5f),tile);
  for(int side=-1;side<=1;side+=2){
   Shape("Waterline band",new Vector3(side*(w-.34f),.2f,EndZ/2),new Vector3(.08f,.3f,EndZ+18),accent,false);
   Shape("Upper ceramic border",new Vector3(side*(w-.34f),3.1f,EndZ/2),new Vector3(.08f,.18f,EndZ+18),accent,false);
   Shape("Maintenance ledge",new Vector3(side*(w-1.4f),-1.6f,EndZ/2),new Vector3(2,.4f,EndZ+18),concrete);
  }
 }
 void BuildBay(int i,float z){
  float w=HallWidth,h=HallHeight;int bay=i/3;
  for(int side=-1;side<=1;side+=2){
   float x=side*(w-2.8f);
   if(stage==0||stage==5||stage==8){
    Shape("Arcade pier",new Vector3(x,h*.4f,z),new Vector3(.9f,h*.9f,1.1f),tile);
    Shape("Pier capital",new Vector3(x,h*.77f,z),new Vector3(1.5f,.4f,1.5f),accent);
    var arch=Shape("Arch shoulder",new Vector3(x-side*.7f,h*.82f,z),new Vector3(2,.5f,1.05f),tile);
    arch.transform.rotation=Quaternion.Euler(0,0,side*35);
    Shape("Arcade lintel",new Vector3(0,h*.93f,z),new Vector3(w*1.5f,.5f,1.05f),tile);
   }else if(stage==2){
    Shape("Reservoir drum",new Vector3(x,2.7f,z),new Vector3(2.8f,3.8f,2.8f),metal,true,PrimitiveType.Cylinder);
    Shape("Tank collar",new Vector3(x,4.2f,z),new Vector3(2.95f,.14f,2.95f),accent,false,PrimitiveType.Cylinder);
    Shape("Suspension",new Vector3(x,h*.65f,z),new Vector3(.15f,h*.65f,.15f),metal,false);
   }else if(stage==4||stage==6){
    Shape("Machine housing",new Vector3(x,1.6f,z),new Vector3(2.7f,4,3.6f),metal);
    Shape("Machine panel",new Vector3(x-side*1.37f,2,z),new Vector3(.06f,2,2.6f),accent,false);
    var pipe=Shape("Overhead conduit",new Vector3(0,h-1,z),new Vector3(.55f,w,.55f),brass,false,PrimitiveType.Cylinder);pipe.transform.rotation=Quaternion.Euler(0,0,90);
    Prop(4,new Vector3(x-side*1.5f,.9f,z-1.4f),.7f,side*90);
   }else if(stage==1){
    var column=Shape("Broken support",new Vector3(x,1.2f,z),new Vector3(1.3f,3.5f,1.2f),concrete);
    column.transform.rotation=Quaternion.Euler(0,0,side*(bay%2==0?14:-8));
    var rubble=Shape("Collapsed slab",new Vector3(x-side,.05f,z+1.4f),new Vector3(2.3f,.8f,1.6f),tile);
    rubble.transform.rotation=Quaternion.Euler(10,25*side,12);
   }else{
    Shape("Service buttress",new Vector3(x,h*.45f,z),new Vector3(.65f,h,1.1f),metal);
    if(i==0)Shape("Service conduit",new Vector3(side*(w-.7f),h-1,EndZ/2),new Vector3(.22f,.22f,EndZ+18),brass,false);
   }
   // Recessed window panels and utility shelves provide a human scale.
   if(bay%2==0){
    Shape("Recess",new Vector3(side*(w-.4f),2,z+3),new Vector3(.13f,2.2f,3),metal,false);
    Shape("Recess light",new Vector3(side*(w-.49f),3.05f,z+3),new Vector3(.08f,.1f,2.7f),glow,false);
   }
  }
  for(int lane=0;lane<9;lane+=2){
   float lampX=LaneX(lane);
   Shape("Pool light housing",new Vector3(lampX,h-.22f,z),new Vector3(2.6f,.18f,.8f),metal,false);
   Shape("Pool fluorescent",new Vector3(lampX,h-.33f,z),new Vector3(2.4f,.035f,.56f),glow,false);
  }
  Shape("Luminaire casing",new Vector3(0,h-.45f,z),new Vector3(stage==7?1:4,.25f,.55f),metal,false);
  Shape("Luminaire",new Vector3(0,h-.59f,z),new Vector3(stage==7?.8f:3.7f,.035f,.35f),stage==6?danger:glow,false);
  if(bay%2==0){
   Shape("Hanging sign back",new Vector3(0,h-2,z),new Vector3(3.7f,.85f,.15f),metal,false);
   Sign((stage+1).ToString("00")+"  /  SECTOR "+(bay+1),new Vector3(0,h-2,z-.09f),.45f);
  }
  if(stage==3&&bay%2==0){
   Shape("Pump block",new Vector3(w-3,.8f,z+3),new Vector3(3,2,3),accent);
   Prop(0,new Vector3(w-3,1.8f,z+3),1.5f,0);
  }
 }
 void DressCheckpoint(float z,int index){
  Shape("Service rack",new Vector3(-2.5f,.8f,z+.7f),new Vector3(1.2f,.15f,.6f),metal);
  Prop(1,new Vector3(-2.5f,.9f,z+.7f),.7f,180);
  Prop(2,new Vector3(-3,.46f,z-.5f),.45f,30);
  Shape("Checkpoint beacon",new Vector3(-3.25f,1,z+.8f),new Vector3(.12f,1.2f,.12f),glow,false);
  if(index==7)Prop(3,new Vector3(-2.5f,.45f,z-1),.7f,0);
 }
 void DressEntranceAndExit(){
  Shape("Exit bulkhead",new Vector3(0,2.2f,EndZ+4),new Vector3(6,4.4f,.5f),metal);
  Shape("Exit inset",new Vector3(0,2,EndZ+3.7f),new Vector3(4,3.7f,.1f),accent,false);
  Shape("Exit seam",new Vector3(0,2,EndZ+3.6f),new Vector3(.04f,3.7f,.03f),glow,false);
  Prop(4,new Vector3(2.4f,1.5f,EndZ+3.4f),.7f,180);
  Sign("SURFACE ACCESS",new Vector3(0,4.8f,EndZ+3.5f),.65f);
 }
 void Prop(int index,Vector3 p,float height,float yawAngle){
  if(dressingAssets==null||index>=dressingAssets.Length||!dressingAssets[index])return;
  var go=Instantiate(dressingAssets[index],room);go.name="Set dressing / "+dressingAssets[index].name;
  go.transform.position=Vector3.zero;go.transform.rotation=Quaternion.Euler(0,yawAngle,0);
  foreach(var c in go.GetComponentsInChildren<Collider>()){c.enabled=false;Destroy(c);}
  foreach(var light in go.GetComponentsInChildren<Light>())light.enabled=false;
  foreach(var behaviour in go.GetComponentsInChildren<MonoBehaviour>())behaviour.enabled=false;
  var rs=go.GetComponentsInChildren<Renderer>();if(rs.Length==0){Destroy(go);return;}
  Bounds bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
  if(bounds.size.y>.001f)go.transform.localScale*=height/bounds.size.y;
  bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
  go.transform.position=p-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
  foreach(var r in rs){
   var mats=r.sharedMaterials;
   for(int j=0;j<mats.Length;j++){
    var source=mats[j];if(!source)continue;
    if(!convertedProps.TryGetValue(source,out var replacement)){
     replacement=Mat(source.HasProperty("_Color")?source.GetColor("_Color"):Color.white);
     if(source.HasProperty("_MainTex"))replacement.mainTexture=source.GetTexture("_MainTex");
     replacement.SetFloat("_Smoothness",.25f);convertedProps[source]=replacement;
    }
    mats[j]=replacement;
   }
   r.sharedMaterials=mats;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  }
 }
}
}






