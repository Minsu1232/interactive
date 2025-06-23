Shader "UI/TextGradientGlow"
{
    Properties
    {
        _MainTex ("Font Texture", 2D) = "white" {}
        _TopColor ("Top Color", Color) = (0.4, 0.49, 0.92, 1)     // #667eea
        _BottomColor ("Bottom Color", Color) = (0.46, 0.29, 0.64, 1) // #764ba2
        _GlowColor ("Glow Color", Color) = (0.4, 0.49, 0.92, 0.5)
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 1.0
        _GlowSize ("Glow Size", Range(0, 1)) = 0.1
        _AnimationSpeed ("Animation Speed", Range(0, 5)) = 2.0
        
        // UI용 설정
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UI_TextGradientGlow"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Properties
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TopColor;
                float4 _BottomColor;
                float4 _GlowColor;
                float _GlowIntensity;
                float _GlowSize;
                float _AnimationSpeed;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = v.uv;
                
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // 텍스처 샘플링
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                
                // 그라데이션 계산 (UV Y좌표 기반)
                float gradientFactor = i.worldPos.y;
                float4 gradientColor = lerp(_BottomColor, _TopColor, gradientFactor);
                
                // 기본 텍스트 색상 (그라데이션 * 텍스처 알파)
                float4 baseColor = gradientColor * texColor.a;
                
                // 글로우 효과 계산
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.worldPos, center);
                float glow = 1.0 - smoothstep(0.0, _GlowSize, dist);
                glow = pow(glow, 2.0);
                
                // 애니메이션 펄스
                float time = _Time.y * _AnimationSpeed;
             float pulse = sin(time) * 0.5 + 1.0; // 0.5 ~ 1.5 범위로 더 크게
glow *= pulse * _GlowIntensity;
                
                // 글로우 색상
                float4 glowEffect = _GlowColor * glow;
                
                // 최종 색상 조합
                float4 finalColor = baseColor + glowEffect;
                finalColor.rgb *= i.color.rgb; // 버텍스 컬러 적용
                finalColor.a = texColor.a * i.color.a;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    Fallback "UI/Default"
}