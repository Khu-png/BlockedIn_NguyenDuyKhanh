Shader "BlockedIn/BlockIce"
{
    Properties
    {
        _BaseColor("Pale Blue Ice / Fade",Color)=(.55,.84,.96,1)
        _SurfaceMap("Custom Translucent Ice",2D)="white" {}
        _FrostMap("Fine Frost",2D)="gray" {}
        _DistortionMap("Original Ice Scratches",2D)="bump" {}
        _ReflectionMap("Original Ice Reflections",2D)="black" {}
        _FrostOpacity("Milky Surface",Range(0,1))=.42
        _RimOpacity("Luminous Rim",Range(0,1))=.88
        _CrackStrength("Fine Scratch Highlights",Range(0,1))=.16
        _RefractionStrength("Refraction Distortion",Range(0,.08))=.0469
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "FrostedGlassIce"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            TEXTURE2D(_SurfaceMap);SAMPLER(sampler_SurfaceMap);
            TEXTURE2D(_FrostMap);SAMPLER(sampler_FrostMap);
            TEXTURE2D(_DistortionMap);SAMPLER(sampler_DistortionMap);
            TEXTURE2D(_ReflectionMap);SAMPLER(sampler_ReflectionMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _SurfaceMap_ST,_FrostMap_ST,_DistortionMap_ST,_ReflectionMap_ST;
                float _FrostOpacity,_RimOpacity,_CrackStrength,_RefractionStrength;
            CBUFFER_END
            float _IceSceneColorAvailable;
            struct Attributes
            {
                float4 vertex:POSITION;float3 normal:NORMAL;
                float2 uv:TEXCOORD0;float2 cell:TEXCOORD1;
                float4 borders:TEXCOORD2;float2 layer:TEXCOORD3;
            };
            struct Varyings
            {
                float4 position:SV_POSITION;float2 surface:TEXCOORD0;
                float2 cell:TEXCOORD1;float4 borders:TEXCOORD2;
                float2 layer:TEXCOORD3;float3 normal:TEXCOORD4;float2 groupUV:TEXCOORD5;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;output.position=TransformObjectToHClip(input.vertex.xyz);
                output.surface=input.vertex.xz;output.cell=input.cell;output.borders=input.borders;
                output.layer=input.layer;output.groupUV=input.uv;
                output.normal=TransformObjectToWorldNormal(input.normal);return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                // Ignore obsolete crystal geometry while older custom meshes are being upgraded.
                clip(2.5-input.layer.x);
                float2 uv=input.groupUV;
                float2 scratchUV=input.surface*float2(.25,1)+float2(.17,.09);
                half4 iceSurface=SAMPLE_TEXTURE2D(_SurfaceMap,sampler_SurfaceMap,
                    TRANSFORM_TEX(input.groupUV,_SurfaceMap));
                float3 scratch=SAMPLE_TEXTURE2D(_DistortionMap,sampler_DistortionMap,scratchUV).rgb;
                float2 distortion=scratch.rg*2-1;
                float frost=SAMPLE_TEXTURE2D(_FrostMap,sampler_FrostMap,input.surface*float2(.7,1.8)+.17).r;
                float reflection=SAMPLE_TEXTURE2D(_ReflectionMap,sampler_ReflectionMap,
                    uv*float2(.25,.61)+float2(.24,0)+distortion*.18).r;
                float streak=saturate(abs(distortion.x)*3+abs(distortion.y)*1.5);
                float4 distances=lerp(10,float4(input.cell.x,1-input.cell.x,input.cell.y,1-input.cell.y),input.borders);
                float edge=min(min(distances.x,distances.y),min(distances.z,distances.w));
                float edgeMist=1-smoothstep(.02,.22,edge);
                float milk=saturate(_FrostOpacity+frost*.04+streak*.03+edgeMist*.05);
                float3 tint=lerp(_BaseColor.rgb,float3(.48,.78,1),.2+frost*.15);
                tint=lerp(tint,1,reflection*.16+smoothstep(.24,.6,streak)*_CrackStrength*.3);
                float frostHighlight=saturate(iceSurface.a*.55+frost*.12+edgeMist*.08);
                float3 color=lerp(tint,float3(.9,.98,1),frostHighlight);
                float opacity=.28+milk*.08+frostHighlight*.06;
                if(input.layer.x<.5)
                {
                    if(_IceSceneColorAvailable>.5)
                    {
                        float2 screenUV=GetNormalizedScreenSpaceUV(input.position);
                        float2 warp=distortion*_RefractionStrength*float2(.36,.12);
                        float3 behind=SampleSceneColor(saturate(screenUV+warp));
                        color=lerp(behind,tint,.38+milk*.12);
                        color=lerp(color,float3(.9,.98,1),frostHighlight);
                    }
                }
                else
                {
                    float depth=saturate(input.layer.y);
                    float facing=saturate(dot(normalize(input.normal),normalize(float3(-.45,.8,.5)))*.5+.5);
                    float band=pow(saturate(1-abs(depth-.28)*2.7),3);
                    float edgeLine=pow(saturate(1-abs(depth-.88)*7),2);
                    color=lerp(float3(.12,.5,.82),float3(.52,.86,1),facing);
                    color=lerp(color,float3(.98,1,1),saturate(band*.85+edgeLine*.45));
                    opacity=_RimOpacity*(.72+band*.28+edgeLine*.15);
                    if(input.layer.x>1.5)
                    {
                        float lip=1-smoothstep(.04,.2,depth);
                        float bottom=pow(saturate(1-abs(depth-.88)*6),2);
                        color=lerp(float3(.1,.46,.78),float3(.7,.93,1),lip*.7+bottom*.6);
                        opacity=_RimOpacity*(.45+lip*.35+bottom*.22);
                    }
                }
                return half4(color,saturate(opacity)*_BaseColor.a);
            }
            ENDHLSL
        }
    }
}
