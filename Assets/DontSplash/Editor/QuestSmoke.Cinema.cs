using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DontSplash {
public static partial class QuestSmoke {
 static Camera cinemaEye;
 static UniversalRenderPipelineAsset cinemaPipeline;
 static RenderPipelineAsset cinemaOriginalQuality;
 static VolumeProfile cinemaProfile;
 static GameObject cinemaRig;
 static RecorderController cinemaRecorder;
 static RecorderControllerSettings cinemaRecorderSettings;
 static MovieRecorderSettings cinemaMovie;
 static Vector3 cinemaStand,cinemaNext,cinemaMonster;
 static int cinemaShot,cinemaFrame,cinemaFrames;
 static float cinemaDuration,cinemaPreviousTime;
 static bool cinemaRunning,cinemaReleased;
 static float cinemaListenerVolume;
 public static string CinemaStatus => $"shot={cinemaShot}; frame={cinemaFrame}/{cinemaFrames}; running={cinemaRunning}; recording={cinemaRecorder?.IsRecording()}; camera={cinemaEye?.transform.position}";
 public static readonly string[] CinemaNames={"01-flooded-hall","02-the-only-way","03-make-a-distraction","04-something-heard-you","05-hold-it-back","06-keep-moving","07-aftermath"};

 public static void CinemaSetup(){
  Check(EditorApplication.isPlaying,"Enter Play mode first");
  if(cinemaRig)return;
  StoreSetup();cinemaEye=Field<Camera>(storeGame,"eye");
  cinemaOriginalQuality=QualitySettings.renderPipeline;
  cinemaPipeline=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset"));
  Check(cinemaPipeline,"PC URP asset required");cinemaPipeline.name="Cinema temporary quality";
  cinemaPipeline.supportsHDR=true;cinemaPipeline.msaaSampleCount=4;cinemaPipeline.renderScale=1;
  cinemaPipeline.shadowDistance=60;cinemaPipeline.mainLightShadowmapResolution=4096;
  cinemaPipeline.additionalLightsShadowmapResolution=4096;cinemaPipeline.supportsCameraDepthTexture=true;cinemaPipeline.supportsCameraOpaqueTexture=true;
  QualitySettings.renderPipeline=cinemaPipeline;
  cinemaEye.allowHDR=true;cinemaEye.allowMSAA=true;cinemaEye.tag="MainCamera";cinemaEye.enabled=true;
  var beforeRender=typeof(SplashGame).GetMethod("BeforeRenderVR",Flags);
  Application.onBeforeRender-=(UnityEngine.Events.UnityAction)Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction),storeGame,beforeRender);
  cinemaEye.fieldOfView=58;cinemaEye.aspect=16f/9;cinemaEye.nearClipPlane=.03f;
  var cameraData=cinemaEye.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=true;cameraData.volumeLayerMask=1;cameraData.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;cameraData.antialiasingQuality=AntialiasingQuality.High;
  cinemaRig=new GameObject("EDITOR ONLY cinematic capture");
  var volume=cinemaRig.AddComponent<Volume>();volume.isGlobal=true;volume.priority=100;volume.weight=1;
  cinemaProfile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=cinemaProfile;
  cinemaProfile.Add<Tonemapping>(true).mode.value=TonemappingMode.ACES;
  var grade=cinemaProfile.Add<ColorAdjustments>(true);grade.contrast.value=22;grade.saturation.value=-12;grade.postExposure.value=.65f;
  var bloom=cinemaProfile.Add<Bloom>(true);bloom.threshold.value=1.1f;bloom.intensity.value=.28f;bloom.scatter.value=.65f;bloom.highQualityFiltering.value=true;
  var vignette=cinemaProfile.Add<Vignette>(true);vignette.intensity.value=.12f;vignette.smoothness.value=.6f;
  RenderPipelineManager.beginFrameRendering+=CinemaOnFrame;
  cinemaListenerVolume=AudioListener.volume;AudioListener.volume=1;
  EditorApplication.playModeStateChanged+=CinemaOnPlayChange;
 }
 static void CinemaOnPlayChange(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode)CinemaCleanup();}
 public static void CinemaPose(int shot,float seconds=0){
  CinemaSetup();CinemaStopRecording();cinemaShot=shot;cinemaDuration=12;cinemaPreviousTime=0;cinemaReleased=false;
  StorePose(3);Call(storeGame,"ResetAtmosphericFX");Call(storeGame,"ResetThrows");
  var steps=Field<System.Collections.IList>(storeGame,"steps").Cast<object>().ToArray();
  var candidates=steps.Where(s=>((Vector3)s.GetType().GetField("size").GetValue(s)).x<12).ToArray();
  var chosen=candidates.OrderBy(s=>Vector3.Distance((Vector3)s.GetType().GetField("home").GetValue(s),new Vector3(17,0,49))).First();
  cinemaStand=(Vector3)chosen.GetType().GetField("home").GetValue(chosen);cinemaStand.y=.65f;
  cinemaNext=candidates.Select(s=>(Vector3)s.GetType().GetField("home").GetValue(s)).Where(p=>p.z>cinemaStand.z+1).OrderBy(p=>Vector3.Distance(p,cinemaStand)).First();cinemaNext.y=.65f;
  Field<CharacterController>(storeGame,"body").transform.position=cinemaStand;
  cinemaMonster=cinemaStand+new Vector3(5,-1.25f,19);
  var creature=Field<MonsterPerformance>(storeGame,"creature");creature.ResetPose();
  Field<Transform>(storeGame,"monster").position=shot==4||shot==5?cinemaMonster:new Vector3(65,-.6f,100);
  Trigger(storeLeft,0);Trigger(storeRight,0);Set(storeGame,"stones",0);Set(storeGame,"cooldown",0f);
  Call(storeGame,"TickVRThrows",Time.unscaledTime);
  if(shot==3||shot==5||shot==6){Trigger(storeRight,1);if(shot==5)Trigger(storeLeft,1);Call(storeGame,"TickVRThrows",Time.unscaledTime+.02f);}
  Color fog=new Color(.19f,.29f,.31f);RenderSettings.fogColor=fog;RenderSettings.fogStartDistance=4;RenderSettings.fogEndDistance=34;
  RenderSettings.ambientLight=new Color(.39f,.51f,.54f);cinemaEye.backgroundColor=fog;cinemaEye.farClipPlane=65;
  Shader.SetGlobalVector("_DSFogDistances",new Vector4(4,34,1,0));
  var water=Field<Material>(storeGame,"water");water.SetColor("_MistColor",fog);water.SetFloat("_FogNear",4);water.SetFloat("_FogFar",34);
  water.SetColor("_BaseColor",new Color(.025f,.15f,.12f,.98f));water.SetColor("_ShallowColor",new Color(.10f,.26f,.22f,.88f));
  Field<Material>(storeGame,"tile").color=new Color(.48f,.59f,.58f);
  Field<Material>(storeGame,"accent").color=new Color(.68f,.45f,.17f);
  foreach(var r in Field<Transform>(storeGame,"monster").GetComponentsInChildren<Renderer>())foreach(var mat in r.materials)if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",new Color(.065f,.085f,.09f));
  var light=Field<Light>(storeGame,"ceilingLight");light.color=new Color(.62f,.80f,.86f);light.intensity=.95f;light.shadows=LightShadows.Soft;
  foreach(var text in Field<System.Collections.Generic.List<TMPro.TMP_Text>>(storeGame,"roomLabels"))if(text)text.gameObject.SetActive(false);
  foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
  Field<LineRenderer>(storeGame,"vrRay").enabled=false;
  var ambience=Field<AudioSource>(storeGame,"ambience");if(!ambience.isPlaying)ambience.Play();
  cinemaEye.fieldOfView=shot==4?40:58;
  CinemaEvaluate(seconds,0);
 }
 static float Smooth(float t)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(t));
 static void CinemaEvaluate(float t,float dt){
  var game=storeGame;var body=Field<CharacterController>(game,"body");var monster=Field<Transform>(game,"monster");
  bool hands=cinemaShot==3||cinemaShot==5||cinemaShot==6;
  Field<Transform>(game,"vrLeft").gameObject.SetActive(hands);Field<Transform>(game,"vrRight").gameObject.SetActive(hands);
  Vector3 basePos=cinemaStand;
  if(cinemaShot==6){float hop=Smooth((t-3.5f)/1.05f);basePos=Vector3.Lerp(cinemaStand,cinemaNext,hop)+Vector3.up*(Mathf.Sin(hop*Mathf.PI)*.6f);}
  body.transform.position=basePos;
  var left=new Vector3(-.30f,1.43f,.64f);var right=new Vector3(.33f,1.38f,.64f);
  float breath=Mathf.Sin(t*1.4f)*.008f;left.y+=breath;right.y+=breath;
  if(cinemaShot==3){
   if(t>=2.6f&&t<3.1f)right=Vector3.Lerp(right,new Vector3(.36f,1.48f,.22f),Smooth((t-2.6f)/.5f));
   if(t>=3.1f&&t<3.45f)right=Vector3.Lerp(new Vector3(.36f,1.48f,.22f),new Vector3(.28f,1.40f,.88f),Smooth((t-3.1f)/.35f));
   if(t>=3.45f)right=Vector3.Lerp(new Vector3(.28f,1.40f,.88f),new Vector3(.33f,1.12f,.5f),Smooth((t-3.45f)/1));
  }
  if(cinemaShot==5){float lift=Smooth((t-2)/1.1f);left=Vector3.Lerp(left,new Vector3(-.24f,1.49f,.65f),lift);if(t>=4.1f)left=Vector3.Lerp(left,new Vector3(-.20f,1.42f,.93f),Smooth((t-4.1f)/.35f));}
  StoreHand(false,left,new Vector3(0,-8,30));StoreHand(true,right,new Vector3(0,8,-30));
  Push(storeHead.centerEyeRotation,Quaternion.Euler(hands?18:0,0,0));
  Push(storeHead.isTracked,1f);Push(storeLeft.isTracked,1f);Push(storeRight.isTracked,1f);
  Call(game,"UpdateVRPose");Field<Animator>(game,"vrLeftAnimator").Update(Mathf.Max(dt,.016f));Field<Animator>(game,"vrRightAnimator").Update(Mathf.Max(dt,.016f));Call(game,"UpdateVRHeldItems");
  Field<Transform>(game,"vrLeftGrip").gameObject.SetActive(hands);Field<Transform>(game,"vrRightGrip").gameObject.SetActive(hands);
  float release=cinemaShot==3?3.42f:4.4f;
  if(!cinemaReleased&&t>=release&&(cinemaShot==3||cinemaShot==5)){
   string field=cinemaShot==3?"vrHeldItem":"vrHeldFlare";var item=typeof(SplashGame).GetField(field,Flags).GetValue(game);
   if(item!=null){var type=item.GetType();type.GetField("held").SetValue(item,false);type.GetField("physicalThrow").SetValue(item,true);type.GetField("velocity").SetValue(item,cinemaShot==3?new Vector3(3.4f,3,8.5f):new Vector3(2.5f,3.5f,11));type.GetField("angularVelocity").SetValue(item,new Vector3(2,1,0));typeof(SplashGame).GetField(field,Flags).SetValue(game,null);}
   Trigger(cinemaShot==3?storeRight:storeLeft,0);cinemaReleased=true;
  }
  if(dt>0){Call(game,"TickThrows",dt);Call(game,"StepFX",dt);}
  var comfort=Field<Material>(game,"vrComfortMaterial");comfort.SetFloat("_Fade",0);comfort.SetFloat("_Vignette",0);
  Vector3 camera=basePos+new Vector3(0,1.65f,-.3f),look=camera+new Vector3(0,-1.3f,4);
  float u=Smooth(t/12);
  if(cinemaShot==1){camera=cinemaStand+Vector3.Lerp(new Vector3(-6,.25f,-4),new Vector3(-3,.55f,0),u);look=cinemaStand+new Vector3(1,.4f,12);}
  if(cinemaShot==2){camera=cinemaStand+Vector3.Lerp(new Vector3(1,2.8f,-5),new Vector3(-1,3.7f,3),u);look=cinemaStand+new Vector3(0,0,15);}
  if(cinemaShot==3){look=basePos+new Vector3(1.2f,.3f,5.5f);}
  if(cinemaShot==4){camera=cinemaStand+new Vector3(-3+u*.9f,1.15f,1);monster.position=cinemaMonster+new Vector3(-u*2,0,-u*3);look=cinemaMonster+new Vector3(-.6f,1.1f,-1);}
  if(cinemaShot==5){monster.position=cinemaStand+new Vector3(4,-1.25f,16-u*3);look=basePos+new Vector3(1,-.2f,7);}
  if(cinemaShot==7){camera=cinemaStand+Vector3.Lerp(new Vector3(4,1.1f,5),new Vector3(7,1.55f,-1),u);look=cinemaStand+new Vector3(0,.5f,18);}
  if(cinemaShot==4||cinemaShot==5){monster.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(camera-monster.position,Vector3.up));Field<MonsterPerformance>(game,"creature").Tick(dt,true,false,cinemaShot==5&&Field<float>(game,"stun")>0,Vector3.Distance(monster.position,camera),.2f);}
  cinemaEye.transform.SetPositionAndRotation(camera,Quaternion.LookRotation(look-camera));
  foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
  cinemaPreviousTime=t;
 }
 public static void CinemaRecord(int shot,string output,int width=3840,int height=2160,int frames=720){
  CinemaPose(shot);cinemaFrame=0;cinemaFrames=frames;cinemaDuration=frames/60f;
  Directory.CreateDirectory(Path.GetDirectoryName(output));
  cinemaRecorderSettings=ScriptableObject.CreateInstance<RecorderControllerSettings>();cinemaRecorderSettings.FrameRate=60;cinemaRecorderSettings.FrameRatePlayback=FrameRatePlayback.Constant;cinemaRecorderSettings.CapFrameRate=false;cinemaRecorderSettings.SetRecordModeToFrameInterval(0,frames-1);
  cinemaMovie=ScriptableObject.CreateInstance<MovieRecorderSettings>();cinemaMovie.name="Cinematic master";cinemaMovie.Enabled=true;cinemaMovie.CaptureAudio=true;cinemaMovie.CaptureAlpha=false;
  cinemaMovie.EncoderSettings=new CoreEncoderSettings{Codec=CoreEncoderSettings.OutputCodec.MP4,EncodingQuality=CoreEncoderSettings.VideoEncodingQuality.Custom,TargetBitRate=width>=3840?100:32,GopSize=60,EncodingProfile=CoreEncoderSettings.H264EncodingProfile.High};
  cinemaMovie.ImageInputSettings=new CameraInputSettings{Source=ImageSource.MainCamera,OutputWidth=width,OutputHeight=height,CaptureUI=false};cinemaMovie.OutputFile=output;
  cinemaRecorderSettings.AddRecorderSettings(cinemaMovie);cinemaRecorder=new RecorderController(cinemaRecorderSettings);cinemaRecorder.PrepareRecording();
  Check(cinemaRecorder.StartRecording(),"Recorder failed to start");cinemaRunning=true;
 }
 static int cinemaLastFrame=-1;
 static void CinemaOnFrame(ScriptableRenderContext context,Camera[] cameras){if(Time.frameCount==cinemaLastFrame)return;cinemaLastFrame=Time.frameCount;if(cinemaRunning)CinemaTick();else if(storeGame&&cinemaEye)CinemaEvaluate(cinemaPreviousTime,0);}
 public static void CinemaTick(){
  if(!cinemaRunning||!storeGame)return;
  if(cinemaFrame>=cinemaFrames){cinemaRunning=false;EditorApplication.delayCall+=CinemaStopRecording;return;}
  CinemaEvaluate(cinemaFrame/60f,1f/60);cinemaFrame++;
 }
 public static void CinemaStopRecording(){cinemaRunning=false;if(cinemaRecorder!=null){cinemaRecorder.StopRecording();cinemaRecorder=null;}if(cinemaMovie)UnityEngine.Object.DestroyImmediate(cinemaMovie);if(cinemaRecorderSettings)UnityEngine.Object.DestroyImmediate(cinemaRecorderSettings);}
 public static void CinemaCapture(string output){
  var previous=cinemaEye.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1920,1080,24);var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);
  try{cinemaEye.targetTexture=rt;cinemaEye.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllBytes(output,tex.EncodeToPNG());}
  finally{cinemaEye.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
 }
 public static void CinemaCleanup(){
  CinemaStopRecording();EditorApplication.playModeStateChanged-=CinemaOnPlayChange;RenderPipelineManager.beginFrameRendering-=CinemaOnFrame;
  QualitySettings.renderPipeline=cinemaOriginalQuality;AudioListener.volume=cinemaListenerVolume;
  if(cinemaRig)UnityEngine.Object.DestroyImmediate(cinemaRig);if(cinemaProfile)UnityEngine.Object.DestroyImmediate(cinemaProfile);if(cinemaPipeline)UnityEngine.Object.DestroyImmediate(cinemaPipeline);
  cinemaRig=null;cinemaEye=null;if(storeGame)StoreCleanup();
 }
}
}
