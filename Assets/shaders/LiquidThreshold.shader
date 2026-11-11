Shader "Bartender/LiquidThreshold"
{
    Properties
    {
        _MainTex ("Liquid Field", 2D) = "black" {}
        _Threshold ("Threshold", Range(0.05, 1.5)) = 0.5
        _Softness ("Edge Softness", Range(0.001, 0.3)) = 0.04
        _RimWidth ("Rim Width", Range(0, 1)) = 0.25
        _RimDarken ("Rim Darken", Range(0, 1)) = 0.7
        _Opacity ("Opacity", Range(0, 1)) = 0.92
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Liquid"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Threshold;
                float _Softness;
                float _RimWidth;
                float _RimDarken;
                float _Opacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 s = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half field = s.a;

                // Average color of the blobs that overlap here (mixed drinks blend)
                half3 col = s.rgb / max(field, 0.0001h);

                // Hard-ish edge where the summed field crosses the threshold
                half mask = smoothstep(_Threshold - _Softness, _Threshold + _Softness, field);

                // Darker rim near the surface edge gives the liquid body
                half inner = smoothstep(_Threshold, _Threshold + _RimWidth, field);
                col *= lerp(_RimDarken, 1.0h, inner);

                return half4(col, mask * _Opacity);
            }
            ENDHLSL
        }
    }
}
