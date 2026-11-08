using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
namespace DontSplash {
public static partial class QuestSmoke {
 static void CheckAtmosphericFX(SplashGame game,StringBuilder report){
  Call(game,"Respawn");Call(game,"Resume");Set(game,"stage",0);Call(game,"ApplyAtmosphere");
  var camera=FeedbackCamera(Field<Camera>(game,"eye"));camera.fieldOfView=54;
  var canvas=Field<Canvas>(game,"uiCanvas");canvas.gameObject.SetActive(false);
  try{
   Check(Resources.Load<Texture2D>("FX/WaterRipple")&&Resources.Load<Texture2D>("FX/SoftSmoke"),"Generated FX textures missing");
   var shader=Shader.Find("DontSplash/Atmospheric Effects");Check(shader&&shader.isSupported,"Atmosphere shader missing/unsupported");
   var water=Field<Material>(game,"water");report.AppendLine("Water shader "+water.shader.name+" queue "+water.renderQueue+"; ripple texture "+Field<Material>(game,"rippleFX").mainTexture.name);
   foreach(int kind in new[]{0,1}){
    Vector3 point=kind==0?new Vector3(2.7f,.1f,8):new Vector3(0,.5f,7);
    Check(kind!=0||(bool)Call(game,"IsOpenWater",point),"FX review splash point is not open water");
    camera.transform.position=kind==0?new Vector3(3.4f,1.7f,5.3f):new Vector3(1.15f,1.35f,5.4f);
    camera.transform.LookAt(point+Vector3.up*(kind==0?.06f:.35f));
    // Billboards face this inspection camera while the camera itself is a real
    // Unity renderer with the scene's lighting, materials and fog.
    var eye=Field<Camera>(game,"eye");Quaternion original=eye.transform.rotation;eye.transform.rotation=camera.transform.rotation;
    try{
     Call(game,"ResetAtmosphericFX");Call(game,"EmitFX",kind,point);float previous=0;
     foreach(float time in new[]{.12f,.45f,1.0f,1.8f}){Call(game,"StepFX",time-previous);previous=time;Capture(camera,"Logs/FX-"+(kind==0?"water":"flare")+"-"+Mathf.RoundToInt(time*100)+".png");}
    }finally{eye.transform.rotation=original;}
   }
   Call(game,"ResetAtmosphericFX");Call(game,"EmitFX",0,new Vector3(2.7f,.1f,8));
   var effects=((IEnumerable)Field<object>(game,"bursts")).Cast<object>().ToArray();
   var active=effects.First(b=>Item<float>(b,"remaining")>0);float age=Item<float>(active,"age");
   Call(game,"Pause");Call(game,"StepFX",.5f);Check(Item<float>(active,"age")==age,"FX advance during pause");
   Call(game,"Resume");for(int i=0;i<100;i++){Call(game,"EmitFX",0,new Vector3(2.7f,.1f,8));Call(game,"EmitFX",1,new Vector3(0,.5f,7));}
   Check(((IEnumerable)Field<object>(game,"bursts")).Cast<object>().Count()==9,"Unbounded effect allocations");
   Call(game,"StepFX",3f);Check(effects.All(b=>!Item<GameObject>(b,"go").activeSelf),"Expired FX remain active");
   report.AppendLine("Generated atmospheric FX: 8 in-scene captures; pause freezes animation; 200 bursts reuse 9 fixed effects; all expire cleanly.");
   File.WriteAllText("Logs/Quest-fx-checks.txt","PASS\n"+report.ToString().Split('\n').LastOrDefault(s=>s.StartsWith("Generated atmospheric FX")));
  }finally{canvas.gameObject.SetActive(true);Object.DestroyImmediate(camera.gameObject);Call(game,"ResetAtmosphericFX");}
 }
}
}
