using UnityEngine;
using System.Collections.Generic;
namespace DontSplash {
public sealed partial class SplashGame {
 // Keep serialized compatibility; replace saturated prefab bursts with pooled effects.
 public GameObject[] fxAssets;public Material fxMaterial;
 sealed class Burst {
  public GameObject go;public int kind;public float age,remaining;
  public MeshRenderer[] sprites;public MeshRenderer crown;public Mesh crownMesh;public Light light;
  public readonly Vector3[] vertices=new Vector3[195];public readonly Color[] colors=new Color[195];
 }
 readonly List<Burst> bursts=new List<Burst>();
 readonly List<Material> atmosphereFXMaterials=new List<Material>();
 Mesh fxQuad;Material rippleFX,smokeFX,softFX;MaterialPropertyBlock fxBlock;
 Material EffectMaterial(Texture texture,bool soft){
  var m=new Material(Shader.Find("DontSplash/Atmospheric Effects")){mainTexture=texture,renderQueue=3020};
  m.SetFloat("_Soft",soft?1:0);atmosphereFXMaterials.Add(m);return m;
 }
 MeshRenderer EffectSprite(Transform parent,string name,Material material){
  var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=fxQuad;
  var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;return r;
 }
 void InitializeFX(){
  fxBlock=new MaterialPropertyBlock();
  fxQuad=new Mesh{name="Atmosphere quad"};
  fxQuad.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(.5f,-.5f),new Vector3(.5f,.5f),new Vector3(-.5f,.5f)};
  fxQuad.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};fxQuad.triangles=new[]{0,2,1,0,3,2};fxQuad.colors=new[]{Color.white,Color.white,Color.white,Color.white};fxQuad.RecalculateBounds();
  rippleFX=EffectMaterial(Resources.Load<Texture2D>("FX/WaterRipple"),false);smokeFX=EffectMaterial(Resources.Load<Texture2D>("FX/SoftSmoke"),false);softFX=EffectMaterial(Texture2D.whiteTexture,true);
  for(int kind=0;kind<3;kind++)for(int copy=0;copy<(kind==0?4:kind==1?3:2);copy++){
   var b=new Burst{kind=kind,go=new GameObject(kind==0?"Pool surface splash":kind==1?"Smoky flare burst":"Quiet reward motes")};b.go.transform.SetParent(transform,false);
   b.sprites=new MeshRenderer[kind==0?11:kind==1?22:5];
   for(int i=0;i<b.sprites.Length;i++)b.sprites[i]=EffectSprite(b.go.transform,"FX layer "+i,kind==0&&i<3?rippleFX:kind==1&&i<5?smokeFX:softFX);
   if(kind==0){
    b.crown=EffectSprite(b.go.transform,"Low water crown",softFX);b.crownMesh=new Mesh{name="Water crown"};b.crownMesh.MarkDynamic();
    var triangles=new int[768];int at=0;
    for(int s=0;s<64;s++)for(int row=0;row<2;row++){int a=s*3+row,c=(s+1)*3+row;triangles[at++]=a;triangles[at++]=c;triangles[at++]=a+1;triangles[at++]=a+1;triangles[at++]=c;triangles[at++]=c+1;}
    b.crownMesh.vertices=b.vertices;b.crownMesh.triangles=triangles;b.crownMesh.uv=System.Array.ConvertAll(b.vertices,v=>Vector2.one*.5f);b.crown.GetComponent<MeshFilter>().sharedMesh=b.crownMesh;
   }
   if(kind==1){
    b.light=new GameObject("Burst light").AddComponent<Light>();b.light.transform.SetParent(b.go.transform,false);
    b.light.transform.localPosition=Vector3.up*.35f;b.light.color=new Color(1,.57f,.25f);b.light.range=5.5f;b.light.shadows=LightShadows.None;
   }
   b.go.SetActive(false);bursts.Add(b);
  }
 }
 void EmitFX(int kind,Vector3 point){
  Burst selected=null;foreach(var b in bursts)if(b.kind==kind&&(selected==null||b.remaining<selected.remaining))selected=b;
  if(selected==null)return;selected.age=0;selected.remaining=kind==0?2.8f:kind==1?2.8f:1.2f;
  if(kind==0)point.y=WaterY+.035f;
  selected.go.transform.position=point;selected.go.SetActive(true);DrawAtmosphericFX(selected);
 }
 void SpritePose(MeshRenderer r,Vector3 local,Vector2 size,Color tint,bool horizontal=false,float roll=0){
  r.enabled=tint.a>.001f;if(!r.enabled)return;r.transform.localPosition=local;
  r.transform.rotation=horizontal?Quaternion.Euler(90,roll,0):eye.transform.rotation*Quaternion.Euler(0,0,roll);
  r.transform.localScale=new Vector3(size.x,size.y,1);fxBlock.Clear();fxBlock.SetColor("_Tint",tint);r.SetPropertyBlock(fxBlock);
 }
 void DrawAtmosphericFX(Burst b){
  float t=b.age;
  if(b.kind==0){
   for(int ring=0;ring<3;ring++){
    float u=t-ring*.23f,fade=Mathf.Clamp01(u/.09f)*Mathf.Pow(Mathf.Clamp01(1-u/2.3f),1.1f),size=.3f+Mathf.Max(0,u)*1.7f;
    SpritePose(b.sprites[ring],Vector3.up*(ring*.003f),Vector2.one*size,new Color(1f,.99f,.83f,fade*(ring==0?.95f:ring==1?.75f:.52f)),true,ring*73);
   }
   float pulse=Mathf.Sin(Mathf.Clamp01(t/.52f)*Mathf.PI),radius=.11f+t*.48f;b.crown.enabled=t<.52f;
   if(b.crown.enabled){
    for(int s=0;s<=64;s++){float a=s/64f*Mathf.PI*2,lobe=.7f+.2f*Mathf.Sin(a*5+1)+.1f*Mathf.Sin(a*3);
     for(int row=0;row<3;row++){float r=radius+(row-1)*.045f;int i=s*3+row;b.vertices[i]=new Vector3(Mathf.Cos(a)*r,row==1?pulse*.17f*lobe:0,Mathf.Sin(a)*r);b.colors[i]=new Color(.42f,.61f,.49f,(row==1?.45f:0)*pulse);}
    }
    b.crownMesh.vertices=b.vertices;b.crownMesh.colors=b.colors;b.crownMesh.RecalculateBounds();
   }
   for(int i=3;i<11;i++){
    float a=i*2.399f,speed=.28f+(i%3)*.12f,y=(1+(i%3)*.18f)*t-3.5f*t*t;
    var p=new Vector3(Mathf.Cos(a)*(.08f+t*speed),Mathf.Max(0,y),Mathf.Sin(a)*(.08f+t*speed));
    SpritePose(b.sprites[i],p,new Vector2(.016f,.035f),new Color(.46f,.62f,.53f,y>0&&t<.5f?.5f*Mathf.Clamp01(1-t/.5f):0),false,i*31);
   }
  }else if(b.kind==1){
   for(int i=0;i<4;i++){
    float u=Mathf.Max(0,t-i*.045f),size=.32f+Mathf.Sqrt(u)*(.85f+i*.1f),alpha=Mathf.Clamp01(u/.14f)*Mathf.Pow(Mathf.Clamp01(1-u/2.7f),1.7f)*.4f;
    var p=new Vector3(Mathf.Sin(i*2.4f)*(.12f+u*.2f)+u*.12f,.16f+u*(.32f+i*.065f),Mathf.Cos(i*2.4f)*(.12f+u*.18f));
    Color c=Color.Lerp(new Color(.52f,.32f,.16f),new Color(.25f,.29f,.25f),Mathf.Clamp01(u*3));c.a=alpha;SpritePose(b.sprites[i],p,Vector2.one*size,c,false,i*91+u*12);
   }
   float glow=Mathf.Pow(Mathf.Clamp01(1-t/.7f),1.5f);
   SpritePose(b.sprites[4],Vector3.up*.24f,Vector2.one*(.8f+t*1.6f),new Color(2.4f,.68f,.13f,glow),false,t*25);
   SpritePose(b.sprites[21],Vector3.up*.22f,Vector2.one*(.32f+t*.25f),new Color(3f,1.4f,.55f,glow));
   for(int i=5;i<21;i++){
    float a=i*2.399f,speed=1.8f+(i%5)*.35f,y=(1.1f+(i%4)*.3f)*t-2.5f*t*t,fade=Mathf.Clamp01(1-t/(1.1f+(i%3)*.18f));
    SpritePose(b.sprites[i],new Vector3(Mathf.Cos(a)*speed*t,Mathf.Max(.025f,y),Mathf.Sin(a)*speed*t),new Vector2(.018f,.12f),new Color(1.6f,.65f,.17f,fade*.9f),false,a*Mathf.Rad2Deg);
   }
   b.light.intensity=glow*4.5f;b.light.enabled=glow>.005f;
  }else for(int i=0;i<b.sprites.Length;i++){float a=i*2.4f;SpritePose(b.sprites[i],new Vector3(Mathf.Sin(a)*.12f,t*.3f,Mathf.Cos(a)*.12f),Vector2.one*.025f,new Color(.65f,.6f,.32f,Mathf.Clamp01(1-t/1.2f)*.4f));}
 }
 void TickFX()=>StepFX(Time.deltaTime);
 void UpdateFlareBurn(FlyingFlare flare){
  float flicker=.85f+.1f*Mathf.Sin(Time.time*17)+.05f*Mathf.Sin(Time.time*29);
  SpritePose(flare.ember,Vector3.up*.28f,Vector2.one*.22f,new Color(1.7f,.7f,.17f,.55f*flicker));
  SpritePose(flare.smoke,Vector3.up*.42f,new Vector2(.18f,.28f),new Color(.29f,.3f,.25f,.16f),false,Time.time*9);
 }
 void StepFX(float dt){
  if(SystemSuspended||mode!=Mode.Play)return;
  foreach(var b in bursts){if(b.remaining<=0)continue;b.age+=dt;b.remaining-=dt;if(b.remaining<=0){b.go.SetActive(false);continue;}DrawAtmosphericFX(b);}
 }
 void ResetAtmosphericFX(){foreach(var b in bursts){b.remaining=0;b.go.SetActive(false);}}
 void DisposeAtmosphericFX(){foreach(var b in bursts)if(b.crownMesh)Destroy(b.crownMesh);foreach(var m in atmosphereFXMaterials)Destroy(m);if(fxQuad)Destroy(fxQuad);}
}
}
