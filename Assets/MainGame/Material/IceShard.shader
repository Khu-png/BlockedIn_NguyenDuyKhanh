Shader "BlockedIn/IceShard"
{
    Properties
    {
        _FrostMap("Matching Frost", 2D)="gray" {}
        _BaseColor("Tint / Fade",Color)=(1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+2" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_FrostMap);SAMPLER(sampler_FrostMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _FrostMap_ST;
            CBUFFER_END
            struct Attributes { float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0; };
            struct Varyings { float4 position:SV_POSITION;float3 normal:TEXCOORD0;float2 uv:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;output.position=TransformObjectToHClip(input.vertex.xyz);
                output.normal=TransformObjectToWorldNormal(input.normal);output.uv=input.uv;return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float lighting=saturate(dot(normalize(input.normal),normalize(float3(-.5,.9,.4)))*.5+.5);
                float frost=smoothstep(.6,.88,SAMPLE_TEXTURE2D(_FrostMap,sampler_FrostMap,input.uv).r);
                float3 color=lerp(float3(.035,.3,.63),float3(.28,.79,1),lighting);
                color=lerp(color,float3(.82,.97,1),pow(lighting,8)*.65+frost*.08);
                return half4(color*_BaseColor.rgb,.87*_BaseColor.a);
            }
            ENDHLSL
        }
    }
}
