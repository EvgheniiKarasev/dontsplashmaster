using System;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace DontSplash {
public static class MobileInputProbe {
 static SplashGame game;static Touchscreen screen;static int passed;static double started;
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 [InitializeOnLoadMethod]static void Init(){EditorApplication.delayCall+=()=>{if(!File.Exists("Temp/MobileInputProbe.request")&&!SessionState.GetBool("DS.MobileProbe",false))return;File.Delete("Temp/MobileInputProbe.request");SessionState.SetBool("DS.MobileProbe",true);started=EditorApplication.timeSinceStartup;EditorApplication.update+=Wait;if(!EditorApplication.isPlaying)EditorApplication.isPlaying=true;};}
 static void Wait(){
  if(EditorApplication.timeSinceStartup-started>90){SessionState.SetBool("DS.MobileProbe",false);File.WriteAllText("Logs/MobileInputProbe.txt","FAIL: Play Mode startup timeout");EditorApplication.update-=Wait;return;}
  if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
  game=UnityEngine.Object.FindFirstObjectByType<SplashGame>();
  if(!game||Get("eye")==null)return;
  EditorApplication.update-=Wait;
  try{Run();File.WriteAllText("Logs/MobileInputProbe.txt","PASS: "+passed+" checks in Unity Play Mode with synthetic Input System multitouch. Physical phone not tested.");}
  catch(Exception e){File.WriteAllText("Logs/MobileInputProbe.txt","FAIL after "+passed+" checks: "+e);}
  finally{SessionState.SetBool("DS.MobileProbe",false);if(screen!=null)InputSystem.RemoveDevice(screen);if(game)game.enabled=true;EditorApplication.isPlaying=false;}
 }
 static object Get(string name)=>typeof(SplashGame).GetField(name,Flags).GetValue(game);
 static void Set(string name,object value)=>typeof(SplashGame).GetField(name,Flags).SetValue(game,value);
 static object Call(string name,params object[] args)=>typeof(SplashGame).GetMethod(name,Flags).Invoke(game,args);
 static Rect Rect(string name)=>(Rect)typeof(SplashGame).GetProperty(name,Flags).GetValue(game);
 static Vector2 Pixel(Vector2 ui)=>new Vector2(Screen.safeArea.x+ui.x*(float)Get("uiScale"),Screen.safeArea.yMax-ui.y*(float)Get("uiScale"));
 static void Finger(int id,Vector2 p,UnityEngine.InputSystem.TouchPhase phase,Vector2 delta=default){
  InputSystem.QueueStateEvent(screen,new TouchState{touchId=id,position=p,delta=delta,phase=phase});
 }
 static void Read(){InputSystem.Update();Call("ReadInput");}
 static void Check(bool value,string reason){if(!value)throw new Exception(reason);passed++;}
 static void Run(){
  game.enabled=false;Set("mode",Enum.Parse(Get("mode").GetType(),"Play"));screen=InputSystem.AddDevice<Touchscreen>();Set("touchMode",true);Call("SyncUI");
  Set("moveFinger",-1);Set("lookFinger",-1);Set("useFinger",-1);
  float w=(float)Get("uw"),h=(float)Get("uh"),scale=(float)Get("uiScale");
  Vector2 left=Pixel(new Vector2(150,h-150)),right=Pixel(new Vector2(w*.6f,h*.55f)),jump=Pixel(Rect("JumpRect").center),cast=Pixel(Rect("ThrowRect").center);
  Finger(1,left,UnityEngine.InputSystem.TouchPhase.Began);Finger(2,right,UnityEngine.InputSystem.TouchPhase.Began);Read();
  Check((int)Get("moveFinger")==1&&(int)Get("lookFinger")==2,"Two independent finger owners");
  Finger(1,left+Vector2.up*70*scale,UnityEngine.InputSystem.TouchPhase.Moved);
  Finger(2,right+Vector2.right*20,UnityEngine.InputSystem.TouchPhase.Moved,new Vector2(20,0));
  Finger(3,jump,UnityEngine.InputSystem.TouchPhase.Began);Read();
  Check(((Vector2)Get("move")).y>.9f,"Movement held during jump");
  Check(((Vector2)Get("look")).x>0,"Look continues during jump");
  Check((bool)Get("jump"),"Third finger jump");
  Call("UpdateTouchControls");Check(((RectTransform)Get("stickBase")).gameObject.activeSelf,"Floating joystick visible while held");
  float oldYaw=(float)Get("yaw");Call("UpdateLook",.016f);Check((float)Get("yaw")>oldYaw,"Touch delta rotates smoothed camera");
  Set("jump",false);Finger(3,jump,UnityEngine.InputSystem.TouchPhase.Ended);Read();
  Check((int)Get("moveFinger")==1&&(int)Get("lookFinger")==2,"Action release preserves movement/look");
  Finger(2,right+Vector2.right*40,UnityEngine.InputSystem.TouchPhase.Moved,new Vector2(20,0));Finger(4,cast,UnityEngine.InputSystem.TouchPhase.Began);Read();
  Check((bool)Get("throwInput")&&((Vector2)Get("move")).y>.9f&&((Vector2)Get("look")).x>0,"Throw while moving and looking");
  Set("cooldown",0f);Call("Throw");
  var items=(Array)Get("thrown");bool flying=false;foreach(var item in items)flying|=(bool)item.GetType().GetField("active").GetValue(item);
  Check(flying,"Throw creates an active projectile");
  Set("throwInput",false);Finger(4,cast,UnityEngine.InputSystem.TouchPhase.Ended);Read();
  Finger(2,Pixel(Rect("UseRect").center),UnityEngine.InputSystem.TouchPhase.Moved,new Vector2(10,0));Read();
  Check(!(bool)Get("use"),"Camera drag across USE does not activate it");
  Finger(5,Pixel(Rect("UseRect").center),UnityEngine.InputSystem.TouchPhase.Began);Read();
  Check((bool)Get("use"),"Dedicated USE finger");
  Finger(1,left,UnityEngine.InputSystem.TouchPhase.Canceled);Finger(2,right,UnityEngine.InputSystem.TouchPhase.Canceled);Finger(5,Pixel(Rect("UseRect").center),UnityEngine.InputSystem.TouchPhase.Canceled);Read();
  Check((int)Get("moveFinger")==-1&&(int)Get("lookFinger")==-1&&(int)Get("useFinger")==-1,"Canceled touches release all owners");
  Call("UpdateTouchControls");Check(!((RectTransform)Get("stickBase")).gameObject.activeSelf,"Floating joystick hides on release");
  Read();Check(((Vector2)Get("move")).sqrMagnitude==0&&!(bool)Get("use"),"No stuck movement or USE after release");
 }
}
}

