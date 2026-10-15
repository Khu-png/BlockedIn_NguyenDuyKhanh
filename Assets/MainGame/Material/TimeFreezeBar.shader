Shader "Blocked In/Time Freeze Bar"
{
    Properties { [PerRendererData] _MainTex("Texture", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            Output Vert(Input i)
            { Output o; o.vertex=UnityObjectToClipPos(i.vertex); o.uv=i.uv; o.color=i.color; return o; }
            fixed4 Frag(Output i):SV_Target
            {
                float width=length(float2(ddx(i.uv.x),ddy(i.uv.x)));
                float height=length(float2(ddx(i.uv.y),ddy(i.uv.y)));
                float aspect=height/max(width,.00001);
                float2 offset=(i.uv-.5)*float2(aspect,1);
                offset.x=max(abs(offset.x)-max(0,aspect*.5-.5),0);
                float distance=length(offset)-.5;
                float alpha=saturate(.5-distance/max(fwidth(distance),.001));
                return fixed4(i.color.rgb*lerp(.8,1,i.uv.y),i.color.a*alpha);
            }
            ENDCG
        }
    }
}
