using UnityEngine;
namespace DontSplash {
public sealed class BakedLobbyLighting : MonoBehaviour {
 public Texture2D[] maps;
 public Renderer[] surfaces;
 public int[] indices;
 public Vector4[] scaleOffsets;
 void Awake(){
  if(maps==null||maps.Length==0)return;
  // Only the current chamber's lobby is loaded. Reuse slots instead of growing
  // the global lightmap array on every chamber transition.
  var data=new LightmapData[maps.Length];
  for(int i=0;i<data.Length;i++)data[i]=new LightmapData{lightmapColor=maps[i]};
  LightmapSettings.lightmapsMode=LightmapsMode.NonDirectional;LightmapSettings.lightmaps=data;
  for(int i=0;i<surfaces.Length;i++)if(surfaces[i]){surfaces[i].lightmapIndex=indices[i];surfaces[i].lightmapScaleOffset=scaleOffsets[i];}
 }
}
}
