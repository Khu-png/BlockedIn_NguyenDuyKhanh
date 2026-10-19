Shader "BlockedIn/BlockIce"
{
    Properties
    {
        _BaseColor("Blue Ice Tint / Fade", Color)=(.12,.65,1,1)
        _FrostMap("Patchy Frost", 2D)="gray" {}
        _FrostOpacity("Transparent Face",Range(0,1))=.2
        _RimOpacity("Beveled Edge",Range(0,1))=.88
        _CrackStrength("Face Cracks",Range(0,1))=.8
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "BlueBeveledIce"
            Tags { "LightMode"="UniversalForward" }
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
                float _FrostOpacity,_RimOpacity,_CrackStrength;
            CBUFFER_END
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
                output.surface=input.vertex.xz;output.cell=input.cell;
                output.borders=input.borders;output.layer=input.layer;output.groupUV=input.uv;
                output.normal=TransformObjectToWorldNormal(input.normal);return output;
            }
            float SegmentDistance(float2 p,float2 a,float2 b)
            {
                float2 direction=b-a;
                float along=saturate(dot(p-a,direction)/dot(direction,direction));
                return length(p-a-direction*along);
            }
            float CrackDistance(float2 p)
            {
                float d=SegmentDistance(p,float2(.12,.25),float2(.3,.34));
                d=min(d,SegmentDistance(p,float2(.3,.34),float2(.4,.53)));
                d=min(d,SegmentDistance(p,float2(.4,.53),float2(.61,.6)));
                d=min(d,SegmentDistance(p,float2(.61,.6),float2(.72,.85)));
                d=min(d,SegmentDistance(p,float2(.4,.53),float2(.22,.62)));
                d=min(d,SegmentDistance(p,float2(.22,.62),float2(.15,.79)));
                d=min(d,SegmentDistance(p,float2(.61,.6),float2(.77,.54)));
                d=min(d,SegmentDistance(p,float2(.77,.54),float2(.91,.59)));
                return min(d,SegmentDistance(p,float2(.3,.34),float2(.39,.18)));
            }
            half4 Frag(Varyings input):SV_Target
            {
                float density=SAMPLE_TEXTURE2D(_FrostMap,sampler_FrostMap,input.surface*.52+.17).r;
                float frost=smoothstep(.57,.86,density);
                float4 distances=lerp(10,float4(input.cell.x,1-input.cell.x,input.cell.y,1-input.cell.y),input.borders);
                float edge=min(min(distances.x,distances.y),min(distances.z,distances.w));
                float nearEdge=1-smoothstep(.04,.27,edge);
                float3 color=lerp(_BaseColor.rgb,float3(.88,.97,1),frost*(.12+nearEdge*.64));
                float opacity=_FrostOpacity*.8+frost*(.05+nearEdge*.31);
                if(input.layer.x>.5)
                {
                    float depth=saturate(input.layer.y);
                    float facing=saturate(dot(normalize(input.normal),normalize(float3(-.4,.8,.3)))*.5+.5);
                    color=lerp(float3(.025,.18,.5),float3(.08,.48,.86),facing);
                    color=lerp(color,float3(.24,.73,1),(1-depth)*.3);
                    float glint=(1-smoothstep(.02,.12,depth))*facing*(.2+.8*smoothstep(.58,.85,density));
                    color=lerp(color,float3(.91,.99,1),glint*.8);
                    color=lerp(color,float3(.78,.94,1),frost*.16);
                    float patch=smoothstep(.43,.8,density);
                    opacity=_RimOpacity*(.16+patch*.4);
                    color=lerp(_BaseColor.rgb,float3(.42,.83,1),patch*.45);
                    if(input.layer.x>1.5) { color*=.75;opacity*=.7; }
                }
                if(input.layer.x>2.5)
                {
                    float lighting=saturate(dot(normalize(input.normal),normalize(float3(-.5,.9,.4)))*.5+.5);
                    color=lerp(float3(.035,.3,.63),float3(.28,.79,1),lighting);
                    float highlight=pow(lighting,8)*.65;
                    color=lerp(color,float3(.82,.97,1),highlight);
                    opacity=.9;
                }
                if(input.layer.x<.5)
                {
                    float distance=CrackDistance(input.groupUV);
                    float aa=max(fwidth(distance)*.65,.001);
                    float core=(1-smoothstep(.003-aa,.003+aa,distance))*_CrackStrength;
                    float outline=(1-smoothstep(.009-aa,.009+aa,distance))*_CrackStrength;
                    color=lerp(color,float3(.025,.28,.59),outline*.65);
                    color=lerp(color,float3(.71,.94,1),core);
                    opacity=max(opacity,outline*.28+core*.57);
                }
                return half4(color,saturate(opacity)*_BaseColor.a);
            }
            ENDHLSL
        }
    }
}
