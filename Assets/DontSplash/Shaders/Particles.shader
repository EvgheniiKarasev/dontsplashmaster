Shader "DontSplash/Particles" {
 Properties { _MainTex("Particle",2D)="white"{} }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"} 
 Pass { Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
 struct A {float4 p:POSITION;half4 c:COLOR;float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V {float4 p:SV_POSITION;half4 c:COLOR;float2 uv:TEXCOORD0;half fog:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.c=i.c;o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.c;c.rgb=MixFog(c.rgb,i.fog);return c;}
 ENDHLSL
 }}
}
