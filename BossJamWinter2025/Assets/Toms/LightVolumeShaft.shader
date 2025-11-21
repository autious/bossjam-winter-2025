Shader "Hidden/LightVolumeShaft"
{
    Properties
    {
        _NoiseTexture("_NoiseTexture", 2D) = "white" {}
        _ShaftColor("_ShaftColor", color) = (1,1,1,1)
        _ScatteringPower ("_ScatteringPower", float) = .5
        _ShaftIntensity ("_ShaftIntensity", float) = .5
        _ShaftDensity ("_ShaftDensity", float) = .5
        _MaxLightLevel ("_MaxLightLevel", float) = 0.6
        _MaxDistance ("MaxDistance", float) = 25
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        Stencil
        {
            Ref 4
            Comp Equal
            Pass Keep
        }
        Pass
        {
            Name "VolumePass"
            Cull Off
            ZWrite Off
            ZTest Always
            Blend One One
            
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS_CASCADE
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            
            float _ScatteringPower;
            float _ShaftIntensity;
            float _ShaftDensity;
            float _MaxDistance;
            float _MaxLightLevel;
            float4 _ShaftColor;
            Texture2D _NoiseTexture;
            sampler sampler_NoiseTexture;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 ndc : TEXCOORD1;
            };

            Varyings vert(uint vertex_id : SV_VertexID)
            {
                Varyings o;
                float2 uv = GetFullScreenTriangleVertexPosition(vertex_id).xy;
                o.uv = uv;
                
                // Convert UV to NDC [-1, 1]
                float2 ndc = uv * 2.0 - 1.0;
                #if UNITY_UV_STARTS_AT_TOP
                ndc.y *= -1.0;
                #endif
                
                o.ndc = ndc;
                o.positionCS = float4(ndc, 0, 1);
                return o;
            }


            float3 ReconstructWorldPosition(float2 ndc, float raw_depth)
            {
                float4 clip_pos = float4(ndc, raw_depth, 1.0);
                float4 view_pos = mul(UNITY_MATRIX_I_P, clip_pos);
                view_pos /= view_pos.w;
                return mul(UNITY_MATRIX_I_V, view_pos).xyz;
            }

            
            float SampleShadowSmooth(float4 uv)
            {
                float shadow = 0;
                float2 texelSize = float2(0.25 / 8, 0.25 / 8);
                
                float count = 0;
                const int size = 1;
                const float center_light = MainLightRealtimeShadow(float4(uv.xy, uv.zw));

                if (center_light == 0)
                    return 0;

                shadow += center_light;
                for (int y = -size; y <= size; y++)
                {
                    for (int x = -size; x <= size; x++)
                    {
                        if (x == 0 && y == 0)
                            continue;
                        
                        count += 1;
                        const float light = MainLightRealtimeShadow(float4(uv.xy + texelSize * float2(x, y), uv.zw));
                        shadow += light;
                    }
                }

                return max(0.000001, shadow) / count;
            }

            
            float SampleShadowMap(float3 worldPos)
            {
                float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
                return MainLightRealtimeShadow(shadowCoord);
            }

            
            float Remap(float inMin, float inMax, float outMin, float outMax, float value)
            {
                return outMin + (value - inMin) / (inMax - inMin) * (outMax - outMin);
            }

            
            inline float Linear01Depth(float z)
            {
                return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);
            }

            inline float unity_noise_randomValue(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            inline float unity_noise_interpolate(float a, float b, float t)
            {
                return (1.0 - t) * a + (t * b);
            }

            inline float unity_valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                uv = abs(frac(uv) - 0.5);
                float2 c0 = i + float2(0.0, 0.0);
                float2 c1 = i + float2(1.0, 0.0);
                float2 c2 = i + float2(0.0, 1.0);
                float2 c3 = i + float2(1.0, 1.0);
                float r0 = unity_noise_randomValue(c0);
                float r1 = unity_noise_randomValue(c1);
                float r2 = unity_noise_randomValue(c2);
                float r3 = unity_noise_randomValue(c3);

                float bottomOfGrid = unity_noise_interpolate(r0, r1, f.x);
                float topOfGrid = unity_noise_interpolate(r2, r3, f.x);
                float t = unity_noise_interpolate(bottomOfGrid, topOfGrid, f.y);
                return t;
            }

            void Unity_SimpleNoise_float(float2 UV, float Scale, out float Out)
            {
                float t = 0.0;

                float freq = pow(2.0, float(0));
                float amp = pow(0.5, float(3 - 0));
                t += unity_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;

                freq = pow(2.0, float(1));
                amp = pow(0.5, float(3 - 1));
                t += unity_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;

                freq = pow(2.0, float(2));
                amp = pow(0.5, float(3 - 2));
                t += unity_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;

                Out = t;
            }
            
            
            float4 frag(Varyings input) : SV_Target
            {
                const int volumetric_steps = 64;
                float rawDepth = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, input.uv).r;
                float linear_depth = Linear01Depth(rawDepth);
                float3 worldPos = ReconstructWorldPosition(input.ndc, rawDepth);

                // float2 noise_uv = (worldPos.xz * 0.5 + input.uv) * (worldPos.y * 0.005);
                // noise_uv.y += sin(_Time.x * 0.12) * 12.453461;
                // noise_uv.x += sin(_Time.x * .02) * 18.31;
                // float noise = Remap(0,1,0,3, _NoiseTexture.Sample(sampler_NoiseTexture, noise_uv).r);
                // noise = _NoiseTexture.Sample(sampler_NoiseTexture, noise_uv).r;
                
                if (linear_depth >= 0.999)
                {
                    // worldPos = _WorldSpaceCameraPos + UNITY_MATRIX_IT_MV[2].xyz * _MaxDistance;
                    // return float4(_ShaftColor * _ShaftIntensity * _ShaftDensity * _MaxLightLevel);
                    // return float4(0,1,1,1); // Debug where skybox is
                }

                
                float3 cameraPos = _WorldSpaceCameraPos;
                float3 rayDir = normalize(worldPos - cameraPos);
                float totalDistance = min(_MaxDistance, distance(worldPos, cameraPos));
                float stepSize = totalDistance / volumetric_steps;
                
                float offset = frac(sin(dot(input.uv, float2(12.9898, 78.233))) * 437581.5453);
                float3 currentPos = cameraPos + rayDir * offset * stepSize;
                const float3 one_step = rayDir * stepSize;
                float distance_in_light = 0.0;
                
                [fastopt]
                for (int i = 0; i < volumetric_steps; ++i)
                {
                    currentPos += one_step;
                    
                    float4 shadowCoord = TransformWorldToShadowCoord(currentPos);
                    float shadow = MainLightRealtimeShadow(shadowCoord);

                    float extinction = exp(-_ShaftDensity * (i * stepSize));

                    float2 noise_uv = (currentPos.xz * 0.5 + input.uv) * (currentPos.y * 0.01);
                    noise_uv.y += sin(_Time.x);
                    // noise_uv.x += sin(_Time.x * 0.02) * 1.31;
                    float noise = 0;
                    Unity_SimpleNoise_float(noise_uv, 100, noise);
                    distance_in_light += shadow * extinction * stepSize * noise;
                }

                float3 shaft = _ShaftColor.rgb * distance_in_light * _ShaftIntensity;
                float angleFalloff = Remap(-1, 1, 0.01, 1, dot(rayDir, normalize(GetMainLight().direction)));
                angleFalloff = pow(saturate(angleFalloff), _ScatteringPower);
                shaft *= angleFalloff;

                // Optional tone mapping
                shaft = shaft / (shaft + 1);
                

                shaft.r = Remap(0.0, 1.0, 0.0, _MaxLightLevel, shaft.r);
                shaft.g = Remap(0.0, 1.0, 0.0, _MaxLightLevel, shaft.g);
                shaft.b = Remap(0.0, 1.0, 0.0, _MaxLightLevel, shaft.b);
                return float4(shaft, 0);
            }
            ENDHLSL
        }
        Pass
        {
            Name "Blit Result"
            Cull Off
            ZWrite Off
            ZTest Always
            Blend One One
            
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 ndc : TEXCOORD1;
            };

            Varyings vert(uint vertex_id : SV_VertexID)
            {
                Varyings o;
                float2 uv = GetFullScreenTriangleVertexPosition(vertex_id).xy;
                o.uv = uv;
                
                // Convert UV to NDC [-1, 1]
                float2 ndc = uv * 2.0 - 1.0;
                #if UNITY_UV_STARTS_AT_TOP
                ndc.y *= -1.0;
                #endif
                
                o.ndc = ndc;
                o.positionCS = float4(ndc, 0, 1);
                return o;
            }
            
            Texture2D _BlitTexture;
            sampler sampler_BlitTexture;
            

            inline float Linear01Depth(float z)
            {
                return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);
            }

            
            float4 frag(Varyings input) : SV_Target
            {
                return _BlitTexture.Sample(sampler_BlitTexture, input.uv);


                float4 blended_result = 0;
                float iterations = 0;
                
                float2 texel_size = float2(1.080, 1.920) / 120;
                const int STEPS = 2;
                float4 blit_color = 0;
                float2 uv = input.uv;
                
                // // Gaussain
                // blit_color += _BlitTexture.Sample(sampler_BlitTexture, uv + texel_size * float2(-1, 1)) * 1.0;
                // blit_color += _BlitTexture.Sample(sampler_BlitTexture, uv + texel_size * float2(0, 1)) * 2.0;
                // blit_color += _BlitTexture.Sample(sampler_BlitTexture, uv + texel_size * float2(1, 1)) * 1.0;
                // blit_color += _BlitTexture.Sample(sampler_BlitTexture, uv + texel_size * float2(-1, 0)) * 2.0;
                // blit_color += _BlitTexture.Sample(sampler_BlitTexture, uv + texel_size * float2(0,0)) * 4.0;
                // blit_color += _BlitTexture.Sample(sampler_BlitTexture, uv + texel_size * float2(1,0)) * 2.0;
                // blit_color += _BlitTexture.Sample(sampler_BlitTexture, uv + texel_size * float2(-1,-1)) * 1.0;
                // blit_color += _BlitTexture.Sample(sampler_BlitTexture, uv + texel_size * float2(0,-1)) * 2.0;
                // blit_color += _BlitTexture.Sample(sampler_BlitTexture, uv + texel_size * float2(1,-1)) * 1.0;
                //
                // blit_color /= 16.0;
                // return blit_color;
                [unroll]
                for (int x = -STEPS; x <= STEPS; x++)
                {
                    [unroll]
                    for (int y = -STEPS; y <= STEPS; y++)
                    {
                        // uv.x = clamp(uv.x, 0, 1);
                        // uv.y = clamp(uv.y, 0, 1);
                
                        // float center_dst = lerp(.2, 1.0, (abs(x) + abs(y)) / (STEPS + STEPS));
                
                        float4 blit_color = _BlitTexture.Sample(sampler_BlitTexture, uv + texel_size * float2(x,y));
                
                        blended_result += blit_color;
                        iterations += 1;
                    }   
                }

                return blended_result / iterations;
                return blit_color;
            }
            ENDHLSL
        }
    }
}