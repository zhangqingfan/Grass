Shader "Grass"
{
    Properties
    {
        _Height("Height", Range(1, 5)) = 1
        _TopOffset("TopOffset", Range(-5, 5)) = 0
        _P1Offset("P1Offset", Range(-5, 5)) = 0.5
        _P2Offset("P2Offset", Range(-5, 5)) = 0.5
        _Taper("Taper", Range(1, 5)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Bezier.hlsl"

            float _Height;
            float _TopOffset;
            float _P1Offset;
            float _P2Offset;
            float _Taper;

            float3 GetP0()
            {
                return float3(0,0,0);
            }

            float3 GetP3()
            {
                return float3(_TopOffset, _Height, 0);
            }

            float3 GetP1()
            {
                float3 P1Start = lerp(GetP0(), GetP3(), 0.33);
                float3 dir = cross((GetP3() - GetP0()), float3(0,0,1));
                dir = normalize(dir);
                return P1Start + dir * _P1Offset;
            }

            float3 GetP2()
            {
                float3 P2Start = lerp(GetP0(), GetP3(), 0.66);
                float3 dir = cross((GetP3() - GetP0()), float3(0,0,1));
                dir = normalize(dir);
                return P2Start + dir * _P2Offset;
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color: COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float3 centerPoint = CubicBezier(GetP0(), GetP1(), GetP2(), GetP3(), v.color.g);
                centerPoint.x += (v.vertex.x * (1 - v.color.g) * _Taper);
                o.vertex = TransformObjectToHClip(centerPoint); 
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {   
                half4 col = half4(1, 1, 1, 1);
                return col;
            }
            ENDHLSL
        }
    }
}
