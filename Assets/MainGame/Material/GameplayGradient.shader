Shader "BlockedIn/GameplayGradient"
{
    Properties { [PerRendererData] _MainTex("Sprite", 2D)="white" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Off
        ZWrite On
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            
            half _HammerMode;
            struct Input { float4 positionOS:POSITION; };
            struct Output { float4 positionCS:SV_POSITION; float3 local:TEXCOORD0; };
            Output Vert(Input i)
            { Output o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.local=i.positionOS.xyz; return o; }
            half4 Frag(Output i):SV_Target
            {
                float2 uv=i.positionCS.xy/_ScaledScreenParams.xy;
                half3 lower=SRGBToLinear(half3(.025,.22,.69));
                half3 upper=lerp(SRGBToLinear(half3(.40,.10,.95)),SRGBToLinear(half3(.11,.36,.80)),smoothstep(0,1,uv.x));
                half3 color=lerp(lower,upper,smoothstep(.12,1,uv.y));
                float2 delta=(uv-float2(.04,.19))*float2(1.1,.8);
                half glow=exp(-dot(delta,delta)*9)*.62;
                color=lerp(color,SRGBToLinear(half3(.025,.49,.77)),glow);
                return half4(color * lerp(1.0h,.3h,saturate(_HammerMode)),1);
            }
            ENDHLSL
        }
    }
}
