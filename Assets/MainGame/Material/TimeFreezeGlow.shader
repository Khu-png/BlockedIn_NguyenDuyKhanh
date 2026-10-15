Shader "Blocked In/Time Freeze Glow"
{
    Properties { [PerRendererData] _MainTex("Effect Texture", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            Output Vert(Input i)
            { Output o; o.vertex=UnityObjectToClipPos(i.vertex); o.uv=i.uv; o.color=i.color; return o; }
            fixed4 Frag(Output i):SV_Target
            {
                fixed4 sampleColor=tex2D(_MainTex,i.uv);
                fixed mask=max(sampleColor.r,sampleColor.g)*sampleColor.a;
                return fixed4(i.color.rgb,i.color.a*mask);
            }
            ENDCG
        }
    }
}
