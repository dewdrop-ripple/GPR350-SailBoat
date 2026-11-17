// Moves every vertex by the FFT displacement and lights it
Shader "Ocean/Surface"
{
    Properties
    {
        _DeepColor ("Trough Color", Color) = (0.02, 0.15, 0.25, 1)
        _ShallowColor ("Lit Color", Color) = (0.1, 0.45, 0.55, 1)
        _NormalStrength ("Normal Strength", Float) = 1
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 1
        _CrestHeight ("Crest Height (m)", Float) = 5
        _RippleFadeDistance ("Ripple Fade Distance (m)", Float) = 150
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"

            // tile 0 is the swells, tile 1 the ripples
            TEXTURE2D(_OceanDisplacement0);
            SAMPLER(sampler_OceanDisplacement0);
            TEXTURE2D(_OceanDisplacement1);
            SAMPLER(sampler_OceanDisplacement1);
            float _OceanPatch0; // tile size
            float _OceanTexel0; // pixel spacing
            float _OceanPatch1;
            float _OceanTexel1;

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _ShallowColor;
                float _NormalStrength; // 1 = true slopes
                float _ReflectionStrength; // 0 = no sky reflection
                float _CrestHeight; // height that counts as a full crest
                float _RippleFadeDistance;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 restXZ : TEXCOORD1; // flat grid position
            };

            // push of one tile: x, up, z
            float3 Displacement(TEXTURE2D_PARAM(tex, samp), float patch, float2 restXZ)
            {
                return SAMPLE_TEXTURE2D_LOD(tex, samp, restXZ / patch, 0).xyz;
            }

            // the mesh follows only the swells, ripples only tilt the lighting
            float3 Displaced(float2 restXZ)
            {
                float3 d = Displacement(TEXTURE2D_ARGS(_OceanDisplacement0, sampler_OceanDisplacement0), _OceanPatch0, restXZ);
                return float3(restXZ.x + d.x, d.y, restXZ.y + d.z);
            }

            // how one tile bends the surface: its push change per metre along x and z
            void AddTilt(TEXTURE2D_PARAM(tex, samp), float patch, float texel, float2 restXZ, float weight, inout float3 alongX, inout float3 alongZ)
            {
                float2 dx = float2(texel, 0.0);
                float2 dz = float2(0.0, texel);
                float3 changeX = Displacement(TEXTURE2D_ARGS(tex, samp), patch, restXZ + dx) - Displacement(TEXTURE2D_ARGS(tex, samp), patch, restXZ - dx);
                float3 changeZ = Displacement(TEXTURE2D_ARGS(tex, samp), patch, restXZ + dz) - Displacement(TEXTURE2D_ARGS(tex, samp), patch, restXZ - dz);
                alongX += weight * changeX / (2.0 * texel);
                alongZ += weight * changeZ / (2.0 * texel);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 restWS = TransformObjectToWorld(input.positionOS);
                float3 positionWS = Displaced(restWS.xz);
                positionWS.y += restWS.y;

                output.positionWS = positionWS;
                output.restXZ = restWS.xz;
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // surface direction: a flat grid bent by each tile
                float3 alongX = float3(1.0, 0.0, 0.0);
                float3 alongZ = float3(0.0, 0.0, 1.0);
                AddTilt(TEXTURE2D_ARGS(_OceanDisplacement0, sampler_OceanDisplacement0), _OceanPatch0, _OceanTexel0, input.restXZ, 1.0, alongX, alongZ);

                // ripples fade with distance
                float rippleFade = saturate(1.0 - distance(_WorldSpaceCameraPos, input.positionWS) / _RippleFadeDistance);
                AddTilt(TEXTURE2D_ARGS(_OceanDisplacement1, sampler_OceanDisplacement1), _OceanPatch1, _OceanTexel1, input.restXZ, rippleFade, alongX, alongZ);

                alongX.y *= _NormalStrength;
                alongZ.y *= _NormalStrength;
                float3 normal = normalize(cross(alongZ, alongX));

                Light light = GetMainLight();
                float lightFacing = saturate(dot(normal, light.direction));

                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));

                // glancing angles mirror the sky
                float fresnel = 0.02 + 0.98 * pow(1.0 - saturate(dot(normal, view)), 5.0);

                // keep the reflection above the horizon
                float3 reflected = reflect(-view, normal);
                reflected.y = abs(reflected.y);
                half3 sky = GlossyEnvironmentReflection(reflected, 0.0, 1.0);

                // color is scattered light: thin crests glow, most when facing the sun
                float height = Displaced(input.restXZ).y;
                float crest = saturate(0.5 + 0.5 * height / _CrestHeight);
                float backlight = pow(saturate(dot(light.direction, -view)), 4.0);
                float scatter = saturate(crest * 0.5 + backlight * crest * 0.5 + lightFacing * 0.15);
                half3 water = lerp(_DeepColor.rgb, _ShallowColor.rgb, scatter) * (0.5 + 0.5 * light.color);

                // sun glint
                float3 halfway = normalize(light.direction + view);
                float facing = saturate(dot(normal, halfway));
                half3 glint = (pow(facing, 100.0) * 0.1 + pow(facing, 1500.0) * 3.0) * light.color;

                half3 color = lerp(water, sky, fresnel * _ReflectionStrength) + glint;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
