// Moves every vertex by the FFT displacement and lights it
Shader "Ocean/Surface"
{
    Properties
    {
        _DeepColor ("Trough Color", Color) = (0.02, 0.15, 0.25, 1)
        _ShallowColor ("Lit Color", Color) = (0.1, 0.45, 0.55, 1)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        // transparent queue so the camera depth texture already has the boat and shore, for the foam
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            // ----------------------------
            // TUNING

            static const float CREST_HEIGHT = 5.0; // m, wave height that counts as a full crest, for the glow
            static const float RIPPLE_FADE_DISTANCE = 150.0; // m, ripples fade out with distance
            static const float ROUGHNESS = 0.25; // how rough far water gets
            static const float ROUGHNESS_DISTANCE = 150.0; // m, distance where it is half as rough
            static const float GLINT_STRENGTH = 0.05;
            static const float REFRACTION = 0.05; // how far the surface tilt shifts what is underneath
            static const float ABSORPTION = 0.4; // per m, how fast the water hides what is underneath
            static const float HORIZON_FOG = 0.002;
            static const float FOAM_DISTANCE = 0.5; // m, how far from a solid object the foam reaches
            static const float FOAM_BREAKUP = 8.0; // how patchy the foam is


            // ----------------------------
            // INPUTS

            // set by Ocean.cs: tile 0 is the swells, tile 1 the ripples
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
                half4 _FoamColor;
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


            // ----------------------------
            // WAVE SHAPE

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


            // ----------------------------
            // SHADERS

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
                // surface direction: a flat grid bent by each tile, ripples fade with distance
                float viewDist = distance(_WorldSpaceCameraPos, input.positionWS);
                float3 alongX = float3(1.0, 0.0, 0.0);
                float3 alongZ = float3(0.0, 0.0, 1.0);
                AddTilt(TEXTURE2D_ARGS(_OceanDisplacement0, sampler_OceanDisplacement0), _OceanPatch0, _OceanTexel0, input.restXZ, 1.0, alongX, alongZ);
                AddTilt(TEXTURE2D_ARGS(_OceanDisplacement1, sampler_OceanDisplacement1), _OceanPatch1, _OceanTexel1, input.restXZ, saturate(1.0 - viewDist / RIPPLE_FADE_DISTANCE), alongX, alongZ);
                float3 normal = normalize(cross(alongZ, alongX));

                Light light = GetMainLight();
                float lightFacing = saturate(dot(normal, light.direction));
                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));

                // far water is rougher: the sheen spreads out and the sky reflection softens
                float roughness = 0.03 + ROUGHNESS * viewDist / (viewDist + ROUGHNESS_DISTANCE);

                // sky reflection: glancing angles mirror the sky, kept above the horizon
                float fresnel = 0.02 + 0.98 * pow(1.0 - saturate(dot(normal, view)), 5.0);
                float3 reflected = reflect(-view, normal);
                reflected.y = abs(reflected.y);
                half3 sky = GlossyEnvironmentReflection(reflected, saturate(roughness * 2.0), 1.0);

                // water color is scattered light: thin crests glow, most when facing the sun
                float height = Displaced(input.restXZ).y;
                float crest = saturate(0.5 + 0.5 * height / CREST_HEIGHT);
                float backlight = pow(saturate(dot(light.direction, -view)), 4.0);
                float scatter = saturate(crest * 0.5 + backlight * crest * 0.5 + lightFacing * 0.15);
                half3 water = lerp(_DeepColor.rgb, _ShallowColor.rgb, scatter) * (0.5 + 0.5 * light.color);

                // sun glint
                float3 halfway = normalize(light.direction + view);
                float facing = saturate(dot(normal, halfway));
                float sharpness = 2.0 / (roughness * roughness) - 2.0;
                half3 glint = pow(facing, sharpness) * (sharpness + 2.0) / (2.0 * PI) * fresnel * lightFacing * light.color * GLINT_STRENGTH;

                // what is under the water: the scene behind it, shifted by the surface tilt, hidden by the water color with depth
                float2 screenUV = input.positionCS.xy / _ScaledScreenParams.xy;
                float behind = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams) - input.positionCS.w;

                float2 refractedUV = screenUV + normal.xz * REFRACTION;
                if (LinearEyeDepth(SampleSceneDepth(refractedUV), _ZBufferParams) < input.positionCS.w)
                {
                    refractedUV = screenUV; // something is in front of the water, don't drag it in
                }
                half3 seen = lerp(SampleSceneColor(refractedUV), water, 1.0 - exp(-max(behind, 0.0) * ABSORPTION));

                half3 color = lerp(seen, sky, fresnel) + glint;

                // foam where the water meets something solid, the ripple heights, scrolling, break it into patches
                float edge = 1.0 - saturate(behind / FOAM_DISTANCE);
                float breakup = Displacement(TEXTURE2D_ARGS(_OceanDisplacement1, sampler_OceanDisplacement1), _OceanPatch1, input.restXZ + _Time.y * 0.3).y * FOAM_BREAKUP;
                float foam = smoothstep(0.5, 0.8, edge + breakup * edge);
                color = lerp(color, _FoamColor.rgb * (0.5 + 0.5 * light.color), foam);

                // far water fades into the horizon
                half3 horizon = GlossyEnvironmentReflection(normalize(float3(-view.x, 0.02, -view.z)), 0.5, 1.0);
                color = lerp(color, horizon, 1.0 - exp(-viewDist * HORIZON_FOG));
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
