Shader "Grass"
{
    Properties
    {
        _Height("Height", Range(1, 5)) = 1
        _TopOffset("TopOffset", Range(-5, 5)) = 0
        _P1Offset("P1Offset", Range(-5, 5)) = 0.5
        _P2Offset("P2Offset", Range(-5, 5)) = 0.5
        _Taper("Taper", Range(1, 5)) = 1
        _Albedo("Albedo", 2D) = "white"{}
        _Gloss("Gloss", 2D) = "white"{}
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

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Bezier.hlsl"

            struct GrassInfo
            {
                float3 pos;
                float rotationDeg;
                float windDeg;
                float windSpeed;
                float heightOffset;
                float topOffset;
                float bend;
            };

            StructuredBuffer<GrassInfo> grassInfoBuffer;
            StructuredBuffer<float3> vertexBuffer;
            StructuredBuffer<float2> uvBuffer;
            StructuredBuffer<float4> colorBuffer;
            StructuredBuffer<int> triangleBuffer;

            RWStructuredBuffer<int> debugBuffer;
            
            float _Height;
            float _TopOffset;
            float _P1Offset;
            float _P2Offset;
            float _Taper;
            float3 _PositionOffset;
            float _WindForce;
            TEXTURE2D(_Gloss);
            SAMPLER(sampler_Gloss);
            TEXTURE2D(_Albedo);
            SAMPLER(sampler_Albedo);

            float3 GetP0()
            {
                return float3(0,0,0);
            }

            float3 GetP3()
            {
                return float3(_TopOffset, _Height, 0);
            }

            float3 GetP1(float blend)
            {
                float3 P1Start = lerp(GetP0(), GetP3(), 0.33);
                float3 dir = cross((GetP3() - GetP0()), float3(0,0,1)); 
                dir = normalize(dir);
                return P1Start + dir * (_P1Offset + blend);
            }

            float3 GetP2(float blend)
            {
                float3 P2Start = lerp(GetP0(), GetP3(), 0.66);
                float3 dir = cross((GetP3() - GetP0()), float3(0,0,1));
                dir = normalize(dir);
                return P2Start + dir * (_P2Offset + blend);
            }

            float3 RotateAroundY(float3 vertex, float angleDeg)
            {
                float angleRad = radians(angleDeg);
    
                float c = cos(angleRad);
                float s = sin(angleRad);
    
                float3 rotatedVertex;
                rotatedVertex.x = vertex.x * c + vertex.z * s; 
                rotatedVertex.y = vertex.y;                    
                rotatedVertex.z = -vertex.x * s + vertex.z * c; 
                
                return rotatedVertex;
            }

            struct appdata
            {
                uint vertexID : SV_VertexID;
                uint instanceID : SV_InstanceID;
                //float4 vertex : POSITION;
            };

            struct v2f
            {
                float3 normal : TEXCOORD0;
                float3 worldPos: TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;

                int index = triangleBuffer[v.vertexID];
                float3 vertex = vertexBuffer[index];
                float4 color = colorBuffer[index];
                float2 uv = uvBuffer[index];
                float3 grassWorldPos = grassInfoBuffer[v.instanceID].pos;

                _Height += grassInfoBuffer[v.instanceID].heightOffset;
                _TopOffset += grassInfoBuffer[v.instanceID].topOffset;
                float blend = grassInfoBuffer[v.instanceID].bend;

                float3 centerPoint = CubicBezier(GetP0(), GetP1(blend), GetP2(blend), GetP3(), color.g);
                centerPoint.x += (vertex.x * (1 - color.g) * _Taper);
                
                //apply rotation
                float rotateDeg = grassInfoBuffer[v.instanceID].rotationDeg;
                centerPoint = RotateAroundY(centerPoint, rotateDeg);
                
                //appley wind speed;
                float windDeg = grassInfoBuffer[v.instanceID].windDeg;
                float windRad = radians(windDeg);
                float curve = 2 * exp(5 * color.g) - 1.0; 
                curve /= (exp(5) - 1.0); 
                float3 windDir = float3(cos(windRad), 0, sin(windRad)) * curve * _WindForce;
                centerPoint += windDir;
                //
                
                o.worldPos = grassWorldPos + centerPoint + _PositionOffset;
                o.vertex = TransformWorldToHClip(o.worldPos); 
                
                //todo....change place!
                float3 tangent = CubicBezierTangent(GetP0(), GetP1(blend), GetP2(blend), GetP3(), color.g);
                float3 normal = normalize(cross(tangent, float3(1,0,0)));
                o.normal = normalize(RotateAroundY(normal, rotateDeg));
                //o.normal = TransformObjectToWorldNormal(normal);
                
                o.uv = uv;
                return o;
            }

            half4 frag (v2f i, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {   
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.worldPos));

                float3 n = isFrontFace ? normalize(i.normal) : -reflect(-normalize(i.normal), normalize(i.normal));
                float3 v = normalize(_WorldSpaceCameraPos - i.worldPos);

                float3 albedo = saturate(_Albedo.Sample(sampler_Albedo, i.uv));
                float gloss = (1 - _Gloss.Sample(sampler_Gloss, i.uv).r) * 0.2;
                half3 GI = SampleSH(n);

                BRDFData brdfdata;
                float alpha = 1;
                InitializeBRDFData(albedo, 0, float3(1,1,1), gloss, alpha, brdfdata);
                float3 brdf = DirectBRDF(brdfdata, n, mainLight.direction, v) * mainLight.color;

                float3 col = GI * albedo + brdf * (mainLight.shadowAttenuation * mainLight.distanceAttenuation);
                return float4(col.xyz, 1);

            }
            ENDHLSL
        }
    }
}
