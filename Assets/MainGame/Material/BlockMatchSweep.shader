Shader "BlockedIn/ConnectionLightSweep"
{
    Properties
    {
        _JoinLine("Connection Line",Vector)=(0,0,0,1)
        _SweepRadius("Travel Distance",Float)=2
        _SweepProgress("Progress",Range(0,1))=0
        _SweepTint("Light Tint",Color)=(.8,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+15" "RenderType"="Transparent" }
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
                float4 _JoinLine;
                half4 _SweepTint;
                float _SweepRadius, _SweepProgress;
            CBUFFER_END
            struct Attributes { float4 position:POSITION; half4 color:COLOR; };
            struct Varyings { float4 position:SV_POSITION; float2 surface:TEXCOORD0; half border:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position=TransformObjectToHClip(input.position.xyz);
                output.surface=input.position.xz;
                output.border=step(.5,input.color.a);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 segment=_JoinLine.zw-_JoinLine.xy;
                float along=saturate(dot(input.surface-_JoinLine.xy,segment)/max(dot(segment,segment),.0001));
                float distanceToJoin=length(input.surface-(_JoinLine.xy+segment*along));
                float progress=saturate(_SweepProgress);
                float radius=lerp(0,_SweepRadius+.2,saturate((progress-.06)/.72));
                float wave=1-smoothstep(.06,.32,abs(distanceToJoin-radius));
                float reached=1-smoothstep(radius-.12,radius+.02,distanceToJoin);
                float fade=1-smoothstep(.78,1,progress);
                float ignition=(1-smoothstep(.04,.22,distanceToJoin))*(1-smoothstep(.12,.38,progress));
                float face=(wave*.65+reached*.065+ignition*.55)*fade;
                float rim=(wave*.6+reached*.48)*fade;
                return half4(_SweepTint.rgb,lerp(face,rim,input.border));
            }
            ENDHLSL
        }
    }
}
