using UnityEditor;
using UnityEngine;
namespace DontSplash {
public static class AtmosphereTextureImport {
 [InitializeOnLoadMethod]static void Schedule(){EditorApplication.delayCall+=Configure;}
 static void Configure(){
  foreach(string name in new[]{"WaterRipple","SoftSmoke"}){
   var importer=AssetImporter.GetAtPath("Assets/DontSplash/Resources/FX/"+name+".png") as TextureImporter;if(!importer)continue;
   if(importer.maxTextureSize==512&&importer.alphaIsTransparency&&importer.wrapMode==TextureWrapMode.Clamp&&importer.filterMode==FilterMode.Trilinear&&!importer.isReadable)continue;
   importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
   importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.mipmapEnabled=true;importer.maxTextureSize=512;importer.isReadable=false;importer.SaveAndReimport();
  }
 }
}
}
