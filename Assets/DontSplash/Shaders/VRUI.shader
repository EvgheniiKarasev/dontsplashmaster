Shader "DontSplash/VRUI" {
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Depth test",Float)=8 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"}
 Pass { Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest [_ZTest] Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 CBUFFER_START(UnityPerMaterial)
 half4 _Color;
 CBUFFER_END
 struct A {float4 p:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.color=i.color*_Color;return o;}
 half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;}
 ENDHLSL
 }}
}
