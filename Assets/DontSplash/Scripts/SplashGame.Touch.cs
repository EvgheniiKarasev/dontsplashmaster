using UnityEngine;
using TMPro;
namespace DontSplash {
public sealed partial class SplashGame {
 RectTransform stickBase,stickKnob;
 static bool TouchHit(Rect rect,Vector2 point)=>(point-rect.center).sqrMagnitude<=rect.width*rect.width*.25f;
 GameObject RoundControl(Rect rect,string caption,bool arrow=false){
  var go=new GameObject(arrow?"Jump":"Touch / "+caption,typeof(RectTransform),typeof(TouchDisc));
  Position(go,rect);var disc=go.GetComponent<TouchDisc>();disc.color=new Color(.12f,.13f,.10f,.48f);disc.arrow=arrow;disc.raycastTarget=false;
  if(!string.IsNullOrEmpty(caption)){
   var label=UILabel(rect,caption,caption=="THROW"?23:16,true);
   label.alignment=TextAlignmentOptions.Center;label.transform.SetParent(go.transform,true);
  }
  return go;
 }
 void BuildTouchControls(){
  RoundControl(JumpRect,"",true);RoundControl(ThrowRect,"THROW");
  RoundControl(FlareRect,"FLARE");RoundControl(UseRect,"USE");
  stickBase=RoundControl(new Rect(0,0,140,140),"").GetComponent<RectTransform>();
  stickKnob=RoundControl(new Rect(0,0,58,58),"").GetComponent<RectTransform>();
  stickKnob.SetParent(stickBase,false);stickKnob.anchorMin=stickKnob.anchorMax=new Vector2(.5f,.5f);stickKnob.pivot=new Vector2(.5f,.5f);
  stickBase.gameObject.SetActive(false);
 }
 void UpdateTouchControls(){
  if(!stickBase)return;
  bool active=touchMode&&mode==Mode.Play&&moveFinger>=0;
  stickBase.gameObject.SetActive(active);if(!active)return;
  Vector2 origin=new Vector2((moveOrigin.x-Screen.safeArea.x)/uiScale,(Screen.safeArea.yMax-moveOrigin.y)/uiScale);
  stickBase.anchoredPosition=new Vector2(origin.x-70,-origin.y+70);
  stickKnob.anchoredPosition=move*42;
 }
}
}
