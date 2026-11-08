using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace DontSplash {
public sealed partial class SplashGame {
 bool tutorialJumpLaunched,tutorialAirborne,tutorialJumpDone;
 bool JumpHelpVisible=>FirstChamber&&!tutorialJumpDone&&body&&body.transform.position.z>-6;
 bool NearShop=>InLobby&&Mathf.Abs(body.transform.position.x)>3&&Mathf.Abs(body.transform.position.x)<10&&body.transform.position.z>-21&&body.transform.position.z<-12;
 bool CanUseNearby{
  get{
   if(mode!=Mode.Play||!body)return false;
   if(NearShop)return true;
   foreach(var valve in valves)if(!valve.done&&Vector3.Distance(body.transform.position+Vector3.up,valve.t.position)<2.5f)return true;
   return false;
  }
 }
 void TickJumpLesson(){
  if(!FirstChamber||tutorialJumpDone||!tutorialJumpLaunched)return;
  if(!body.isGrounded)tutorialAirborne=true;
  if(wet>0){tutorialJumpLaunched=tutorialAirborne=false;return;}
  if(!tutorialAirborne||!body.isGrounded)return;
  foreach(var step in steps){
   Vector3 local=step.t.InverseTransformPoint(body.transform.position);
   if(step.home.z>=7&&Mathf.Abs(local.x)<.55f&&Mathf.Abs(local.z)<.55f&&body.transform.position.y>step.t.position.y&&body.transform.position.y<step.t.position.y+.85f){
    tutorialJumpDone=true;PlayerPrefs.SetInt("DS.JumpLearned",1);PlayerPrefs.Save();Notice("Nice jump. Reach the coin ahead.",3);break;
   }
  }
  tutorialJumpLaunched=tutorialAirborne=false;
 }
 bool FirstGiftPending=>PlayerPrefs.GetInt("DS.FirstGiftPending")==1&&PlayerPrefs.GetInt("DS.FirstGiftClaimed")==0;
 void ClaimFirstGift(bool jumping){
  if(mode!=Mode.Between||!FirstGiftPending)return;
  if(jumping){if(jumpLevel<3)jumpLevel++;else reserveFlares=Mathf.Min(20,reserveFlares+2);}
  else{if(speedLevel<3)speedLevel++;else reserveFlares=Mathf.Min(20,reserveFlares+2);}
  PlayerPrefs.SetInt("DS.FirstGiftClaimed",1);PlayerPrefs.SetInt("DS.FirstGiftPending",0);SaveEconomy();

  ContinueGame(()=>{BuildRoom();mode=Mode.Play;CursorState(true);});
 }
 float attemptBest;bool attemptTracked;int attemptMilestone;bool jumpHintShown;
 string AttemptKey=>"DS.Best."+stage+"."+leg;
 string AttemptSummary(){
  int reached=Mathf.FloorToInt(attemptBest*100),best=Mathf.Max(reached,PlayerPrefs.GetInt(AttemptKey));
  string tip=flares+reserveFlares>0?(vr?"Hold LEFT TRIGGER, swing your left hand, then release.":touchMode?"Tap the flare when the creature gets close.":"Press F to throw a flare near the creature."):"Jump near the edge. Turn back at dead ends.";
  return "CROSSING REACHED  "+reached+"%"+ "\nPERSONAL BEST  "+best+"%\n"+tip;
 }

 void EndAttempt(bool cleared){
  if(!attemptTracked)return;
  attemptTracked=false;
  PlayerPrefs.SetInt(AttemptKey,Mathf.Max(PlayerPrefs.GetInt(AttemptKey),cleared?100:Mathf.FloorToInt(attemptBest*100)));PlayerPrefs.Save();

 }
 void TickAttempt(){
  if(!attemptTracked){attemptTracked=true;}
  if(!FirstChamber&&!jumpHintShown&&body.transform.position.z>0&&body.transform.position.z<8){
   jumpHintShown=true;Notice(vr?"Reach the far shore. A to jump.":touchMode?"Reach the far shore. Tap the arrow to jump.":"Reach the far shore. SPACE to jump.",5);
  }
  // Progress counts safe landings, so falling or jumping into water earns nothing.
  if(!body.isGrounded||wet>0)return;
  attemptBest=Mathf.Max(attemptBest,Mathf.Clamp01((body.transform.position.z-4)/(EndZ-4)));
  int milestone=Mathf.Min(3,Mathf.FloorToInt(attemptBest*4));
  while(attemptMilestone<milestone){
   attemptMilestone++;
   int earned=RewardOnce("Distance"+attemptMilestone,20);
   if(earned>0){Notice((attemptMilestone*25)+"% CROSSED   +20 COINS",3);Sfx(clickClip);}
  }
 }
 const float WaterY=.10f;
 int lastRoomReward;
 class BonusCoin {public Transform visual;public string key;public Vector3 home;}
 readonly System.Collections.Generic.List<BonusCoin> bonusCoins=new System.Collections.Generic.List<BonusCoin>();
 int coins,reserveFlares,reserveBottles,speedLevel,jumpLevel,rangeLevel,flareLevel;
 bool roomSplashed;float shopCooldown;string shopMessage="Earn coins at new checkpoints. Upgrades stay forever.";
 TMP_Text coinLabel;int shopTab;
 readonly string[] upgradeNames={"RUNNING SHOES","SPRING SOLES","THROWING ARM","FLARE MIX"};
 int UpgradeLevel(int id)=>id==0?speedLevel:id==1?jumpLevel:id==2?rangeLevel:flareLevel;
 int UpgradePrice(int id)=>(id<2?120:80)+UpgradeLevel(id)*60;
 float RunSpeed=>5.3f*(1+speedLevel*.05f);
 float JumpSpeed=>(assist?9.5f:8.7f)*(1+jumpLevel*.03f);
 bool InLobby=>body&&body.transform.position.z<-7;
 void LoadEconomy(){
  coins=Mathf.Max(0,PlayerPrefs.GetInt("DS.Coins",80));
  reserveFlares=Mathf.Clamp(PlayerPrefs.GetInt("DS.Flares"),0,20);reserveBottles=Mathf.Clamp(PlayerPrefs.GetInt("DS.Bottles"),0,30);
  speedLevel=Mathf.Clamp(PlayerPrefs.GetInt("DS.Speed"),0,3);jumpLevel=Mathf.Clamp(PlayerPrefs.GetInt("DS.Jump"),0,3);
  rangeLevel=Mathf.Clamp(PlayerPrefs.GetInt("DS.Range"),0,3);flareLevel=Mathf.Clamp(PlayerPrefs.GetInt("DS.FlarePower"),0,3);
  SaveEconomy();
 }
 void SaveEconomy(){
  PlayerPrefs.SetInt("DS.Coins",coins);PlayerPrefs.SetInt("DS.Flares",reserveFlares);PlayerPrefs.SetInt("DS.Bottles",reserveBottles);
  PlayerPrefs.SetInt("DS.Speed",speedLevel);PlayerPrefs.SetInt("DS.Jump",jumpLevel);PlayerPrefs.SetInt("DS.Range",rangeLevel);PlayerPrefs.SetInt("DS.FlarePower",flareLevel);PlayerPrefs.Save();
 }
 int RewardOnce(string award,int amount){
  string key="DS.Reward."+stage+"."+leg+"."+award;
  if(PlayerPrefs.GetInt(key)==1)return 0;
  PlayerPrefs.SetInt(key,1);coins+=amount;SaveEconomy();return amount;
 }
 void RewardCheckpoint(){
  int earned=RewardOnce("CP"+checkpointIndex,40);
  Save();
  Notice(earned>0?"CHECKPOINT  +40 COINS  /  balance "+coins:"CHECKPOINT SAVED  /  reward already collected");
 }
 void RewardRoom(){
  int earned=RewardOnce("Clear",80);
  if(!roomSplashed)earned+=RewardOnce("Dry",40);
  if(stage==8&&leg==2)earned+=RewardOnce("Escape",200);
  lastRoomReward=earned;
  if(earned>0)Notice("SECTOR CLEAR  +"+earned+" COINS",10);
 }
 void BuyUpgrade(int id){
  if(mode!=Mode.Shop||!InLobby||id<0||id>3)return;
  int level=UpgradeLevel(id),cost=UpgradePrice(id);
  if(level>=3){shopMessage="Already at maximum level.";uiKey="";return;}
  if(coins<cost){shopMessage="Need "+(cost-coins)+" more coins. Reach a new checkpoint.";uiKey="";return;}
  coins-=cost;
  if(id==0)speedLevel++;else if(id==1)jumpLevel++;else if(id==2)rangeLevel++;else flareLevel++;
  SaveEconomy();shopMessage=upgradeNames[id]+" upgraded to "+(level+1)+"/3";Sfx(clickClip);EmitFX(2,body.transform.position+Vector3.forward);uiKey="";
 }
 void BuySupply(bool flare){
  if(mode!=Mode.Shop||!InLobby)return;
  int cost=flare?35:25,stock=flare?reserveFlares:reserveBottles,amount=flare?1:3,cap=flare?20:30;
  if(stock+amount>cap)shopMessage="Reserve is full.";
  else if(coins<cost)shopMessage="Not enough coins. New checkpoints pay 40 coins.";
  else{coins-=cost;if(flare)reserveFlares++;else reserveBottles+=3;SaveEconomy();shopMessage=flare?"Flare added to reserve.":"Three bottles added to reserve.";Sfx(clickClip);EmitFX(2,body.transform.position+Vector3.forward);}
  uiKey="";
 }
 void OpenShop(int tab=0){
  if(mode!=Mode.Play)return;
  if(!InLobby){Notice("Visit the lobby to shop. Pause > LOBBY / UPGRADES.");return;}
  shopTab=tab;mode=Mode.Shop;CursorState(false);move=look=Vector2.zero;moveFinger=lookFinger=-1;
 }
 void CloseShop(){
  shopCooldown=.7f;mode=Mode.Play;AudioListener.pause=false;CursorState(true);
 }
 void VisitLobby(){
  if(mode!=Mode.Pause&&mode!=Mode.Dead)return;
  codeOpen=false;Respawn();body.enabled=false;body.transform.position=new Vector3(0,.65f,-18);body.enabled=true;
  ParkCreature();mode=Mode.Play;AudioListener.pause=false;CursorState(true);Notice("Welcome back. Your checkpoint is kept.");
 }
 void ReturnToCheckpoint(){
  if(mode!=Mode.Shop)return;
  Respawn();CloseShop();
 }
 void PlaceBonusCoin(Vector3 position,string id){
  string key="Treasure"+id;
  if(PlayerPrefs.GetInt("DS.Reward."+stage+"."+leg+"."+key)==1)return;
  var coin=Shape("Bonus coin / optional dead end",position,new Vector3(.5f,.075f,.5f),brass,false,PrimitiveType.Cylinder);
  coin.transform.rotation=Quaternion.Euler(90,0,0);
  bonusCoins.Add(new BonusCoin{visual=coin.transform,key=key,home=position});
 }
 void TickEconomy(float dt){
  TickAttempt();
  for(int i=bonusCoins.Count-1;i>=0;i--){
   var coin=bonusCoins[i];coin.visual.Rotate(0,0,dt*90,Space.Self);coin.visual.position=coin.home+Vector3.up*Mathf.Sin(simTime*2+i)*.1f;
   if(Vector3.Distance(body.transform.position+Vector3.up*.7f,coin.visual.position)<1){
    int earned=RewardOnce(coin.key,15);if(earned>0){Notice(coin.key=="TreasureFirstJumps"?"FIRST THREE JUMPS   +15 COINS":"SECRET COIN  +15   Turn back before the dead end");Sfx(clickClip);EmitFX(2,coin.visual.position);}
    Destroy(coin.visual.gameObject);bonusCoins.RemoveAt(i);
   }
  }
  shopCooldown-=dt;
  if(InLobby&&use&&shopCooldown<=0&&Mathf.Abs(body.transform.position.x)>3&&body.transform.position.z>-21&&body.transform.position.z<-12)OpenShop(body.transform.position.x<0?0:1);
 }
 void BuildShopStalls(){
  for(int side=-1;side<=1;side+=2){
   float x=side*7;
   Material color=side<0?brass:accent;
   Shape("Shop counter",new Vector3(x,1.05f,-15),new Vector3(4,1.1f,1.5f),color);
   Shape("Shop awning",new Vector3(x,3.6f,-15),new Vector3(4.7f,.3f,2.6f),color);
   for(int j=-1;j<=1;j+=2)Shape("Shop post",new Vector3(x+j*2,2,-15),new Vector3(.15f,3,.15f),brass);
   Sign(side<0?"UPGRADES":"EQUIPMENT",new Vector3(x,3,-16),.75f);
   Sign("E / USE TO SHOP",new Vector3(x,1.8f,-16),.4f);
   Shape("Shop interaction pad",new Vector3(x,.53f,-17.5f),new Vector3(3,.025f,3),color,false);
   Prop(side<0?2:1,new Vector3(x,1.65f,-15),.6f,180);
  }
 }
 void PurchaseButton(Rect rect,int price,bool capped,string cappedLabel,System.Action purchase){
  bool affordable=coins>=price&&!capped;
  string text=capped?cappedLabel:affordable?"BUY  /  "+price:price+" COINS\nNEED "+(price-coins)+" MORE";
  TMP_Text caption;var button=UIButton(rect,text,purchase,out caption);
  button.transition=Selectable.Transition.ColorTint;
  var colors=button.colors;colors.disabledColor=Color.white;button.colors=colors;
  button.interactable=affordable;
  button.GetComponent<Image>().color=affordable?new Color(.35f,.87f,.43f):new Color(.29f,.31f,.29f);
  caption.color=affordable?new Color(.035f,.12f,.04f):new Color(.87f,.88f,.84f);
  caption.fontSizeMax=19;caption.fontSizeMin=15;
 }
 void ShopTabButton(Rect rect,string title,int index){
  TMP_Text caption;var button=UIButton(rect,title,()=>{shopTab=index;uiKey="";},out caption);
  button.GetComponent<Image>().color=shopTab==index?uiMint:new Color(.35f,.34f,.25f);
  caption.color=shopTab==index?uiInk:new Color(.9f,.87f,.7f);
 }
 void BuildShop(){
  UIPanel(new Rect(0,0,uw,uh),new Color(.04f,.035f,.02f,.94f));
  float x=uw*.5f-450,y=uh*.5f-300;
  UIPanel(new Rect(x,y,900,600),new Color(.22f,.2f,.12f,.98f));
  Heading(new Rect(x+20,y+12,860,65),"LOBBY SHOP  /  "+coins+" COINS");
  ShopTabButton(new Rect(x+195,y+83,245,56),"UPGRADES",0);
  ShopTabButton(new Rect(x+460,y+83,245,56),"EQUIPMENT",1);
  if(shopTab==0){
   for(int i=0;i<4;i++){
    int id=i,l=UpgradeLevel(i);float row=y+150+i*73;
    UILabel(new Rect(x+30,row,400,30),upgradeNames[i]+"  "+l+"/3",22,true);
    string effect=i==0?"Speed: +"+(l*5)+"%  / next +5%":i==1?"Jump: +"+(l*3)+"%  / next +3%":i==2?"Throw range: +"+(l*20)+"%":"Stun: "+(5+l*.75f).ToString("0.00")+" seconds";
    UILabel(new Rect(x+30,row+30,480,29),effect,17);
    PurchaseButton(new Rect(x+620,row+4,245,58),UpgradePrice(i),l==3,"MAX LEVEL",()=>BuyUpgrade(id));
   }
  }else{
   UILabel(new Rect(x+30,y+156,530,55),"FLARE RESERVE  "+reserveFlares+"/20",26,true);
   UILabel(new Rect(x+30,y+205,530,48),"One extra stun. Used after your free flares.",19);
   PurchaseButton(new Rect(x+620,y+171,245,62),35,reserveFlares>=20,"RESERVE FULL",()=>BuySupply(true));
   UILabel(new Rect(x+30,y+282,530,55),"BOTTLE RESERVE  "+reserveBottles+"/30",26,true);
   UILabel(new Rect(x+30,y+331,530,48),"Three loud decoys. Used after your free bottles.",19);
   PurchaseButton(new Rect(x+620,y+297,245,62),25,reserveBottles+3>30,"RESERVE FULL",()=>BuySupply(false));
   UILabel(new Rect(x+30,y+400,830,45),vr?"Empty bottles? hold RIGHT TRIGGER for a free pebble.":"Empty bottles? Q / THROW uses a free pebble instead.",19);
  }
  UILabel(new Rect(x+30,y+449,840,50),shopMessage,18);
  UIButton(new Rect(x+95,y+516,335,64),"BACK TO LOBBY",CloseShop);
  var back=UIButton(new Rect(x+470,y+516,335,64),checkpointZ>0?"TO CHECKPOINT":"NO CHECKPOINT YET",ReturnToCheckpoint);back.interactable=checkpointZ>0;
 }
}
}

