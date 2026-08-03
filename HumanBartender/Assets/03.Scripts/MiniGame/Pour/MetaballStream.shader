// 블롭(점+반지름) 배열을 받아 필드값을 합산하고 threshold로 잘라 방울들이 서로 뭉쳐 보이게 그린다.
// 블롭 사이 간격이 반지름보다 좁으면 자연스럽게 하나로 이어진 액체 줄기처럼 보인다.
Shader "Pour/MetaballStream"
{
    Properties
    {
        _Color ("Liquid Color", Color) = (1,1,1,1)
        _Threshold ("Threshold", Range(0.01, 3)) = 1.0
        _Smoothness ("Edge Smoothness", Range(0.001, 1)) = 0.15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            #define MAX_BLOBS 96

            fixed4 _Color;
            float _Threshold;
            float _Smoothness;

            int _BlobCount;
            // xy = 블롭 월드 위치, z = 반지름
            float4 _BlobPositions[MAX_BLOBS];

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 worldPos : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float field = 0;

                for (int b = 0; b < _BlobCount; b++)
                {
                    float2 c = _BlobPositions[b].xy;
                    float r = _BlobPositions[b].z;
                    if (r <= 0) continue;

                    float d = distance(i.worldPos, c);
                    float t = saturate(1 - d / r);
                    field += t * t * t;
                }

                float alpha = smoothstep(_Threshold - _Smoothness, _Threshold + _Smoothness, field);
                clip(alpha - 0.001);

                return fixed4(_Color.rgb, alpha * _Color.a);
            }
            ENDCG
        }
    }
}
