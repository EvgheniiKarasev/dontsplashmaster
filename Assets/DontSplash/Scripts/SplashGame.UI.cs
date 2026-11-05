using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using System;
namespace DontSplash {
public sealed partial class SplashGame {
 public GameObject uiButtonPrefab,uiPanelPrefab,uiTitlePrefab;
 public TMP_FontAsset uiBodyFont,uiHeadingFont;
 TMP_Text pebbleCount,flareCount;RawImage flareIcon;
 GameObject shopControl;
 Image deathBlackout;Canvas uiCanvas;CanvasScaler uiScaler;RectTransform uiRoot;
 string uiKey="";TMP_Text hudTitle,hudStats,noticeLabel,inventoryLabel,codeLabel,assistLabel,soundLabel;
 GameObject noticePanel,dangerPanel;Image holdFill;Button primaryButton;Rect lastSafe;
 readonly Color uiMint=new Color(.83f,.78f,.51f),uiInk=new Color(.12f,.11f,.065f);
 void InitializeUI(){
  var go=new GameObject("Dont Splash / Layer Lab UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
  uiCanvas=go.GetComponent<Canvas>();uiCanvas.renderMode=RenderMode.ScreenSpaceOverlay;uiCanvas.sortingOrder=50;
  uiScaler=go.GetComponent<CanvasScaler>();uiScaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
  if(!FindFirstObjectByType<EventSystem>())new GameObject("UI Input",typeof(EventSystem),typeof(InputSystemUIInputModule));
  var blackout=new GameObject("Death fade",typeof(RectTransform),typeof(Image));
  var rect=blackout.GetComponent<RectTransform>();rect.SetParent(uiCanvas.transform,false);
  rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
  deathBlackout=blackout.GetComponent<Image>();deathBlackout.color=Color.clear;deathBlackout.raycastTarget=false;
  ConfigureVRCanvas();SyncUI();
 }
 RectTransform Position(GameObject go,Rect r,Transform parent=null){
  var rt=go.GetComponent<RectTransform>();if(!rt)rt=go.AddComponent<RectTransform>();
  rt.SetParent(parent?parent:uiRoot,false);rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);
  rt.anchoredPosition=new Vector2(r.x,-r.y);rt.sizeDelta=r.size;rt.localScale=Vector3.one;return rt;
 }
 GameObject Prefab(GameObject asset,Rect r,string name){
  var go=asset?Instantiate(asset):new GameObject(name,typeof(RectTransform),typeof(Image));go.name=name;Position(go,r);
  foreach(var text in go.GetComponentsInChildren<TMP_Text>(true)){text.font=uiBodyFont;text.raycastTarget=false;}
  foreach(var image in go.GetComponentsInChildren<Image>(true))image.raycastTarget=false;
  foreach(var animator in go.GetComponentsInChildren<Animator>(true))animator.enabled=false;
  return go;
 }
 GameObject UIPanel(Rect r,Color tint){
  var go=Prefab(uiPanelPrefab,r,"Layer Lab panel");foreach(var text in go.GetComponentsInChildren<TMP_Text>(true))text.gameObject.SetActive(false);
  var img=go.GetComponent<Image>();if(img)img.color=tint;return go;
 }
 TMP_Text UILabel(Rect r,string text,float size=21,bool heading=false){
  var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));Position(go,r);
  var label=go.GetComponent<TextMeshProUGUI>();label.font=heading?uiHeadingFont:uiBodyFont;label.fontSize=size;label.color=Color.white;label.text=text;
  label.alignment=TextAlignmentOptions.MidlineLeft;label.raycastTarget=false;label.enableAutoSizing=true;label.fontSizeMin=size*.7f;label.fontSizeMax=size;
  return label;
 }
 Button UIButton(Rect r,string text,Action action,out TMP_Text caption){
  var go=Prefab(uiButtonPrefab,r,"Layer Lab button / "+text);
  foreach(var txt in go.GetComponentsInChildren<TMP_Text>(true))txt.gameObject.SetActive(false);
  // The source art has 195 px of vertical borders. Scale borders rather than
  // squeezing the original 195 px-high face into a 50 px-high button.
  var img=go.GetComponent<Image>();
  foreach(var decoration in go.GetComponentsInChildren<Image>(true))if(decoration!=img)decoration.enabled=false;
  if(img){img.type=Image.Type.Sliced;img.pixelsPerUnitMultiplier=4.5f;img.color=uiMint;img.raycastTarget=action!=null;}
  var button=go.GetComponent<Button>();if(!button)button=go.AddComponent<Button>();button.onClick.RemoveAllListeners();button.targetGraphic=img;
  button.navigation=new Navigation{mode=Navigation.Mode.None};
  var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.08f,1.08f,1.08f);
  colors.pressedColor=new Color(.78f,.78f,.78f);colors.disabledColor=new Color(.5f,.5f,.5f);colors.fadeDuration=.1f;button.colors=colors;
  button.enabled=action!=null;
  if(action!=null)button.onClick.AddListener(()=>{if(SystemSuspended)return;Sfx(clickClip);action();});
  caption=UILabel(new Rect(r.x+12,r.y+4,r.width-24,r.height-8),text,21);caption.alignment=TextAlignmentOptions.Center;caption.color=uiInk;
  caption.transform.SetParent(go.transform,true);
  caption.textWrappingMode=TextWrappingModes.NoWrap;caption.overflowMode=TextOverflowModes.Ellipsis;
  // Explicit insets keep text clear of the preserved sprite borders.
  Position(caption.gameObject,new Rect(14,3,r.width-28,r.height-6),go.transform);
  return button;
 }
 Button UIButton(Rect r,string text,Action action){TMP_Text ignored;return UIButton(r,text,action,out ignored);}
 void Heading(Rect r,string text){
  var go=Prefab(uiTitlePrefab,r,"Layer Lab title");
  foreach(var label in go.GetComponentsInChildren<TMP_Text>(true))label.gameObject.SetActive(false);
  var caption=UILabel(new Rect(r.x+20,r.y+3,r.width-40,r.height-6),text,38,true);caption.alignment=TextAlignmentOptions.Center;caption.color=uiMint;
 }
 void ContinueUI(){
  if(mode==Mode.Win){StartRun(false);return;}
  if(mode==Mode.Dead){ContinueGame(()=>{stones=Mathf.Max(stones,5);flares=Mathf.Max(flares,2);Respawn();mode=Mode.Play;CursorState(true);});return;}
  if(mode==Mode.Between&&FirstGiftPending)return;
  if(mode==Mode.Between){ContinueGame(()=>{BuildRoom();mode=Mode.Play;CursorState(true);});return;}
  Resume();
 }
 float nextUIRefresh; Mode lastUIMode;
 void SyncUI(){
  if(mode==lastUIMode&&Time.unscaledTime<nextUIRefresh&&mode!=Mode.Caught)return;
  lastUIMode=mode;nextUIRefresh=Time.unscaledTime+.1f;
  if(!uiCanvas)return;
  // Covers the whole display, including outside the safe area; menus render above it.
  if(deathBlackout)deathBlackout.color=new Color(0,0,0,mode==Mode.Dead?1:mode==Mode.Caught?CatchBlackout:0);
  Rect safe=vr?new Rect(0,0,1000,650):Screen.safeArea;uiScale=Mathf.Max(.1f,Mathf.Min(safe.width/1000f,safe.height/650f));uw=safe.width/uiScale;uh=safe.height/uiScale;
  string key=mode+"/"+codeOpen+"/"+touchMode+"/"+Screen.width+"/"+Screen.height+"/"+safe;
  if(key!=uiKey){
   uiKey=key;lastSafe=safe;uiScaler.scaleFactor=uiScale;
   if(uiRoot){uiRoot.gameObject.SetActive(false);Destroy(uiRoot.gameObject);}
   var root=new GameObject("Safe area",typeof(RectTransform));uiRoot=root.GetComponent<RectTransform>();
   Position(root,new Rect(safe.x/uiScale,(vr?0:Screen.height-safe.yMax)/uiScale,uw,uh),uiCanvas.transform);
   pebbleCount=flareCount=null;flareIcon=null;
   coinLabel=hudTitle=hudStats=noticeLabel=inventoryLabel=codeLabel=assistLabel=soundLabel=null;noticePanel=dangerPanel=null;holdFill=null;primaryButton=null;
   if(mode==Mode.Play)BuildHUD();else if(mode==Mode.Shop)BuildShop();else if(mode!=Mode.Caught)BuildMenu();RefreshVRUI();
  }
  if(shopControl)shopControl.SetActive(!vr&&mode==Mode.Play&&NearShop);
  if(coinLabel)coinLabel.text="COINS  "+coins;

  if(hudStats)hudStats.text="<mspace=0.65em>"+TimeText()+"</mspace>";
  if(pebbleCount)pebbleCount.text=stones+reserveBottles>0?(stones+reserveBottles).ToString():"∞";
  if(flareCount)flareCount.text=(flares+reserveFlares).ToString();
  if(flareIcon)flareIcon.color=flares+reserveFlares>0?Color.white:new Color(.45f,.45f,.45f,.65f);
  if(noticePanel)noticePanel.SetActive(noticeTime>0||JumpHelpVisible||(vr&&NearShop));if(noticeLabel){noticeLabel.gameObject.SetActive(noticeTime>0||JumpHelpVisible||(vr&&NearShop));noticeLabel.text=vr&&NearShop?"HOLD LEFT GRIP TO OPEN SHOP":JumpHelpVisible?(vr?"Jump onto the first platform. Press A.":touchMode?"Jump onto the first platform. Tap the jump button.":"Jump onto the first platform. Press SPACE.") : notice;}
  if(dangerPanel)dangerPanel.SetActive(wet>0);
  if(holdFill){holdFill.rectTransform.sizeDelta=new Vector2(Mathf.Clamp01(hold)*196,10);holdFill.transform.parent.gameObject.SetActive(hold>0);}
  if(codeLabel)codeLabel.text=code.PadRight(4,'_');
  if(assistLabel)assistLabel.text=assist?"ASSIST: ON":"ASSIST: OFF";
  if(soundLabel)soundLabel.text=muted?"SOUND: OFF":"SOUND: ON";


 }
 void BuildMenu(){
  UIPanel(new Rect(0,0,uw,uh),new Color(.055f,.05f,.025f,.88f));
  float x=uw/2-290,y=uh/2-275;
  UIPanel(new Rect(x-25,y-15,630,580),new Color(.2f,.19f,.12f,.97f));
  Heading(new Rect(x,y+8,580,85),mode==Mode.Menu?"DONT SPLASH":codeOpen?"EMERGENCY EXIT":mode==Mode.Win?"SURFACE REACHED":mode==Mode.Dead?"IT HEARD YOU":mode==Mode.Between&&FirstGiftPending?"FIRST CROSSING!":mode==Mode.Between?"SECTOR CLEAR":"PAUSED");
  if(mode==Mode.Menu){
   UILabel(new Rect(x+20,y+145,540,vr?120:60),vr?"LEFT STICK: move   RIGHT STICK: snap turn\nA: jump   B: pause   X: bring menu here\nHOLD TRIGGER: right bottle / left flare\nSwing + release to throw   LEFT GRIP: use":"Keep the water quiet.",vr?20:26).alignment=TextAlignmentOptions.Center;
   UIButton(new Rect(x+110,y+410,360,65),"PLAY",()=>StartRun(false));
  }else if(mode==Mode.Between&&FirstGiftPending){
   UILabel(new Rect(x+20,y+120,540,85),"Choose a free permanent upgrade.",25).alignment=TextAlignmentOptions.Center;
   UIButton(new Rect(x+20,y+245,260,75),jumpLevel<3?"HIGHER JUMP":"2 EXTRA FLARES",()=>ClaimFirstGift(true));
   UIButton(new Rect(x+300,y+245,260,75),speedLevel<3?"FASTER RUN":"2 EXTRA FLARES",()=>ClaimFirstGift(false));
   UILabel(new Rect(x+20,y+345,540,90),"Next: moving platforms.\nYour choice starts the next chamber.",22).alignment=TextAlignmentOptions.Center;
  }else if(codeOpen){
   UILabel(new Rect(x+20,y+108,540,48),"Emergency code engraved in this chamber: 4173",20);
   codeLabel=UILabel(new Rect(x+20,y+162,540,55),"____",40,true);codeLabel.alignment=TextAlignmentOptions.Center;
   for(int i=0;i<10;i++){int digit=i;UIButton(new Rect(x+20+(i%5)*108,y+240+(i/5)*70,98,60),i.ToString(),()=>{if(code.Length<4)code+=digit;});}
   UIButton(new Rect(x+20,y+400,260,60),"CLEAR",()=>code="");
   UIButton(new Rect(x+300,y+400,260,60),"UNLOCK",()=>{if(code=="4173"){codeOpen=false;Finish();}else{code="";codeLabel.color=new Color(1,.4f,.25f);}});
  }else{
   string text=mode==Mode.Between?"Progress saved. Next:\n"+names[stage]+" / "+(leg+1)+" of 3"+"\nReward: "+lastRoomReward+" coins  /  Wallet: "+coins:mode==Mode.Dead?AttemptSummary():mode==Mode.Win?"You escaped all 27 chambers.\nTime: "+TimeText()+" / Falls: "+deaths:"Progress: "+completed+" / 27 chambers\nTime: "+TimeText()+" / Falls: "+deaths;
   UILabel(new Rect(x+20,y+120,540,115),text,24).alignment=TextAlignmentOptions.Center;
   primaryButton=UIButton(new Rect(x+(mode==Mode.Dead?70:110),y+280,mode==Mode.Dead?440:360,mode==Mode.Dead?82:70),mode==Mode.Win?"PLAY AGAIN":mode==Mode.Dead?"RETRY":mode==Mode.Between?"NEXT CHAMBER":"RESUME",ContinueUI);
   if(mode==Mode.Dead){
    var face=primaryButton.targetGraphic;
    if(face)face.color=new Color(1f,.86f,.38f);
    var outline=primaryButton.gameObject.AddComponent<Outline>();
    outline.effectColor=new Color(1f,.89f,.48f,.65f);outline.effectDistance=new Vector2(2,-2);
    var lobby=UIButton(new Rect(x+20,y+465,260,56),"LOBBY",VisitLobby);
    if(lobby.targetGraphic)lobby.targetGraphic.color=new Color(.56f,.55f,.39f);
   }else if(mode==Mode.Pause&&!codeOpen)UIButton(new Rect(x+130,y+490,320,56),"LOBBY",VisitLobby);
   if(mode!=Mode.Dead)UIButton(new Rect(x+20,y+420,260,60),"SOUND",ToggleSound,out soundLabel);
   var mainMenu=UIButton(new Rect(x+300,y+(mode==Mode.Dead?465:420),260,mode==Mode.Dead?56:60),"MAIN MENU",()=>{if(mode!=Mode.Win)Save();mode=Mode.Menu;AudioListener.pause=false;ambience.Stop();});
   if(mode==Mode.Dead&&mainMenu.targetGraphic)mainMenu.targetGraphic.color=new Color(.56f,.55f,.39f);
  }
 }
 void BuildInventory(){
  BuildItemSlot(ThrowRect,"Pebbles",vr?"RT":"Q",out pebbleCount);
  flareIcon=BuildItemSlot(FlareRect,"Flare",vr?"LT":"F",out flareCount);
  if(touchMode){
   var hint=UILabel(new Rect(uw/2-110,uh-142,220,28),"Tap item",20);
   hint.alignment=TextAlignmentOptions.Center;
  }
 }
 RawImage BuildItemSlot(Rect rect,string asset,string key,out TMP_Text count){
  var panel=UIPanel(rect,new Color(.16f,.15f,.085f,.78f));
  panel.name=asset+" inventory slot";
  var icon=new GameObject(asset,typeof(RectTransform),typeof(RawImage));
  Position(icon,new Rect(8,8,rect.width-16,rect.height-16),panel.transform);
  var graphic=icon.GetComponent<RawImage>();graphic.texture=Resources.Load<Texture2D>("UI/"+asset);graphic.raycastTarget=false;
  count=UILabel(new Rect(rect.x+rect.width-(vr?50:35),rect.y+rect.height-(vr?40:29),vr?44:29,vr?36:25),"",vr?30:20,true);
  count.alignment=TextAlignmentOptions.BottomRight;
  if(!touchMode){
   var badge=UIPanel(new Rect(rect.x+4,rect.y+4,26,26),uiMint);
   var caption=UILabel(new Rect(rect.x+4,rect.y+4,26,26),key,18,true);
   caption.color=uiInk;caption.alignment=TextAlignmentOptions.Center;
  }
  return graphic;
 }
 void ToggleSound(){muted=!muted;AudioListener.volume=muted?0:1;}
 void BuildHUD(){
  if(!vr){
  hudStats=UILabel(new Rect(28,22,160,46),"",32,true);
  hudStats.alignment=TextAlignmentOptions.Center;hudStats.color=new Color(1f,.94f,.74f);
  var timerShadow=hudStats.gameObject.AddComponent<Shadow>();
  timerShadow.effectColor=new Color(.04f,.05f,.025f,.9f);timerShadow.effectDistance=new Vector2(2,-2);
  var settings=UIButton(new Rect(uw-80,22,60,60),"",Pause);
  settings.gameObject.name="Settings";settings.gameObject.SetActive(!vr);
  var icon=new GameObject("Settings gear",typeof(RectTransform),typeof(RawImage));
  Position(icon,new Rect(8,8,44,44),settings.transform);
  var graphic=icon.GetComponent<RawImage>();
  graphic.texture=Resources.Load<Texture2D>("UI/SettingsGear");
  graphic.raycastTarget=false;
  coinLabel=UILabel(new Rect(uw-355,25,220,40),"",23,true);
  shopControl=UIButton(new Rect(uw-210,87,190,48),"SHOP",()=>OpenShop()).gameObject;shopControl.SetActive(NearShop);
  if(!vr){var cross=UILabel(new Rect(uw/2-12,uh/2-20,24,40),"+",24);cross.alignment=TextAlignmentOptions.Center;}
  }
  noticePanel=UIPanel(new Rect(uw/2-335,vr?510:145,670,58),new Color(.16f,.15f,.085f,.93f));
  noticeLabel=UILabel(new Rect(uw/2-315,vr?516:151,630,46),"",18);noticeLabel.alignment=TextAlignmentOptions.Center;
  dangerPanel=UIPanel(new Rect(uw/2-260,uh*.34f,520,60),new Color(.62f,.09f,.04f,.9f));
  var warning=UILabel(new Rect(uw/2-245,uh*.34f+5,490,50),"SPLASH! JUMP BACK TO SAFETY",23,true);warning.transform.SetParent(dangerPanel.transform,true);
  var progress=UIPanel(new Rect(uw/2-100,uh/2+40,200,14),uiInk);
  var fill=new GameObject("Valve progress",typeof(RectTransform),typeof(Image));Position(fill,new Rect(2,2,0,10),progress.transform);holdFill=fill.GetComponent<Image>();holdFill.color=uiMint;holdFill.raycastTarget=false;
  if(vr)BuildVRInventory();else BuildInventory();
  if(touchMode)BuildTouchControls();
 }
 void BuildVRInventory(){
  var root=uiRoot;
  var go=new GameObject("Left wrist inventory",typeof(RectTransform));
  vrWrist=Position(go,new Rect(0,0,340,220));vrWrist.pivot=new Vector2(.5f,.5f);
  vrWristFade=go.AddComponent<CanvasGroup>();vrWristFade.alpha=0;vrWristFade.interactable=false;vrWristFade.blocksRaycasts=false;
  vrWristOpen=false;vrWristAmount=vrWristGestureTime=0;
  var canvas=go.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=uiCanvas.sortingOrder+1;
  uiRoot=vrWrist;
  UIPanel(new Rect(0,0,340,220),new Color(.075f,.10f,.10f,1));
  hudStats=UILabel(new Rect(12,10,150,42),"",32,true);hudStats.alignment=TextAlignmentOptions.Center;
  coinLabel=UILabel(new Rect(174,13,154,36),"",22,true);coinLabel.alignment=TextAlignmentOptions.Center;
  BuildItemSlot(new Rect(32,66,120,120),"Pebbles","RT",out pebbleCount);
  flareIcon=BuildItemSlot(new Rect(188,66,120,120),"Flare","LT",out flareCount);
  UILabel(new Rect(32,190,120,24),"THROW",17).alignment=TextAlignmentOptions.Center;
  UILabel(new Rect(188,190,120,24),"FLARE",17).alignment=TextAlignmentOptions.Center;
  uiRoot=root;
  go.SetActive(false);
 }
}
}

