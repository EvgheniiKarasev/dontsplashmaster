using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DontSplash {
// Reuses the installed Meta assets and their controller-to-wrist offsets.
public static class QuestHands {
 const string Folder="Assets/Resources/VR";
 public static void Prepare(){
  if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Resources","VR");
  string source="Packages/com.meta.xr.sdk.interaction/Runtime/UnityXR/Prefabs/UnityXRControllerHands.prefab";
  var asset=AssetDatabase.LoadAssetAtPath<GameObject>(source);
  if(!asset)throw new Exception("Meta controller hands prefab is missing.");
  foreach(string side in new[]{"Left","Right"}){
   string path=Folder+"/"+side+"Hand.prefab";
   if(AssetDatabase.LoadAssetAtPath<GameObject>(path))continue;
   var model=asset.GetComponentsInChildren<Transform>(true).First(t=>t.name=="OpenXR"+side+"Hand");
   var root=new GameObject(side+" hand");
   try{
    var hand=UnityEngine.Object.Instantiate(model.gameObject,root.transform,false);
    hand.name=model.name;hand.SetActive(true);
    hand.transform.localPosition=model.localPosition;hand.transform.localRotation=model.localRotation;hand.transform.localScale=model.localScale;
    foreach(var script in hand.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
    var animator=hand.GetComponentInChildren<Animator>(true);
    if(!animator)throw new Exception("Meta hand animator missing: "+side);
    animator.enabled=true;animator.updateMode=AnimatorUpdateMode.UnscaledTime;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
    var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=side+" glove"};
    material.color=side=="Left"?new Color(.48f,.73f,.76f):new Color(.86f,.76f,.46f);
    material.SetFloat("_Smoothness",.25f);
    AssetDatabase.CreateAsset(material,Folder+"/"+side+"Glove.mat");
    foreach(var renderer in hand.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
     renderer.gameObject.SetActive(true);renderer.enabled=true;renderer.sharedMaterial=material;
     renderer.updateWhenOffscreen=true;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    PrefabUtility.SaveAsPrefabAsset(root,path);
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  // The sample offsets expect Meta's controller root, not a raw OpenXR grip.
  // Match FromUnityXRControllerDataSource's FlipZ / tip conversion before
  // applying each ORIGINAL hand offset. Mirroring the left offset is incorrect.
  foreach(string side in new[]{"Left","Right"}){
   var sourceHand=asset.GetComponentsInChildren<Transform>(true).First(t=>t.name=="OpenXR"+side+"Hand");
   var tip=side=="Left"?Quaternion.AngleAxis(90,Vector3.forward):Quaternion.AngleAxis(180,Vector3.right)*Quaternion.AngleAxis(-90,Vector3.forward);
   var q=Quaternion.Inverse(tip);var correction=new Quaternion(-q.x,-q.y,q.z,q.w);
   string path=Folder+"/"+side+"Hand.prefab";var root=PrefabUtility.LoadPrefabContents(path);
   try{
    var model=root.transform.Find("OpenXR"+side+"Hand");
    var rotation=correction*sourceHand.localRotation;var position=correction*sourceHand.localPosition;
    if(Quaternion.Angle(model.localRotation,rotation)>.001f||Vector3.Distance(model.localPosition,position)>.0001f){
     model.SetLocalPositionAndRotation(position,rotation);PrefabUtility.SaveAsPrefabAsset(root,path);
    }
   }finally{PrefabUtility.UnloadPrefabContents(root);}
  }
 }
}
}
