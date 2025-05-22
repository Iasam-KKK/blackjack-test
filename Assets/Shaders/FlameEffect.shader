Shader "Custom/FlameEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 0.5, 0, 1)
        _SecondaryColor ("Secondary Color", Color) = (1, 0, 0, 1)
        _StreakLevel ("Streak Level", Range(0, 5)) = 0
        _Speed ("Speed", Range(0, 10)) = 1
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.5
        _FlameHeight ("Flame Height", Range(0, 2)) = 1
        _FlameWidth ("Flame Width", Range(0, 2)) = 1
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
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
            
            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _SecondaryColor;
            float _StreakLevel;
            float _Speed;
            float _NoiseStrength;
            float _FlameHeight;
            float _FlameWidth;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            float4 frag (v2f i) : SV_Target
            {
                // Calculate time-based offset for animation
                float timeOffset = _Time.y * _Speed;
                
                // Get noise value for flame distortion
                float2 noiseUV = i.uv + float2(sin(timeOffset * 0.1), timeOffset * 0.2);
                float noise = tex2D(_NoiseTex, noiseUV).r;
                
                // Adjust UV based on noise
                float2 distortedUV = i.uv;
                distortedUV.x += (noise - 0.5) * _NoiseStrength * (0.2 + _StreakLevel * 0.1);
                
                // Base flame shape (stronger at the bottom, weaker at top)
                float yFactor = 1.0 - i.uv.y;
                
                // Adjust flame width based on streak level
                float width = _FlameWidth * (1.0 + _StreakLevel * 0.2);
                float xFactor = 1.0 - abs((i.uv.x - 0.5) * 2.0) / width;
                
                // Adjust flame height based on streak level
                float height = _FlameHeight * (1.0 + _StreakLevel * 0.3);
                yFactor = pow(yFactor * height, 1.0 / (1.0 + _StreakLevel * 0.2));
                
                // Calculate flame intensity
                float flameIntensity = yFactor * xFactor;
                flameIntensity = saturate(flameIntensity + (noise - 0.5) * _NoiseStrength);
                
                // Add flickering effect
                float flicker = sin(timeOffset * 5.0) * 0.1 + 0.9;
                flameIntensity *= flicker;
                
                // Enhance intensity based on streak level
                flameIntensity = pow(flameIntensity, 1.0 / (1.0 + _StreakLevel * 0.2));
                
                // Blend between flame colors based on streak level and position
                float colorBlend = i.uv.y + (_StreakLevel * 0.1);
                float4 flameColor = lerp(_Color, _SecondaryColor, colorBlend);
                
                // Final color
                float4 finalColor = flameColor;
                finalColor.a = flameIntensity;
                
                // Add brightness based on streak level
                finalColor.rgb *= 1.0 + (_StreakLevel * 0.2);
                
                return finalColor;
            }
            ENDCG
        }
    }
} 