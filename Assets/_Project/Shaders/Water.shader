// Cheap stylised lake water: dark up close, picks up the haze colour at grazing angles,
// two scrolling ripple layers, a broken moon glitter streak, point-light glints and a soft shoreline.
Shader "Anthem/Water"
{
    Properties
    {
        _Deep ("Deep Color", Color) = (0.02, 0.015, 0.01, 1)
        _Haze ("Reflected Haze (match fog)", Color) = (0.5, 0.31, 0.16, 1)
        [HDR] _Glint ("Moon Glint", Color) = (3, 1.9, 0.9, 1)
        _MoonDir ("Moon Dir", Vector) = (0.25, 0.45, 0.85, 0)
        _BumpMap ("Ripples", 2D) = "bump" {}
        _Tile ("Ripple Tile (1/m)", Float) = 0.25
        _Strength ("Ripple Strength", Float) = 0.35
        _Speed ("Scroll Speed", Float) = 0.03
        _Gloss ("Glint Tightness", Float) = 180
        _Fade ("Shore Fade (m)", Float) = 0.35
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _Deep, _Haze, _Glint;
                float4 _MoonDir;
                float _Tile, _Strength, _Speed, _Gloss, _Fade;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.positionWS.xz * _Tile;
                float t = _Time.y * _Speed;
                half3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv + float2(t, t * 0.6)));
                half3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv * 1.7 + float2(-t * 0.8, t * 0.5)));
                float2 tilt = (n1.xy + n2.xy) * _Strength;
                float3 n = normalize(float3(tilt.x, 1, tilt.y));

                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float fres = pow(1 - saturate(dot(n, v)), 4);
                half3 c = lerp(_Deep.rgb, _Haze.rgb, saturate(fres * 1.2));

                float3 r = reflect(-v, n);
                c += _Glint.rgb * pow(saturate(dot(r, normalize(_MoonDir.xyz))), _Gloss);

                // glints from the glow bulbs / flower lights
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light l = GetAdditionalLight(lightIndex, i.positionWS);
                    c += l.color * l.distanceAttenuation * pow(saturate(dot(r, l.direction)), 60) * 0.6;
                LIGHT_LOOP_END

                // soft shoreline: fade out where the lake bed is just under the surface
                float2 suv = i.positionCS.xy / _ScaledScreenParams.xy;
                float scene = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float surf = LinearEyeDepth(i.positionCS.z, _ZBufferParams);
                float a = saturate((scene - surf) / _Fade);

                c = MixFog(c, i.fogFactor);
                return half4(c, a * 0.97);
            }
            ENDHLSL
        }
    }
}
