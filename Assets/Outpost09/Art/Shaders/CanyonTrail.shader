Shader "Outpost09/Soft Trail"
{
    Properties {_BaseMap("Dust texture",2D)="white"{} _BaseColor("Dust",Color)=(0.65,0.46,0.31,0.42)}
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct Input {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};
            struct Output {float4 p:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;};
            Output Vert(Input i){Output o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.n);o.uv=i.uv;o.fog=ComputeFogFactor(o.p.z);return o;}
            half4 Frag(Output i):SV_Target
            {
                half grain=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.world.xz*.2).r;
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
                half3 light=max(SampleSH(normalize(i.normal)),half3(.18,.18,.2))+sun.color*(saturate(dot(normalize(i.normal),sun.direction))*sun.shadowAttenuation*.85+.1);
                half alpha=smoothstep(0,.3,i.uv.x)*smoothstep(0,.3,1-i.uv.x)*_BaseColor.a;
                return half4(MixFog(_BaseColor.rgb*(grain*.5+.6)*light,i.fog),alpha);
            }
            ENDHLSL
        }
    }
}
