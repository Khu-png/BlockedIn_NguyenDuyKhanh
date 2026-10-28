Shader "BlockedIn/BoardFrame"
{
    Properties { [PerRendererData] _MainTex("Sprite", 2D)="white" {} _BoardShape("Outline Distance", 2D)="white" {} _PanelSize("Panel Size", Vector)=(6,8,0,0) _ResultFade("Result Fade", Range(0,1))=1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            TEXTURE2D(_BoardShape);
            SAMPLER(sampler_BoardShape);
            CBUFFER_START(UnityPerMaterial)
            float4 _PanelSize;
            half _ResultFade;
            CBUFFER_END
            half _HammerMode;
            struct Input { float4 positionOS:POSITION; };
            struct Output { float4 positionCS:SV_POSITION; float3 local:TEXCOORD0; };
            Output Vert(Input i)
            { Output o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.local=i.positionOS.xyz; return o; }
            half4 Frag(Output i):SV_Target
            {
                float2 uv=float2(i.local.x,-i.local.y)/_PanelSize.xy+.5;
                float distance=SAMPLE_TEXTURE2D(_BoardShape,sampler_BoardShape,uv).r*2-1;
                float aa=max(fwidth(distance),.002);
                half fill=1-smoothstep(-aa,aa,distance);
                half glow=exp(-max(distance,0)*max(distance,0)/.012)*.55;
                half3 color=lerp(SRGBToLinear(half3(.025,.28,.72)),SRGBToLinear(half3(.022,.12,.23)),fill);
                return half4(color * lerp(1.0h,.3h,saturate(_HammerMode)),max(fill,glow)*_ResultFade);
            }
            ENDHLSL
        }
    }
}
