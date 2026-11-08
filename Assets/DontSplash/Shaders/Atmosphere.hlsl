#ifndef DS_ATMOSPHERE_INCLUDED
#define DS_ATMOSPHERE_INCLUDED
float4 _DSFogDistances; // start, fully opaque distance, enabled, unused
half3 DSApplyFog(half3 color,float3 world){
 float distanceToEye=distance(GetCameraPositionWS(),world);
 float opacity=smoothstep(_DSFogDistances.x,_DSFogDistances.y,distanceToEye);
 return lerp(color,unity_FogColor.rgb,opacity*_DSFogDistances.z);
}
#endif
