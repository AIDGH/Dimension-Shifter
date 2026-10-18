Shader "DimensionShifter/HollowMap" {
 Properties { _MainTex("Texture",2D)="white"{} }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" } Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float _HollowEcho;
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.color=i.color;return o;}
 half4 frag(V i):SV_Target{half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;half lum=dot(c.rgb,half3(.3,.59,.11));c.rgb=lerp(c.rgb*half3(.88,1,1.05),lerp(c.rgb,lum*half3(1.1,.65,1.6),.65),_HollowEcho);return c;}
 ENDHLSL }
 }
}
