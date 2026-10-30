Shader "DontSplash/Particles" {
 Properties { _MainTex("Particle",2D)="white"{} }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"} 
 Pass { Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
 struct A {float4 p:POSITION;half4 c:COLOR;float2 uv:TEXCOORD0;};
 struct V {float4 p:SV_POSITION;half4 c:COLOR;float2 uv:TEXCOORD0;half fog:TEXCOORD1;};
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.c=i.c;o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i):SV_Target {half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.c;c.rgb=MixFog(c.rgb,i.fog);return c;}
 ENDHLSL
 }}
}
