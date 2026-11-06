Shader "Outpost09/Canyon Flow"
{
    Properties {
        _BaseColor("Water color", Color) = (0.1, 0.35, 0.4, 0.65)
        _Speed("Flow speed", Float) = 0.25
    }
    SubShader {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float fog:TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _Speed;
            CBUFFER_END
            Varyings vert(Attributes v) { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv; o.fog=ComputeFogFactor(o.positionCS.z); return o; }
            half4 frag(Varyings i):SV_Target {
                float t=_Time.y*_Speed;
                float ripples=sin(i.uv.x*31+sin(i.uv.y*8+t*3)*2)*0.5+0.5;
                float bands=pow(saturate(sin((i.uv.y+t)*18+sin(i.uv.x*7))*0.5+0.5),12);
                half3 col=lerp(_BaseColor.rgb,half3(0.65,0.80,0.80),bands*0.16+ripples*0.05);
                float edge=smoothstep(0,.06,i.uv.x)*smoothstep(0,.06,1-i.uv.x);
                return half4(MixFog(col,i.fog),_BaseColor.a*edge);
            }
            ENDHLSL
        }
    }
}
