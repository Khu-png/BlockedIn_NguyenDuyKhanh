Shader "BlockedIn/HammerImpactBurst"
{
    Properties { _ImpactProgress("Progress",Range(0,1))=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+25" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _ImpactProgress;
            CBUFFER_END
            struct Attributes { float4 position:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 position:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position=TransformObjectToHClip(input.position.xyz);
                output.uv=input.uv*2-1;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float t=saturate(_ImpactProgress);
                float r=length(input.uv);
                float angle=atan2(input.uv.y,input.uv.x);
                float travel=1-pow(1-t,3);
                float radius=lerp(.07,.9,travel);
                float ring=1-smoothstep(.012,.065,abs(r-radius));
                float glow=exp2(-abs(r-radius)*32)*.35;
                float echo=(1-smoothstep(.008,.035,abs(r-radius*.69)))*.35;
                float rays=pow(saturate(cos(angle*9+.5)),18);
                rays*=smoothstep(.1,.25,r)*(1-smoothstep(.4+travel*.4,.7+travel*.25,r));
                float flash=exp2(-r*r*36)*(1-smoothstep(.04,.32,t));
                float fade=pow(1-t,1.6);
                float alpha=((ring+glow+echo)*smoothstep(0,.08,t)+rays*1.1+flash*1.4)*fade;
                half3 color=lerp(half3(1,.38,.035),half3(1,.92,.5),saturate(ring+flash));
                return half4(color,saturate(alpha));
            }
            ENDHLSL
        }
    }
}
