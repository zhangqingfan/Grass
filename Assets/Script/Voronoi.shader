Shader "Voronoi"
{
    Properties
    {
        _Count("Count", Range(1, 40)) = 5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            int _Count;

            struct appdata
            {
                float4 vertex : POSITION;
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
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float2 random2(float2 st)
            {
                return frac(sin(float2(dot(st, float2(12.9898, 78.233)),  
                                dot(st, float2(39.3468, 11.135)))) * 43758.5453);
            }

            float4 frag (v2f i) : SV_Target
            {
                float minDist = 10;
                float2 centerPos = float2(0, 0);

                for(int idx = 1; idx < (int)_Count; idx++)
                {
                    float2 curCenterPos = random2(float2(idx, idx));
                    float dist = distance(i.uv, curCenterPos);
                  
                    if(dist < minDist)
                    {
                        minDist = dist;
                        centerPos = curCenterPos;
                    }
                }

                float4 col = float4(centerPos.x, centerPos.y, centerPos.y, 1); 
                return col;
            }
            ENDHLSL
        }
    }
}
