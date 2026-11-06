Shader "Outpost09/Geological Ground"
{
    Properties
    {
        _BaseMap("Sandstone grain",2D)="white"{}
        _BaseColor("Rock colour",Color)=(0.55,0.29,0.18,1)
        _SandColor("Dust colour",Color)=(0.62,0.43,0.29,1)
        _Cull("Cull",Float)=2
        _Cutoff("Cutoff",Float)=0.5
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _SandColor;
            float _Cull;
            float _Cutoff;
            CBUFFER_END
            struct Input {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct Output {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half fog:TEXCOORD2;};
            Output Vert(Input i)
            {
                Output o;o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.fog=ComputeFogFactor(o.positionCS.z);return o;
            }
            half Grain(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).r*.65h+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv*2.713+0.37).r*.35h;
            }
            half4 Frag(Output i):SV_Target
            {
                half3 n=normalize(i.normalWS);half3 weights=pow(abs(n),5);weights/=max(dot(weights,half3(1,1,1)),0.001h);
                half grain=Grain(i.positionWS.zy*.18)*weights.x+Grain(i.positionWS.xz*.18)*weights.y+Grain(i.positionWS.xy*.18)*weights.z;
                float layers=sin(i.positionWS.y*1.15+sin(i.positionWS.x*.08)*.9+sin(i.positionWS.z*.055)*.7);
                half dust=smoothstep(.52h,.88h,n.y);
                half3 albedo=lerp(_BaseColor.rgb,_SandColor.rgb,dust)*(grain*.6h+.53h);
                albedo*=lerp(.83h,1.07h,(half)(layers*.5+.5)*(1-dust));
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl=saturate(dot(n,sun.direction));
                half3 ambient=max(SampleSH(n),half3(.16,.17,.20));
                half3 lit=albedo*(ambient+sun.color*(ndl*sun.shadowAttenuation*.85h+.1h));
                // Soft sky bounce gives shaded canyon walls readable colour.
                lit+=albedo*half3(.07,.09,.13)*saturate(n.y*.5h+.5h);
                return half4(MixFog(lit,i.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
