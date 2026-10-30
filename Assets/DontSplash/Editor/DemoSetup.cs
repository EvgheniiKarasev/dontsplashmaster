using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
namespace DontSplash {
public static class DemoSetup {
 [InitializeOnLoadMethod] static void PrepareOnReload(){
  EditorApplication.playModeStateChanged-=AfterPlay;EditorApplication.playModeStateChanged+=AfterPlay;
  EditorApplication.delayCall+=()=>{
   if(EditorApplication.isPlayingOrWillChangePlaymode)return;
   PrepareScene();
  };
 }
 static void AfterPlay(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredEditMode)PrepareScene();}
 [MenuItem("Dont Splash/Prepare demo")]
 public static void PrepareScene(){
  if(!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")){var package=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMPro.TMP_Text).Assembly);if(package!=null){string essentials=Path.Combine(package.resolvedPath,"Package Resources/TMP Essential Resources.unitypackage");if(File.Exists(essentials)){AssetDatabase.ImportPackage(essentials,false);return;}}}
  const string path="Assets/Scenes/DontSplash.unity";
  if(!File.Exists(path)){Create();return;}
  var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
  bool opened=!scene.isLoaded;
  if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
  bool wasDirty=scene.isDirty;
  foreach(var root in scene.GetRootGameObjects()){
   var game=root.GetComponent<SplashGame>();if(!game)continue;
   const string tilePath="Assets/DontSplash/PoolTiles.mat";
   game.tileMaterial=AssetDatabase.LoadAssetAtPath<Material>(tilePath);
   if(!game.tileMaterial&&Shader.Find("DontSplash/PoolTiles")){game.tileMaterial=new Material(Shader.Find("DontSplash/PoolTiles"));AssetDatabase.CreateAsset(game.tileMaterial,tilePath);}
   game.surfaceMaterial=MaterialAsset("Assets/DontSplash/Surface.mat",Color.white);
   game.monsterMaterial=MaterialAsset("Assets/DontSplash/Creature.mat",new Color(.65f,.72f,.65f),AssetDatabase.LoadAssetAtPath<Texture>("Assets/Monster/textures/body.png"));
   const string normalPath="Assets/Monster/textures/body_n.png";
   var normalImporter=AssetImporter.GetAtPath(normalPath) as TextureImporter;
   if(normalImporter&&normalImporter.textureType!=TextureImporterType.NormalMap){normalImporter.textureType=TextureImporterType.NormalMap;normalImporter.SaveAndReimport();}
   game.monsterMaterial.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture>(normalPath));game.monsterMaterial.EnableKeyword("_NORMALMAP");game.monsterMaterial.SetFloat("_BumpScale",.8f);game.monsterMaterial.SetFloat("_Smoothness",.32f);
   game.bakedLobby=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DontSplash/BakedLighting/Lobby.prefab");
   AssignDressing(game); game.waterMaterial=WaterMaterial(); game.bottleMaterial=MaterialAsset("Assets/DontSplash/Bottle.mat",new Color(.2f,.7f,.4f));
   EditorUtility.SetDirty(game);
  }
  if(!wasDirty)EditorSceneManager.SaveScene(scene);
  if(opened)EditorSceneManager.CloseScene(scene,true);
  AssetDatabase.SaveAssets();
 }
 static void AssignDressing(SplashGame game){AssignUI(game);AssignFX(game);foreach(var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Monster/source/Monster 10.fbx")){if(asset is AnimationClip clip&&!clip.name.StartsWith("__preview__")){string take=clip.name.ToLowerInvariant();if(take.Contains("attack"))game.monsterAttack=clip;else if(take.EndsWith("|idle"))game.monsterIdle=clip;else if(take.EndsWith("|run"))game.monsterRun=clip;}}string prefix="Assets/Hypercasual Action Props Pack Volume 3/Prefabs/Standard Shader Prefabs/";string[] names={"WaterCooler","LargeFirstAidKit","WalkieTalkie","NuclearBarrel","EmergencyButtonCase_GRP"};game.dressingAssets=new GameObject[names.Length];for(int i=0;i<names.Length;i++)game.dressingAssets[i]=AssetDatabase.LoadAssetAtPath<GameObject>(prefix+names[i]+"_standardShader.prefab");} static void AssignUI(SplashGame game){
  const string root="Assets/Layer Lab/GUI Pro-SuperCasual/";
  game.uiButtonPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(root+"Prefabs/Prefabs_Component_Buttons/Button_Round01_BtnText_White.prefab");
  game.uiPanelPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(root+"Prefabs/Prefabs_Component_Frames/BasicFrame_Round20_White.prefab");
  game.uiTitlePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(root+"Prefabs/Prefabs_Component_Labels/Title_Tapered02_Dark.prefab");
  game.uiBodyFont=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(root+"ResourcesData/Fonts/Sen SDF.asset");
  game.uiHeadingFont=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(root+"ResourcesData/Fonts/Cairo SDF.asset");
 }
 static void AssignFX(SplashGame game){
  const string root="Assets/Lana Studio/Hyper Casual FX/Prefabs/";
  game.fxAssets=new[]{AssetDatabase.LoadAssetAtPath<GameObject>(root+"Water/Water_blast_blue.prefab"),AssetDatabase.LoadAssetAtPath<GameObject>(root+"Flash/Flash_ellow.prefab"),AssetDatabase.LoadAssetAtPath<GameObject>(root+"Shine/Shine_blue.prefab")};
  const string path="Assets/DontSplash/Particles.mat";
  game.fxMaterial=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!game.fxMaterial){game.fxMaterial=new Material(Shader.Find("DontSplash/Particles"));AssetDatabase.CreateAsset(game.fxMaterial,path);}
 }
 static Material WaterMaterial(){
  const string path="Assets/DontSplash/PoolWaterMobile.mat";
  var m=AssetDatabase.LoadAssetAtPath<Material>(path);
  var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/StylizedWater2/Materials/StylizedWater2_Mobile.mat");
  if(!source)return AssetDatabase.LoadAssetAtPath<Material>("Assets/DontSplash/Water.mat");
  if(!m){m=new Material(source);AssetDatabase.CreateAsset(m,path);}
  m.SetFloat("_ShadingMode",0);m.SetFloat("_RefractionOn",0);m.SetFloat("_EnvironmentReflectionsOn",0);
  m.SetFloat("_PlanarReflectionsEnabled",0);m.SetFloat("_WavesOn",0);m.SetFloat("_WaveHeight",0);m.SetFloat("_CausticsOn",0);
  m.SetFloat("_NormalStrength",.42f);m.SetFloat("_NormalSpeed",.1f);m.SetVector("_NormalTiling",new Vector4(.38f,.38f,0,0));m.SetFloat("_SunReflectionStrength",.75f);m.SetFloat("_SunReflectionSize",.18f);m.SetFloat("_PointSpotLightReflectionStrength",.35f);m.SetFloat("_EdgeFade",.12f);m.SetFloat("_FoamOn",0);m.SetColor("_FoamColor",new Color(.5f,.75f,.66f,.04f));
  m.SetFloat("_IntersectionLength",.25f);m.SetFloat("_IntersectionFalloff",.3f);
  m.SetFloat("_DepthVertical",2);m.SetFloat("_DepthHorizontal",1.5f);m.SetFloat("_DisableDepthTexture",0);
  m.SetColor("_ShallowColor",new Color(.14f,.32f,.3f,.88f));m.SetColor("_BaseColor",new Color(.035f,.16f,.18f,.98f));
  m.SetColor("_HorizonColor",new Color(.28f,.39f,.35f,.12f));m.SetColor("_IntersectionColor",new Color(.5f,.75f,.66f,.5f));
  foreach(var keyword in new[]{"_FOAM","_ADVANCED_SHADING","_REFRACTION","_WAVES","_CAUSTICS","_DISTANCE_NORMALS","_DISABLE_DEPTH_TEX"})m.DisableKeyword(keyword);
  m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");m.EnableKeyword("_RECEIVE_SHADOWS_OFF");EditorUtility.SetDirty(m);return m;
 } static Material MaterialAsset(string path,Color color,Texture texture=null){
  var m=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
  m.color=color;if(texture)m.mainTexture=texture;EditorUtility.SetDirty(m);return m;
 }
 [MenuItem("Dont Splash/Create demo scene")]
 public static void Create(){
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var game=new GameObject("DONT SPLASH").AddComponent<SplashGame>();
  game.monsterAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Monster/source/Monster 10.fbx");
  game.bottleAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Hypercasual Action Props Pack Volume 3/Prefabs/Standard Shader Prefabs/Bottle_standardShader.prefab");
  game.surfaceMaterial=MaterialAsset("Assets/DontSplash/Surface.mat",Color.white);
  game.monsterMaterial=MaterialAsset("Assets/DontSplash/Creature.mat",new Color(.65f,.72f,.65f),AssetDatabase.LoadAssetAtPath<Texture>("Assets/Monster/textures/body.png"));
  AssignDressing(game); game.waterMaterial=WaterMaterial(); game.bottleMaterial=MaterialAsset("Assets/DontSplash/Bottle.mat",new Color(.2f,.7f,.4f));
  EditorSceneManager.SaveScene(scene,"Assets/Scenes/DontSplash.unity");
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/DontSplash.unity",true)};
  AssetDatabase.SaveAssets();
 }
 [MenuItem("Dont Splash/Build Web demo")]
 public static void Build(){
  PrepareScene();
  foreach(var path in AssetDatabase.GetDependencies("Assets/Scenes/DontSplash.unity",true)){
   var importer=AssetImporter.GetAtPath(path) as TextureImporter;
   if(importer==null)continue;
   importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.Compressed;
   importer.crunchedCompression=importer.textureType!=TextureImporterType.Lightmap;importer.compressionQuality=65;importer.SaveAndReimport();
  }
  PokiReleaseSettings.Apply();
  var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,"Build/Web",BuildTarget.WebGL,BuildOptions.None);
  Debug.Log("WEB_RESULT: "+report.summary.result+"; bytes: "+report.summary.totalSize);
  if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Web build failed");
  long bytes=0;foreach(var f in Directory.GetFiles("Build/Web","*",SearchOption.AllDirectories))bytes+=new FileInfo(f).Length;
  File.WriteAllText("Build/build-size.txt","Total deployed files: "+bytes+" bytes. Budget: 50000000 bytes.");
  if(bytes>50000000)throw new System.Exception("Web build exceeds 50 MB: "+bytes);
 }
}
}










