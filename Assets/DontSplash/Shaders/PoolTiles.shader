Shader "DontSplash/PoolTiles" {
 Properties { [MainColor] _BaseColor("Tile",Color)=(.72,.66,.39,1) _MainTex("Unused grid fallback",2D)="white"{} _BaseMap("Bake albedo",2D)="white"{} }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass { Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #pragma multi_compile_instancing
 #pragma multi_compile _ LIGHTMAP_ON
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor;
 CBUFFER_END
 struct A {float4 p:POSITION;float3 n:NORMAL;float2 lm:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V {float4 p:SV_POSITION;float3 world:TEXCOORD0;half3 n:TEXCOORD1;half fog:TEXCOORD2;float2 lm:TEXCOORD3; UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(i.n);o.fog=ComputeFogFactor(o.p.z);o.lm=i.lm*unity_LightmapST.xy+unity_LightmapST.zw;return o;}
 half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
  half3 n=normalize(i.n);float3 a=abs(n);
  float2 uv=(a.y>a.x&&a.y>a.z)?i.world.xz:(a.x>a.z?i.world.zy:i.world.xy);
  uv*=2.5;float2 cell=frac(uv);float2 edge=min(cell,1-cell);float2 aa=max(fwidth(uv),.001);
  float2 lines=smoothstep(.018-aa,.018+aa,edge);float grout=1-min(lines.x,lines.y);
  float variation=frac(sin(dot(floor(uv),float2(127.1,311.7)))*43758.5453);
  half3 color=_BaseColor.rgb*lerp(.9,1.03,variation);color=lerp(color,_BaseColor.rgb*.4,grout*.75);
  Light light=GetMainLight();
  float cornerFade=lerp(.74,1,smoothstep(.1,2.3,i.world.y));
  float lampPool=.5+.5*cos(i.world.z*.36)*cos(i.world.x*.175);
  half3 hallLight=(half3(.48,.47,.39)+light.color*saturate(dot(n,light.direction))*.48)*(cornerFade+lampPool*.09);
  #if defined(LIGHTMAP_ON)
   half3 baked=SampleLightmap(i.lm,float2(0,0),n);
   // Blend the last two metres of the doorway into the procedural shore light.
   // Floor meshes meet at z=-7 without overlapping.
   float doorway=smoothstep(-9,-7,i.world.z)*(1-smoothstep(3.5,4.2,abs(i.world.x)));
   color*=lerp(baked,hallLight,doorway);
  #else
   color*=hallLight;
  #endif
  return half4(MixFog(color,i.fog),1);
 }
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/Meta"
 Pass {Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask R
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #pragma multi_compile_instancing
 struct DA {float4 p:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct DV {float4 p:SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO};
 DV vert(DA i){DV o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.p=TransformObjectToHClip(i.p.xyz);return o;}
 half4 frag():SV_Target{return 0;}
 ENDHLSL
 }
 }
}

