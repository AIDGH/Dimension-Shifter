Shader "DimensionShifter/PaintedTerrain"
{
    Properties
    {
        _MainTex ("Stone", 2D) = "white" {}
        _Tint ("Stone tint", Color) = (0.8,0.88,1,1)
        _EdgeColor ("Frost edge", Color) = (0.48,0.72,0.76,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Tint;
            half4 _EdgeColor;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                half3 stone = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb * _Tint.rgb;
                half edge = saturate(input.color.a);
                return half4(lerp(stone, _EdgeColor.rgb, edge * 0.8), _Tint.a);
            }
            ENDHLSL
        }
    }
}
