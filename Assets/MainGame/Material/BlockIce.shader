Shader "BlockedIn/BlockIce"
{
    Properties
    {
        _BaseColor ("Ice Tint / Fade", Color) = (.55,.87,1,.42)
        _ReflectionMap ("Reflection Streaks", 2D) = "black" {}
        _ClearAlpha ("Clear Center", Range(0,1)) = .12
        _RimAlpha ("Frozen Edge", Range(0,1)) = .72
        _RimWidth ("Frozen Edge Width", Range(.01,.3)) = .085
        _CrackStrength ("Fine Cracks", Range(0,1)) = .34
        _ReflectionStrength ("Glass Reflection", Range(0,1)) = .2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "IceGlass"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_ReflectionMap); SAMPLER(sampler_ReflectionMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ReflectionMap_ST;
                float _ClearAlpha, _RimAlpha, _RimWidth, _CrackStrength, _ReflectionStrength;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 cell : TEXCOORD1;
                float4 borders : TEXCOORD2;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 cell : TEXCOORD1;
                float4 borders : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;output.cell = input.cell;output.borders = input.borders;
                return output;
            }
            float Segment(float2 p, float2 a, float2 b)
            {
                float2 edge=b-a;
                float t=saturate(dot(p-a,edge)/dot(edge,edge));
                float distance=length(p-a-edge*t);
                return 1-smoothstep(.0015,.0065,distance);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv=input.cell;
                float4 flags=input.borders;
                // Older standalone shells use their normalized UVs as one outer contour.
                if (dot(flags,flags)<.01 && dot(uv,uv)<.00001) { uv=input.uv;flags=1; }
                float4 distances=float4(uv.x,1-uv.x,uv.y,1-uv.y);
                distances=lerp(10,distances,flags);
                float edge=min(min(distances.x,distances.y),min(distances.z,distances.w));
                float rim=1-smoothstep(.005,_RimWidth,edge);
                float frost=1-smoothstep(0,_RimWidth*2.5,edge);
                float crack=Segment(input.uv,float2(.02,.72),float2(.29,.57));
                crack=max(crack,Segment(input.uv,float2(.29,.57),float2(.43,.61)));
                crack=max(crack,Segment(input.uv,float2(.29,.57),float2(.35,.38)));
                crack=max(crack,Segment(input.uv,float2(.98,.2),float2(.76,.33)));
                crack=max(crack,Segment(input.uv,float2(.76,.33),float2(.68,.29)));
                float reflection=SAMPLE_TEXTURE2D(_ReflectionMap,sampler_ReflectionMap,input.uv*float2(.65,1)+float2(.19,.07)).r;
                float sweep=input.uv.x+input.uv.y*.65;
                float band=exp2(-240*pow(sweep-.68,2))*.45;
                float gleam=saturate(reflection*.65+band)*_ReflectionStrength;
                float side=1-abs(normalize(input.normalWS).y);
                float alpha=saturate(lerp(_ClearAlpha,_RimAlpha,rim)+frost*.07+crack*_CrackStrength+gleam+side*.15);
                float3 color=lerp(_BaseColor.rgb,float3(.9,.98,1),saturate(rim*.85+crack*.75+gleam*2));
                color=lerp(color,float3(.28,.7,.98),side*.25);
                return half4(color,alpha*saturate(_BaseColor.a/.42));
            }
            ENDHLSL
        }
    }
}
