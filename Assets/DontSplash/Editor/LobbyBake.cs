using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
namespace DontSplash {
public static class LobbyBake {
 const string Folder="Assets/DontSplash/BakedLighting";
 static Scene scene,previous;
 static GameObject root;static bool baking;
 [InitializeOnLoadMethod] static void Init(){
  EditorApplication.delayCall+=TryRequested;
  EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=TryRequested;};
 }
 static void TryRequested(){if(File.Exists("Temp/BakeLobby.request")&&!EditorApplication.isPlayingOrWillChangePlaymode&&!Lightmapping.isRunning){File.Delete("Temp/BakeLobby.request");Bake();}}
 [MenuItem("Dont Splash/Bake lobby lighting")]
 public static void Bake(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||Lightmapping.isRunning){Debug.LogWarning("Exit Play Mode and wait for the current bake before baking the lobby.");return;}
  Directory.CreateDirectory(Folder);
  previous=SceneManager.GetActiveScene();
  scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  root=new GameObject("Baked arrival room");
  var shader=Shader.Find("DontSplash/PoolTiles");
  var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/LobbyTiles.mat");
  if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,Folder+"/LobbyTiles.mat");}
  mat.color=new Color(.78f,.70f,.43f);EditorUtility.SetDirty(mat);
  Add("Floor",new Vector3(0,0,-18),new Vector3(22,1,22),mat);
  Add("Ceiling",new Vector3(0,7,-18),new Vector3(23,.5f,23),mat);
  Add("Back wall",new Vector3(0,3.5f,-29),new Vector3(23,7,.5f),mat);
  for(int side=-1;side<=1;side+=2){
   Add("Side wall",new Vector3(side*11,3.5f,-18),new Vector3(.5f,7,22),mat);
   Add("Doorway side",new Vector3(side*7.6f,3.5f,-7),new Vector3(7,7,.6f),mat);
  }
  Add("Doorway lintel",new Vector3(0,6,-7),new Vector3(8.2f,2,.7f),mat);
  RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.38f,.36f,.28f);
  for(int x=-1;x<=1;x++)for(int z=0;z<2;z++){
   var light=new GameObject("Baked warm fluorescent").AddComponent<Light>();
   light.type=LightType.Rectangle;light.lightmapBakeType=LightmapBakeType.Baked;light.color=new Color(1,.94f,.76f);light.intensity=12;
   light.areaSize=new Vector2(3,.8f);light.range=18;light.transform.position=new Vector3(x*7,6.5f,-12-z*11);light.transform.rotation=Quaternion.Euler(90,0,0);
  }
  var entrance=new GameObject("Baked entrance fluorescent").AddComponent<Light>();
  entrance.type=LightType.Rectangle;entrance.lightmapBakeType=LightmapBakeType.Baked;
  entrance.color=new Color(1,.94f,.8f);entrance.intensity=10;entrance.areaSize=new Vector2(4,.6f);
  entrance.transform.position=new Vector3(0,4.8f,-7.6f);entrance.transform.rotation=Quaternion.Euler(90,0,0);
  var settings=AssetDatabase.LoadAssetAtPath<LightingSettings>(Folder+"/LobbyLighting.asset");
  if(!settings){settings=new LightingSettings();AssetDatabase.CreateAsset(settings,Folder+"/LobbyLighting.asset");}
  settings.bakedGI=true;settings.realtimeGI=false;settings.lightmapper=LightingSettings.Lightmapper.ProgressiveCPU;
  settings.lightmapResolution=8;settings.lightmapMaxSize=1024;settings.directionalityMode=LightmapsMode.NonDirectional;
  settings.directSampleCount=32;settings.indirectSampleCount=64;settings.environmentSampleCount=32;settings.maxBounces=2;
  Lightmapping.lightingSettings=settings;
  EditorSceneManager.SaveScene(scene,Folder+"/LobbyBake.unity");
  Lightmapping.bakeCompleted+=Complete;baking=true;
  File.WriteAllText("Logs/LobbyBake.txt","Baking lobby lighting. No player build.\n");
  if(!Lightmapping.BakeAsync()){File.AppendAllText("Logs/LobbyBake.txt","FAILED to start bake.\n");Cleanup();}
 }
 static void Add(string name,Vector3 position,Vector3 size,Material mat){
  var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root.transform);go.transform.position=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;
  GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI);
 }
 static void Complete(){
  if(!baking)return;
  try{
   var lightmaps=LightmapSettings.lightmaps;
   if(lightmaps.Length==0||lightmaps.Any(l=>!l.lightmapColor)){File.AppendAllText("Logs/LobbyBake.txt","FAILED: no valid lightmaps.\n");return;}
   var binding=root.AddComponent<BakedLobbyLighting>();binding.maps=lightmaps.Select(l=>l.lightmapColor).ToArray();
   binding.surfaces=root.GetComponentsInChildren<Renderer>();binding.indices=binding.surfaces.Select(r=>r.lightmapIndex).ToArray();binding.scaleOffsets=binding.surfaces.Select(r=>r.lightmapScaleOffset).ToArray();
   if(binding.indices.Any(i=>i<0||i>=lightmaps.Length)){File.AppendAllText("Logs/LobbyBake.txt","FAILED: an unmapped surface.\n");return;}
   PrefabUtility.SaveAsPrefabAsset(root,Folder+"/Lobby.prefab");EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   File.AppendAllText("Logs/LobbyBake.txt","SUCCESS: "+lightmaps.Length+" lightmap(s), "+binding.surfaces.Length+" surfaces. Prefab exported.\n");
  }finally{Cleanup();DemoSetup.PrepareScene();}
 }
 static void Cleanup(){
  baking=false;Lightmapping.bakeCompleted-=Complete;
  if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
  if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
 }
}
}

