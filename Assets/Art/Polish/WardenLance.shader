Shader "DimensionShifter/WardenLance"
{
 Properties { _Tint("Lightning tint",Color)=(.35,.85,1,1) }
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
 float hash(float n){return frac(sin(n*127.1)*43758.5453);}
 half4 frag(V i):SV_Target {
 float y=i.uv.y,t=floor(_Time.y*24),node=floor(y*22);
 float zig=lerp(hash(node+t*7),hash(node+1+t*7),frac(y*22))-.5;
 float taper=smoothstep(0,.18,y)*smoothstep(0,.08,1-y);
 float spine=.5+zig*.16*taper;
 float x=abs(i.uv.x-spine);
 float core=1-smoothstep(.016,.058,x);
 float aura=exp(-x*15)*.42;
 float fork=exp(-abs(i.uv.x-.5-sin(y*65+_Time.y*9)*.15)*70)*.26;
 float tip=exp(-pow((y-.89)*18,2))*exp(-abs(i.uv.x-.5)*19);
 float alpha=saturate(core+aura+fork+tip*.65)*taper*_Tint.a;
 half3 col=lerp(_Tint.rgb,half3(.92,1,1),saturate(core+tip*.7));
 return half4(col,alpha);
 }
 ENDHLSL
 } }
}
