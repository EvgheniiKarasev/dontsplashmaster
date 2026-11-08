using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DontSplash {
public static partial class QuestSmoke {
 sealed class RunPreferences:IDisposable {
  readonly Dictionary<string,int?> integers=new Dictionary<string,int?>();readonly float? time;
  public RunPreferences(){
   foreach(var key in new[]{"HasSave","Chamber","Leg","DS.Deaths","DS.Checkpoint","DS.Valves","DS.Muted","DS.FirstGiftPending","DS.FirstGiftClaimed"})integers[key]=PlayerPrefs.HasKey(key)?PlayerPrefs.GetInt(key):(int?)null;
   time=PlayerPrefs.HasKey("Time")?PlayerPrefs.GetFloat("Time"):(float?)null;
  }
  public void Dispose(){foreach(var pair in integers){if(pair.Value.HasValue)PlayerPrefs.SetInt(pair.Key,pair.Value.Value);else PlayerPrefs.DeleteKey(pair.Key);}if(time.HasValue)PlayerPrefs.SetFloat("Time",time.Value);else PlayerPrefs.DeleteKey("Time");PlayerPrefs.Save();}
 }
 static void CheckReleasePersistence(SplashGame game){
  Call(game,"OnApplicationPause",false);Call(game,"SetXRUserPresence",true);Call(game,"SetXRSessionFocus",true);Set(game,"vrTrackingAvailable",true);SettleVRRecovery(game);
  PlayerPrefs.SetInt("DS.FirstGiftPending",0);PlayerPrefs.SetInt("Chamber",6);PlayerPrefs.SetInt("Leg",1);PlayerPrefs.SetInt("HasSave",1);PlayerPrefs.SetInt("DS.Checkpoint",-1);PlayerPrefs.SetInt("DS.Valves",0);
  Call(game,"StartRun",true);
  var steps=Field<IList>(game,"steps");int checkpoint=-1;
  for(int i=0;i<steps.Count;i++){var t=steps[i].GetType();var home=(Vector3)t.GetField("home").GetValue(steps[i]);if(home.z>=195&&(int)t.GetField("kind").GetValue(steps[i])==0){checkpoint=i;break;}}
  Check(checkpoint>=0,"Persistence test: no safe checkpoint");Set(game,"checkpointIndex",checkpoint);Set(game,"deaths",7);Set(game,"elapsed",123.5f);
  var valves=Field<IList>(game,"valves");Check(valves.Count>0,"Persistence test: missing valves");valves[0].GetType().GetField("done").SetValue(valves[0],true);
  Call(game,"OnApplicationPause",true);Check(PlayerPrefs.GetInt("DS.Checkpoint")==checkpoint&&PlayerPrefs.GetInt("DS.Deaths")==7,"OS pause failed to save checkpoint/deaths");
  Call(game,"OnApplicationPause",false);SettleVRRecovery(game);Call(game,"StartRun",true);
  Check(Field<int>(game,"stage")==6&&Field<int>(game,"leg")==1&&Field<int>(game,"deaths")==7&&Mathf.Abs(Field<float>(game,"elapsed")-123.5f)<.01f,"Saved run did not round-trip");
  Check(Field<int>(game,"checkpointIndex")==checkpoint&&Vector3.Distance(Field<CharacterController>(game,"body").transform.position,Field<Vector3>(game,"spawn"))<.01f,"Safe checkpoint was not restored");
  valves=Field<IList>(game,"valves");Check((bool)valves[0].GetType().GetField("done").GetValue(valves[0]),"Opened valve was lost");
  var modeField=typeof(SplashGame).GetField("mode",Flags);modeField.SetValue(game,Enum.Parse(modeField.FieldType,"Menu"));Set(game,"uiKey","");Set(game,"nextUIRefresh",0f);Call(game,"SyncUI");Call(game,"LateVR");
  var canvas=Field<Canvas>(game,"uiCanvas");Check(canvas.GetComponentsInChildren<Button>().Any(b=>b.name.EndsWith(" / CONTINUE")),"Saved run has no CONTINUE button");
  canvas.GetComponentsInChildren<Button>().First(b=>b.name.EndsWith(" / NEW RUN")).onClick.Invoke();Check(Field<bool>(game,"confirmNewRun")&&PlayerPrefs.GetInt("Chamber")==6,"New run erased progress before confirmation");
  Set(game,"nextUIRefresh",0f);Call(game,"SyncUI");canvas.GetComponentsInChildren<Button>().First(b=>b.name.EndsWith(" / CANCEL")).onClick.Invoke();Check(PlayerPrefs.GetInt("Chamber")==6,"Cancel erased progress");
  Set(game,"nextUIRefresh",0f);Call(game,"SyncUI");Call(game,"LateVR");Field<Material>(game,"vrComfortMaterial").SetFloat("_Fade",0);Capture(Field<Camera>(game,"eye"),"Logs/Quest-release-continue.png");
  Call(game,"StartRun",true);Call(game,"Finish");Check(PlayerPrefs.GetInt("DS.Checkpoint")==-1&&PlayerPrefs.GetInt("DS.Valves")==0,"Old checkpoint/valves leaked into next chamber");
  PlayerPrefs.SetInt("DS.FirstGiftPending",1);PlayerPrefs.SetInt("DS.FirstGiftClaimed",0);PlayerPrefs.SetInt("Chamber",0);PlayerPrefs.SetInt("Leg",1);Call(game,"StartRun",true);Check(Field<object>(game,"mode").ToString()=="Between","Pending first upgrade was skipped on restart");
  int coins=Field<int>(game,"coins"),upgrade=Field<int>(game,"jumpLevel");Call(game,"StartRun",false);Check(Field<int>(game,"stage")==0&&Field<int>(game,"leg")==0&&Field<int>(game,"deaths")==0&&Field<int>(game,"coins")==coins&&Field<int>(game,"jumpLevel")==upgrade,"New run reset permanent upgrades or failed to restart");
  File.WriteAllText("Logs/Quest-release-save-checks.txt","PASS: OS pause saves run; chamber, checkpoint, opened valve, timer and deaths survive restart; CONTINUE is available; NEW RUN requires confirmation; cancel preserves progress; chamber advance clears old checkpoint/valves; pending first gift survives restart; permanent upgrades survive new run.");
 }
 static void AuditMetaSetup(){
  var lines=new List<string>();
  var tasks=(IEnumerable)typeof(OVRProjectSetup).GetMethod("GetTasks",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{BuildTargetGroup.Android});
  foreach(var task in tasks){
   try{var type=task.GetType();object level=type.GetProperty("Level").GetValue(task),message=type.GetProperty("Message").GetValue(task);
    var check=(Func<BuildTargetGroup,bool>)type.GetField("_isDone",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(task);
    lines.Add((check==null?"MANUAL":check(BuildTargetGroup.Android)?"PASS":"REVIEW")+" | "+level.GetType().GetMethod("GetValue").Invoke(level,new object[]{BuildTargetGroup.Android})+" | "+message.GetType().GetMethod("GetValue").Invoke(message,new object[]{BuildTargetGroup.Android}));
   }catch(Exception e){lines.Add("MANUAL | "+e.GetBaseException().Message);}
  }
  File.WriteAllLines("Logs/Quest-meta-project-setup.txt",lines);
 }
}
}
