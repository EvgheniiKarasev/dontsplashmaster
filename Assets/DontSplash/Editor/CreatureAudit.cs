using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;
namespace DontSplash {
public static class CreatureAudit {


 [MenuItem("Dont Splash/Audit Demogorgon")]
 public static void Run(){
 const string path="Assets/demogorgon-rig/source/Demogorgon.fbx";
 var log=new StringBuilder();
 var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
 var bones=model.GetComponentsInChildren<Transform>();
 var lf=bones.First(t=>t.name=="joint_FootLT_01");var rf=bones.First(t=>t.name=="joint_FootRT_01");
 var preview=new PreviewRenderUtility();preview.AddSingleGO(model);
 preview.camera.fieldOfView=35;preview.camera.nearClipPlane=.1f;preview.camera.farClipPlane=1000;
 preview.lights[0].intensity=1.3f;preview.lights[0].transform.rotation=Quaternion.Euler(35,160,0);
 preview.lights[1].intensity=.8f;preview.ambientColor=new Color(.4f,.4f,.4f);
 foreach(var a in AssetDatabase.LoadAllAssetsAtPath(path)){
  if(!(a is AnimationClip c)||c.name.StartsWith("__preview__"))continue;
  string name=c.name.Substring(c.name.LastIndexOf('|')+1);
  AssetDatabase.TryGetGUIDAndLocalFileIdentifier(c,out string guid,out long id);
  log.AppendLine(name+" | "+c.length+" | "+id);
  if(name.StartsWith("Run")&&c.length<2){
   Vector3 step=Vector3.zero;
   for(int i=1;i<24;i++){
    c.SampleAnimation(model,c.length*(i-1)/24);Vector3 l=lf.position,r=rf.position;
    c.SampleAnimation(model,c.length*i/24);Vector3 dl=lf.position-l,dr=rf.position-r;
    step+=(lf.position.y<rf.position.y?dl:dr);
   }
   log.AppendLine("STANCE "+name+" "+step.ToString("F3"));
  }
  if(new[]{"Idle","IdleHunt","Search","Swing","Run","RunHunt","Run_8","RunHunt_8"}.Contains(name)){
   for(int frame=0;frame<3;frame++){
    c.SampleAnimation(model,c.length*(frame+.1f)/3);
    Bounds b=model.GetComponentsInChildren<Renderer>()[0].bounds;
    foreach(var r in model.GetComponentsInChildren<Renderer>())b.Encapsulate(r.bounds);
    preview.camera.transform.position=b.center+new Vector3(.25f,.05f,1)*b.size.y*2;
    preview.camera.transform.LookAt(b.center);
    preview.BeginStaticPreview(new Rect(0,0,400,400));
    var baked=new System.Collections.Generic.List<GameObject>();
    var meshes=new System.Collections.Generic.List<Mesh>();
    foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){
     var mesh=new Mesh();skin.BakeMesh(mesh);meshes.Add(mesh);log.AppendLine("BAKED "+name+" "+skin.name+" "+mesh.bounds+" scale "+skin.transform.lossyScale);

     for(int sub=0;sub<mesh.subMeshCount;sub++)preview.DrawMesh(mesh,Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one),skin.sharedMaterials[sub],sub);
     skin.enabled=false;
    }
    preview.camera.transform.position=b.center+new Vector3(.25f,.05f,1)*b.size.y*2;preview.camera.transform.LookAt(b.center);preview.Render(true);
    var texture=preview.EndStaticPreview();File.WriteAllBytes("Logs/Creature-"+name+"-"+frame+".png",texture.EncodeToPNG());Object.DestroyImmediate(texture);
    foreach(var go in baked)Object.DestroyImmediate(go);foreach(var mesh in meshes)Object.DestroyImmediate(mesh);
    foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())skin.enabled=true;
   }
  }
 }
 File.WriteAllText("Logs/CreatureAudit.txt",log.ToString());preview.Cleanup();VerifyPerformance();
 }
 static void VerifyPerformance(){
  var game=Object.FindFirstObjectByType<SplashGame>();
  if(!game||!game.monsterWalk||!game.monsterRun||!game.monsterReveal||!game.monsterAttack)throw new System.Exception("Missing creature clips.");
  var nav=new GameObject("Creature verification");
  var model=Object.Instantiate(game.monsterAsset,nav.transform);
  var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();Bounds bounds=skins[0].bounds;
  foreach(var skin in skins)bounds.Encapsulate(skin.bounds);
  model.transform.localScale*=3.4f/bounds.size.y;
  var perf=nav.AddComponent<MonsterPerformance>();perf.Initialize(model.transform,game.monsterAttack,game.monsterIdle,game.monsterRun,null,game.monsterWalk,game.monsterReveal);
  var preview=new PreviewRenderUtility();preview.AddSingleGO(nav);preview.camera.fieldOfView=64;
  preview.lights[0].intensity=1.3f;preview.lights[0].transform.rotation=Quaternion.Euler(35,160,0);preview.lights[1].intensity=.8f;
  preview.ambientColor=new Color(.4f,.4f,.4f);
  try{
   for(int frame=0;frame<4;frame++){
    perf.Tick(.1f,true,false,false,100,0);
    if(frame>0)perf.InspectVictim(frame/3f);
    if(frame==3){perf.BeginAttack();perf.Tick(.5f,true,true,false,100,0);}
    var face=perf.FacePosition;
    preview.BeginStaticPreview(new Rect(0,0,640,400));
    var meshes=new System.Collections.Generic.List<Mesh>();
    foreach(var skin in skins){var mesh=new Mesh();skin.BakeMesh(mesh);meshes.Add(mesh);skin.enabled=false;
     for(int sub=0;sub<mesh.subMeshCount;sub++)preview.DrawMesh(mesh,Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one),skin.sharedMaterials[sub],sub);
    }
    preview.camera.nearClipPlane=.05f;preview.camera.farClipPlane=100;
    preview.camera.transform.position=face+nav.transform.forward*2.5f+Vector3.up*.08f;preview.camera.transform.LookAt(face);
    preview.Render(true);var texture=preview.EndStaticPreview();File.WriteAllBytes("Logs/Creature-catch-"+frame+".png",texture.EncodeToPNG());Object.DestroyImmediate(texture);
    foreach(var mesh in meshes)Object.DestroyImmediate(mesh);foreach(var skin in skins)skin.enabled=true;
   }
   if(!perf.Tick(.19f,true,true,false,100,0))throw new System.Exception("Attack impact missing.");
   if(perf.Tick(.02f,true,true,false,100,0))throw new System.Exception("Attack impact repeated.");
   perf.ResetPose();perf.Tick(.1f,true,false,false,100,5);
  }finally{preview.Cleanup();}
  Debug.Log("Demogorgon verification: all five clips assigned; playable reveal and attack rendered.");
 }

}
}
