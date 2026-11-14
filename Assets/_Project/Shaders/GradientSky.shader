Shader "Anthem/GradientSky"
{
    Properties
    {
        _Horizon ("Horizon (match fog)", Color) = (0.5, 0.27, 0.11, 1)
        _Zenith ("Zenith", Color) = (0.3, 0.15, 0.07, 1)
        _Ground ("Ground", Color) = (0.4, 0.2, 0.09, 1)
        _SunGlow ("Sun Glow", Color) = (1, 0.62, 0.3, 1)
        _SunDir ("Sun Dir", Vector) = (0, 0.2, 1, 0)
        _GlowSize ("Glow Tightness", Range(1, 64)) = 5
        _MoonTex ("Moon Texture", 2D) = "white" {}
        [HDR] _MoonColor ("Moon Color", Color) = (2.2, 1.5, 0.8, 1)
        _MoonDir ("Moon Dir", Vector) = (0.25, 0.4, 1, 0)
        _MoonSize ("Moon Radius (rad)", Range(0.01, 0.3)) = 0.075
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Horizon, _Zenith, _Ground, _SunGlow;
            float4 _SunDir;
            float _GlowSize, _MoonSize;
            sampler2D _MoonTex;
            float4 _MoonDir, _MoonColor;

            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float up = saturate(d.y);
                fixed3 c = lerp(_Horizon.rgb, _Zenith.rgb, pow(up, 0.6));
                c = lerp(c, _Ground.rgb, saturate(-d.y * 4));
                float s = saturate(dot(d, normalize(_SunDir.xyz)));
                c += _SunGlow.rgb * (pow(s, _GlowSize) * 0.9 + pow(s, _GlowSize * 24) * 1.5);

                // moon: hard-edged textured disc; the haze colour still tints it a little
                float3 m = normalize(_MoonDir.xyz);
                float3 mr = normalize(cross(float3(0, 1, 0), m)), mu = cross(m, mr);
                float2 uv = float2(dot(d, mr), dot(d, mu)) / _MoonSize;
                float inside = step(dot(uv, uv), 1) * step(0, dot(d, m));
                fixed3 moon = tex2D(_MoonTex, uv * 0.5 + 0.5).rgb * _MoonColor.rgb;
                c = lerp(c, moon + c * 0.25, inside);
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
}
