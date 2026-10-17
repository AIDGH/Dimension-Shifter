Shader "DimensionShifter/LivingRift"
{
 Properties { _Tint("Light",Color)=(.3,.9,1,1) }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
 Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { Tags {"LightMode"="SRPDefaultUnlit"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 p:POSITION;float2 uv:TEXCOORD0;};struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
 float4 _Tint;
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
 half4 frag(V i):SV_Target {
 float y=i.uv.y;float wave=sin(y*24+_Time.y*1.8)*.045+sin(y*53-_Time.y*2)*.02;
 float x=abs(i.uv.x-.5-wave);float core=exp(-x*32);float filaments=exp(-abs(x-.18-.02*sin(y*40+_Time.y))*65);
 float fade=smoothstep(0,.035,y)*smoothstep(0,.035,1-y);
 float mist=exp(-x*8)*(.1+.04*sin(y*80+_Time.y*3));
 return half4(_Tint.rgb*(1+core*.7),(core*.7+filaments*.45+mist)*fade*_Tint.a);
 }
 ENDHLSL
 } }
}
