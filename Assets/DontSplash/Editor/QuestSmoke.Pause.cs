using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace DontSplash {
public static partial class QuestSmoke {
 static void CheckPauseCombinations(SplashGame game,XRHMD head,OculusTouchControllerProfile.OculusTouchController left,OculusTouchControllerProfile.OculusTouchController right){
  var report=new StringBuilder();var body=Field<CharacterController>(game,"body");var canvas=Field<Canvas>(game,"uiCanvas");
  void Frame(){Call(game,"TickVR");Call(game,"SyncUI");Call(game,"LateVR");}
  void Neutral(){
   Trigger(left,0);Trigger(right,0);Push(left.grip,0f);Push(left.gripPressed,0f);
   Push(right.primaryButton,0f);Push(right.secondaryButton,0f);Push(left.primaryButton,0f);Push(left.menu,0f);
   Push(left.thumbstick,Vector2.zero);Push(right.thumbstick,Vector2.zero);Push(left.isTracked,1f);Push(right.isTracked,1f);Push(head.isTracked,1f);
   Push(head.centerEyePosition,new Vector3(0,1.65f,0));Push(head.centerEyeRotation,Quaternion.identity);Frame();
  }
  try{
   for(int mask=0;mask<64;mask++){
    Neutral();Call(game,"Respawn");Call(game,"Resume");Frame();
    Push(right.secondaryButton,1f);Push(left.menu,(mask&1)!=0?1f:0f);Push(left.primaryButton,(mask&2)!=0?1f:0f);
    Push(right.primaryButton,(mask&4)!=0?1f:0f);Trigger(right,(mask&8)!=0?1f:0f);Trigger(left,(mask&16)!=0?1f:0f);
    Push(left.gripPressed,(mask&32)!=0?1f:0f);Push(left.thumbstick,Vector2.down);Push(right.thumbstick,Vector2.right);Frame();
    Check(Field<object>(game,"mode").ToString()=="Pause","Button combination immediately escaped pause: "+mask);
    Vector3 anchor=canvas.transform.position,position=body.transform.position;float height=body.height;
    var root=Field<RectTransform>(game,"uiRoot");
    for(int frame=0;frame<12;frame++){
     Push(head.centerEyePosition,new Vector3(frame*.004f,1.65f-frame*.012f,frame*.003f));Push(head.centerEyeRotation,Quaternion.Euler(0,frame*2,0));Frame();
     Check(Field<object>(game,"mode").ToString()=="Pause","Held combination toggled pause repeatedly: "+mask);
     Check(Vector3.Distance(anchor,canvas.transform.position)<.0001f,"Held X/combination made menu chase head: "+mask);
     Check(body.transform.position==position&&body.height==height,"Paused capsule moved/resized with tracking");
     Check(Field<RectTransform>(game,"uiRoot")==root,"Held combination recreated pause controls");
    }
    Neutral();Check(Field<object>(game,"mode").ToString()=="Pause","Releasing held controls clicked through pause");
    Push(right.secondaryButton,1f);Frame();Check(Field<object>(game,"mode").ToString()=="Play","Fresh B could not exit pause: "+mask);
    Frame();Check(Field<object>(game,"mode").ToString()=="Play","Held B reopened pause");
   }
   report.AppendLine("64 combinations of B with menu/X/A/triggers/grip and both sticks: stable pause, no held-button retrigger, fresh B resumes.");
   Neutral();Call(game,"Pause");Frame();Vector3 anchored=canvas.transform.position;
   for(int cycle=0;cycle<12;cycle++){
    Call(game,"SetXRSessionFocus",false);Call(game,"RefreshSystemPause");Frame();
    Push(head.centerEyePosition,new Vector3(cycle*.02f,1.65f,0));Push(head.centerEyeRotation,Quaternion.Euler(0,cycle*3,0));
    Call(game,"SetXRSessionFocus",true);Call(game,"RefreshSystemPause");Frame();
    Check(Vector3.Distance(anchored,canvas.transform.position)<.0001f,"Focus flicker moved anchored pause menu");
   }
   report.AppendLine("12 focus-loss/return cycles with head motion: existing pause panel stays anchored.");
   Neutral();Call(game,"OnApplicationFocus",false);Call(game,"SetXRSessionFocus",false);Call(game,"RefreshSystemPause");
   Trigger(left,1);Push(left.gripPressed,1f);Call(game,"SetXRSessionFocus",true);Call(game,"RefreshSystemPause");Frame();
   Check(!Field<bool>(game,"systemSuspended"),"Stale window-focus callback blocked focused OpenXR session");
   Check(!Field<bool>(game,"vrInputReady"),"Held gameplay control lost neutral guard");
   Aim(game,right,"RESUME");Click(game,right,"RESUME");
   Check(Field<object>(game,"mode").ToString()=="Play","Unrelated held left controls blocked right-hand menu click");
   Push(right.secondaryButton,1f);Frame();Check(Field<object>(game,"mode").ToString()=="Pause","Held left controls blocked fresh pause button");
   Neutral();
   Call(game,"OnApplicationPause",true);Call(game,"SetXRSessionFocus",true);Call(game,"RefreshSystemPause");
   Check(Field<bool>(game,"systemSuspended"),"Native focus bypassed actual OS suspension");
   Push(right.secondaryButton,1f);Frame();Call(game,"OnApplicationPause",false);SettleVRRecovery(game);Frame();
   Check(Field<object>(game,"mode").ToString()=="Pause","Held B resumed on focus return");
   Neutral();Push(right.secondaryButton,1f);Frame();Check(Field<object>(game,"mode").ToString()=="Play","Fresh B failed after OS suspension");
   report.AppendLine("OpenXR focus restores input despite stale window-focus callback; OS pause remains authoritative; held controls cannot auto-click; independent right-hand click/B remains usable.");
   var safe=new Rect(0,0,1000,650);string key=(string)Call(game,"UIStateKey",safe,1832,1920);
   Check(key==(string)Call(game,"UIStateKey",safe,1440,1584),"VR resolution change invalidates menu controls");
   report.AppendLine("VR render-target size changes do not invalidate world-space menu layout.");
   File.WriteAllText("Logs/Quest-pause-checks.txt","PASS\n"+report);
  }catch(System.Exception e){File.WriteAllText("Logs/Quest-pause-checks.txt","FAIL\n"+report+e);throw;}
  finally{Set(game,"xrSessionOwnsFocus",false);Call(game,"OnApplicationPause",false);Call(game,"OnApplicationFocus",true);Call(game,"SetXRInputFocus",true);Neutral();Call(game,"Respawn");Call(game,"Resume");Frame();}
 }
}
}
