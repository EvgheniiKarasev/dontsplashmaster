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
  // Both imported skeletons use the same wrist axes. The sample's right root
  // offset is not the mirrored left grip pose; correct the visual, not XR input.
  var left=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/LeftHand.prefab").transform.Find("OpenXRLeftHand");
  var right=PrefabUtility.LoadPrefabContents(Folder+"/RightHand.prefab");
  try{
   var model=right.transform.Find("OpenXRRightHand");var q=left.localRotation;var p=left.localPosition;
   var rotation=new Quaternion(q.x,-q.y,-q.z,q.w);var position=new Vector3(-p.x,p.y,p.z);
   if(Quaternion.Angle(model.localRotation,rotation)>.001f||Vector3.Distance(model.localPosition,position)>.0001f){
    model.SetLocalPositionAndRotation(position,rotation);PrefabUtility.SaveAsPrefabAsset(right,Folder+"/RightHand.prefab");
   }
  }finally{PrefabUtility.UnloadPrefabContents(right);}
 }
}
}
