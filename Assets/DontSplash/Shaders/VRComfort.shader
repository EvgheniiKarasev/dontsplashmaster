Shader "DontSplash/VRComfort" {
 Properties { _Fade("Fade",Range(0,1))=0 _Vignette("Vignette",Range(0,1))=0 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Overlay+100"}
 Pass { Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest Always Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float _Fade; float _Vignette;
 CBUFFER_END
 struct A {float4 p:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V {float4 p:SV_POSITION;float4 screen:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.p=TransformObjectToHClip(i.p.xyz);o.screen=ComputeScreenPos(o.p);return o;}
 half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float2 uv=i.screen.xy/i.screen.w;float edge=smoothstep(.26,.59,length(uv-.5));return half4(0,0,0,max(_Fade,edge*_Vignette));}
 ENDHLSL
 }}
}
