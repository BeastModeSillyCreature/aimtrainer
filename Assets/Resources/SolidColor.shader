// Простой однотонный шейдер для мишеней, пола и стены.
// Лежит в папке Resources, поэтому гарантированно попадает в билд (в отличие от стандартных
// материалов примитивов, шейдер которых в билд может не попасть, из-за чего объекты становятся розовыми).
// _Shade добавляет простое затенение по углу обзора, чтобы шар выглядел объёмным без источников света.
Shader "Custom/SolidColor"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Shade ("Fake Shading", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Shade;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = _WorldSpaceCameraPos - worldPos;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float nv = saturate(dot(normalize(i.worldNormal), normalize(i.viewDir)));
                float k = lerp(1.0, 0.45 + 0.55 * nv, _Shade);
                return fixed4(_Color.rgb * k, 1);
            }
            ENDCG
        }
    }
}
