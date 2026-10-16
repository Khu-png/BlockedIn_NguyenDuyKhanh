Shader "BlockedIn/BlockPlastic"
{
    Properties
    {
        [MainTexture] _BaseMap ("Block Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Block Color", Color) = (1,1,1,1)
        _GlossStrength ("Soft Reflection", Range(0,1)) = .32
        _GlossPower ("Reflection Focus", Range(4,40)) = 16
        _ShadeStrength ("Side Shading", Range(0,.5)) = .16
        [PerRendererData] _HammerSelected ("Hammer Target", Float) = 0
        [HideInInspector] _Cull ("Cull", Float) = 2
        [HideInInspector] _Cutoff ("Cutoff", Float) = .5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _GlossStrength;
                half _GlossPower;
                half _ShadeStrength;
                half _HammerSelected;
            CBUFFER_END
            half _HammerMode;
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 edgeUv : TEXCOORD1;
                float2 edgeMatch : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 edgeUv : TEXCOORD3;
                half edgeMatch : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                // Round the coating highlight across the existing bevel without changing the mesh.
                half bevel = saturate((input.positionOS.y - .255) / .137);
                bevel *= smoothstep(.15, .65, input.normalOS.y);
                half3 coatingNormal = normalize(lerp(input.normalOS, half3(0, 1, 0), bevel));
                output.normalWS = TransformObjectToWorldNormal(coatingNormal);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.edgeUv = input.edgeUv;
                output.edgeMatch = input.edgeMatch.x;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 normal = normalize(input.normalWS);
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 reflection = reflect(-view, normal);
                // Broad studio reflections keep the coating visible without a reflection probe.
                half3 softbox = normalize(half3(-.6h, .5h, .75h));
                half3 strip = normalize(half3(0.0h, .3h, 1.0h));
                half corner = pow(saturate(dot(reflection, softbox)), _GlossPower);
                half edge = pow(saturate(dot(reflection, strip)), _GlossPower) * .3h;
                half gloss = saturate((corner + edge) * _GlossStrength);
                half diffuse = saturate(dot(normal, GetMainLight().direction));
                half shade = 1.0h - _ShadeStrength * (1.0h - diffuse);
                half3 textureColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                // Rotated ports retain their recess detail while matching the destination edge.
                if (input.edgeMatch < -16.5h)
                {
                    half3 sourceEdge = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.edgeUv.xy).rgb;
                    half3 targetEdge = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.edgeUv.zw).rgb;
                    textureColor *= clamp(targetEdge / max(sourceEdge, .03h), .35h, 2.8h);
                }
                half3 color = textureColor * _BaseColor.rgb;
                color *= shade;
                half3 coated = lerp(color, half3(1.0h, 1.0h, 1.0h), gloss);
                half selected = saturate(_HammerSelected);
                half pulse = (.04h + .04h * sin(_Time.y * 5)) * selected;
                coated = lerp(coated, coated * lerp(.26h, 1.0h, selected) + pulse, saturate(_HammerMode));
                return half4(coated, 1.0h);
            }
            float3 _LightDirection;
            float3 _LightPosition;
            Varyings ShadowVert(Attributes input)
            {
                Varyings output = Vert(input);
                float3 normal = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 direction = normalize(_LightPosition - output.positionWS);
                #else
                    float3 direction = _LightDirection;
                #endif
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(output.positionWS, normal, direction));
                output.positionCS = ApplyShadowClamping(output.positionCS);
                return output;
            }
            half4 DepthFrag(Varyings input) : SV_Target { return 0; }
        ENDHLSL
        Pass
        {
            Name "Plastic"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull Back
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
}
