Shader "DontSplash/PoolWater" {
 Properties { _BaseColor("Water tint",Color)=(0.025,0.18,0.19,1) _MistColor("Mist",Color)=(.08,.14,.14,1) _FogNear("Fog start",Float)=9 _FogFar("Fog end",Float)=30 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
  Pass {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
 #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor; half4 _MistColor; float _FogNear; float _FogFar;
   CBUFFER_END
   struct A {float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V {float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; half fog:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO};
   V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    float2 p=i.world.xz;
    float t=_Time.y*.45;
    float a=sin(p.x*2.8+p.y*1.4+t), b=sin(p.y*3.2-p.x*.7-t*1.3);
    float3 n=normalize(float3(a*.09,1,b*.09));
    float3 view=normalize(_WorldSpaceCameraPos-i.world);
    float fresnel=pow(1-saturate(dot(n,view)),3);
    float light=pow(saturate(dot(reflect(-normalize(float3(.1,1,.4)),n),view)),48);
    float ripple=pow(saturate(a*b),8)*.035;
    half3 col=lerp(_BaseColor.rgb,half3(.16,.3,.29),fresnel)+light*half3(.5,.75,.65)+ripple;
    float visibility=1-saturate((distance(_WorldSpaceCameraPos,i.world)-_FogNear)/max(1,_FogFar-_FogNear)); return half4(lerp(_MistColor.rgb,col,visibility),1);
   }
   ENDHLSL
  }
 }
}

