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
 Image deathBlackout;Canvas uiCanvas;CanvasScaler uiScaler;RectTransform uiRoot;
 string uiKey="";TMP_Text hudTitle,hudStats,noticeLabel,inventoryLabel,codeLabel,assistLabel,soundLabel,busyLabel;
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
  SyncUI();
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
  if(action!=null)button.onClick.AddListener(()=>{if(portal.Busy)return;Sfx(clickClip);action();});
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
  if(mode==Mode.Dead){portal.Break(()=>{stones=Mathf.Max(stones,3);flares=Mathf.Max(flares,1);Respawn();mode=Mode.Play;CursorState(true);portal.StartPlay();});return;}
  if(mode==Mode.Between){portal.Break(()=>{BuildRoom();mode=Mode.Play;CursorState(true);portal.StartPlay();});return;}
  Resume();
 }
 float nextUIRefresh; Mode lastUIMode;
 void SyncUI(){
  if(mode==lastUIMode&&Time.unscaledTime<nextUIRefresh&&mode!=Mode.Caught)return;
  lastUIMode=mode;nextUIRefresh=Time.unscaledTime+.1f;
  if(!uiCanvas)return;
  // Covers the whole display, including outside the safe area; menus render above it.
  if(deathBlackout)deathBlackout.color=new Color(0,0,0,mode==Mode.Dead?1:mode==Mode.Caught?CatchBlackout:0);
  Rect safe=Screen.safeArea;uiScale=Mathf.Max(.1f,Mathf.Min(safe.width/1000f,safe.height/650f));uw=safe.width/uiScale;uh=safe.height/uiScale;
  string key=mode+"/"+codeOpen+"/"+touchMode+"/"+Screen.width+"/"+Screen.height+"/"+safe;
  if(key!=uiKey){
   uiKey=key;lastSafe=safe;uiScaler.scaleFactor=uiScale;
   if(uiRoot){uiRoot.gameObject.SetActive(false);Destroy(uiRoot.gameObject);}
   var root=new GameObject("Safe area",typeof(RectTransform));uiRoot=root.GetComponent<RectTransform>();
   Position(root,new Rect(safe.x/uiScale,(Screen.height-safe.yMax)/uiScale,uw,uh),uiCanvas.transform);
   coinLabel=hudTitle=hudStats=noticeLabel=inventoryLabel=codeLabel=assistLabel=soundLabel=busyLabel=null;noticePanel=dangerPanel=null;holdFill=null;primaryButton=null;
   if(mode==Mode.Play)BuildHUD();else if(mode==Mode.Shop)BuildShop();else if(mode!=Mode.Caught)BuildMenu();
  }
  if(coinLabel)coinLabel.text="COINS  "+coins;
  if(hudTitle)hudTitle.text=(stage+1).ToString("00")+" / "+names[stage];
  if(hudStats){int count=0;foreach(var valve in valves)if(valve.done)count++;hudStats.text="CHAMBER "+(leg+1)+"/3    VALVES "+count+"/2    "+TimeText();}
  if(inventoryLabel)inventoryLabel.text=(touchMode?"":"Q  ")+(stones+reserveBottles>0?"BOTTLE "+(stones+reserveBottles):"PEBBLE / FREE")+(touchMode?"     FLARE ":"     F  FLARE ")+(flares+reserveFlares)+(touchMode?"":"     E  USE");
  if(noticePanel)noticePanel.SetActive(noticeTime>0);if(noticeLabel){noticeLabel.gameObject.SetActive(noticeTime>0);noticeLabel.text=notice;}
  if(dangerPanel)dangerPanel.SetActive(wet>0);
  if(holdFill){holdFill.rectTransform.sizeDelta=new Vector2(Mathf.Clamp01(hold)*196,10);holdFill.transform.parent.gameObject.SetActive(hold>0);}
  if(codeLabel)codeLabel.text=code.PadRight(4,'_');
  if(assistLabel)assistLabel.text=assist?"ASSIST: ON":"ASSIST: OFF";
  if(soundLabel)soundLabel.text=muted?"SOUND: OFF":"SOUND: ON";
  if(primaryButton)primaryButton.interactable=!portal.Busy;
  if(busyLabel)busyLabel.gameObject.SetActive(portal.Busy);
 }
 void BuildMenu(){
  UIPanel(new Rect(0,0,uw,uh),new Color(.055f,.05f,.025f,.88f));
  float x=uw/2-290,y=uh/2-275;
  UIPanel(new Rect(x-25,y-15,630,580),new Color(.2f,.19f,.12f,.97f));
  Heading(new Rect(x,y+8,580,85),mode==Mode.Menu?"DONT SPLASH":codeOpen?"EMERGENCY EXIT":mode==Mode.Win?"SURFACE REACHED":mode==Mode.Dead?"IT HEARD YOU":mode==Mode.Between?"SECTOR CLEAR":"PAUSED");
  if(mode==Mode.Menu){
   UILabel(new Rect(x+20,y+110,540,72),"A blind creature. Twenty-seven flooded chambers.\nKeep the water quiet.",23);
   UILabel(new Rect(x+20,y+193,540,64),touchMode?"Left thumb: move  /  Right thumb: look\nUse the action buttons to jump, distract and turn valves.":"WASD / arrows: move   Mouse: look   Space: jump\nE: valve   Q: bottle   F: flare   Esc: pause",18);
   UIButton(new Rect(x+110,y+280,360,65),"ENTER THE POOLROOMS",()=>StartRun(false));
   if(PlayerPrefs.GetInt("HasSave")==1)UIButton(new Rect(x+110,y+358,360,62),"CONTINUE SAVED RUN",()=>StartRun(true));
   UIButton(new Rect(x+20,y+435,260,52),"ASSIST",()=>assist=!assist,out assistLabel);
   UIButton(new Rect(x+300,y+435,260,52),"SOUND",ToggleSound,out soundLabel);
   UILabel(new Rect(x+20,y+495,540,35),"Assist gives longer jumps and more time to escape the water.",16);
  }else if(codeOpen){
   UILabel(new Rect(x+20,y+108,540,48),"Emergency code engraved in this chamber: 4173",20);
   codeLabel=UILabel(new Rect(x+20,y+162,540,55),"____",40,true);codeLabel.alignment=TextAlignmentOptions.Center;
   for(int i=0;i<10;i++){int digit=i;UIButton(new Rect(x+20+(i%5)*108,y+240+(i/5)*70,98,60),i.ToString(),()=>{if(code.Length<4)code+=digit;});}
   UIButton(new Rect(x+20,y+400,260,60),"CLEAR",()=>code="");
   UIButton(new Rect(x+300,y+400,260,60),"UNLOCK",()=>{if(code=="4173"){codeOpen=false;Finish();}else{code="";codeLabel.color=new Color(1,.4f,.25f);}});
  }else{
   string text=mode==Mode.Between?"Progress saved. Next:\n"+names[stage]+" / "+(leg+1)+" of 3"+"\nReward: "+lastRoomReward+" coins  /  Wallet: "+coins:mode==Mode.Dead?"The creature followed your splash.\nRetry from your last safe island.":mode==Mode.Win?"You escaped all 27 chambers.\nTime: "+TimeText()+" / Falls: "+deaths:"Progress: "+completed+" / 27 chambers\nTime: "+TimeText()+" / Falls: "+deaths;
   UILabel(new Rect(x+20,y+120,540,115),text,24);
   primaryButton=UIButton(new Rect(x+110,y+280,360,70),mode==Mode.Win?"PLAY AGAIN":mode==Mode.Dead?"RETRY CHECKPOINT":mode==Mode.Between?"NEXT CHAMBER":"RESUME",ContinueUI);
   if((mode==Mode.Pause&&!codeOpen)||mode==Mode.Dead)UIButton(new Rect(x+130,y+490,320,56),"LOBBY / UPGRADES",VisitLobby);
   busyLabel=UILabel(new Rect(x+20,y+355,540,40),"Returning to the poolrooms...",18);
   UIButton(new Rect(x+20,y+420,260,60),"SOUND",ToggleSound,out soundLabel);
   UIButton(new Rect(x+300,y+420,260,60),"MAIN MENU",()=>{if(mode!=Mode.Win)Save();mode=Mode.Menu;AudioListener.pause=false;portal.StopPlay();ambience.Stop();});
  }
 }
 void ToggleSound(){muted=!muted;AudioListener.volume=muted?0:1;}
 void BuildHUD(){
  UIPanel(new Rect(18,18,590,92),new Color(.16f,.15f,.085f,.92f));
  hudTitle=UILabel(new Rect(34,25,550,36),"",25,true);hudStats=UILabel(new Rect(34,65,550,30),"",18);
  UIButton(new Rect(uw-115,22,95,60),"II",Pause);
  coinLabel=UILabel(new Rect(uw-355,25,220,40),"",23,true);
  UIButton(new Rect(uw-210,87,190,48),"SHOP",()=>OpenShop());
  var cross=UILabel(new Rect(uw/2-12,uh/2-20,24,40),"+",24);cross.alignment=TextAlignmentOptions.Center;
  noticePanel=UIPanel(new Rect(uw/2-335,145,670,58),new Color(.16f,.15f,.085f,.93f));
  noticeLabel=UILabel(new Rect(uw/2-315,151,630,46),"",18);noticeLabel.alignment=TextAlignmentOptions.Center;
  dangerPanel=UIPanel(new Rect(uw/2-260,uh*.34f,520,60),new Color(.62f,.09f,.04f,.9f));
  var warning=UILabel(new Rect(uw/2-245,uh*.34f+5,490,50),"SPLASH! JUMP BACK TO SAFETY",23,true);warning.transform.SetParent(dangerPanel.transform,true);
  var progress=UIPanel(new Rect(uw/2-100,uh/2+40,200,14),uiInk);
  var fill=new GameObject("Valve progress",typeof(RectTransform),typeof(Image));Position(fill,new Rect(2,2,0,10),progress.transform);holdFill=fill.GetComponent<Image>();holdFill.color=uiMint;holdFill.raycastTarget=false;
  if(touchMode){
   BuildTouchControls();
   inventoryLabel=UILabel(new Rect(25,uh-210,530,35),"",17);
  }else{
   UIPanel(new Rect(uw/2-290,uh-68,580,50),new Color(.16f,.15f,.085f,.9f));
   inventoryLabel=UILabel(new Rect(uw/2-270,uh-60,540,36),"",19);inventoryLabel.alignment=TextAlignmentOptions.Center;
  }
 }
}
}

