Shader "BlockedIn/BoardSurface"
{
    Properties { [MainColor] _BaseColor("Color", Color)=(.04,.1,.25,1) _ClipOuter("Hide Wall Surface", Float)=0 _ClipToFloor("Floor Backdrop", Float)=0 }
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
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _ClipOuter;
            half _ClipToFloor;
            CBUFFER_END
            float4x4 _GameplayBoardWorldToLocal;
            float4 _GameplayBoardBounds;
            half _HammerMode;
            float4 _GameplayBoardGrid;
            TEXTURE2D(_GameplayBoardCells);
            SAMPLER(sampler_GameplayBoardCells);
            struct Input { float4 positionOS:POSITION; };
            struct Output { float4 positionCS:SV_POSITION; float3 local:TEXCOORD0; };
            Output Vert(Input i)
            { Output o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.local=i.positionOS.xyz; return o; }
            half4 Frag(Output i):SV_Target
            {
                if(_GameplayBoardBounds.z>.5 && (_ClipOuter>.5 || _ClipToFloor>.5))
                {
                    float3 world=TransformObjectToWorld(i.local);
                    float3 board=mul(_GameplayBoardWorldToLocal,float4(world,1)).xyz;
                    clip(.5-_ClipOuter);
                    float2 uv=float2(board.x,-board.z)/_GameplayBoardGrid.xy+.5;
                    clip(min(min(uv.x,uv.y),min(1-uv.x,1-uv.y)));
                    clip(SAMPLE_TEXTURE2D(_GameplayBoardCells,sampler_GameplayBoardCells,uv).r-.5);
                }
                return half4(_BaseColor.rgb * lerp(1.0h,.3h,saturate(_HammerMode)),_BaseColor.a);
            }
            ENDHLSL
        }
    }
}
