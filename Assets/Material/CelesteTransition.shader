Shader "UI/CelesteTransition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0,0,0,1)
        _Cutoff ("Cutoff", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.01
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Aspect ("Aspect Ratio", Float) = 1.77
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float _Cutoff;
            float _Smoothness;
            float4 _Center;
            float _Aspect;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 修正长宽比，使圆圈保持正圆
                float2 uv = i.uv;
                uv.x = (uv.x - _Center.x) * _Aspect + _Center.x;
                
                // 计算到指定中心的距离
                float dist = distance(uv, _Center.xy) * 1.5; 
                
                // _Cutoff 为 1 时全黑，为 0 时全透明
                float alpha = smoothstep(_Cutoff, _Cutoff - _Smoothness, dist);
                
                return fixed4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}
