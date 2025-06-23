Shader "Custom/TMP_SoftGlow"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        [HDR] _FaceColor ("Face Color", Color) = (1,1,1,1)
        _FaceDilate ("Face Dilate", Range(-1,1)) = 0
        
        [HDR] _GlowColor ("Glow Color", Color) = (0.4, 0.8, 1, 0.5)
        _GlowOffset ("Glow Offset", Range(-1,1)) = -0.4
        _GlowInner ("Glow Inner", Range(0,1)) = 0.1
        _GlowOuter ("Glow Outer", Range(0,1)) = 0.4
        _GlowPower ("Glow Power", Range(1, 4)) = 2
        
        _WeightNormal ("Weight Normal", float) = 0
        _WeightBold ("Weight Bold", float) = 0.5
        
        _ScaleRatioA ("Scale RatioA", float) = 1
        _ScaleRatioB ("Scale RatioB", float) = 1
        _ScaleRatioC ("Scale RatioC", float) = 1
        
        _VertexOffsetX ("Vertex OffsetX", float) = 0
        _VertexOffsetY ("Vertex OffsetY", float) = 0
        
        _ClipRect ("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
        _MaskSoftnessX ("Mask SoftnessX", float) = 0
        _MaskSoftnessY ("Mask SoftnessY", float) = 0
        
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
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
                half4 mask : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _FaceColor;
            float _FaceDilate;
            
            fixed4 _GlowColor;
            float _GlowOffset;
            float _GlowInner;
            float _GlowOuter;
            float _GlowPower;
            
            float _WeightNormal;
            float _WeightBold;
            float _ScaleRatioA;
            float _ScaleRatioB;
            float _ScaleRatioC;
            
            float _VertexOffsetX;
            float _VertexOffsetY;
            
            float4 _ClipRect;
            float _MaskSoftnessX;
            float _MaskSoftnessY;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                float4 vPosition = v.vertex;
                vPosition.x += _VertexOffsetX;
                vPosition.y += _VertexOffsetY;
                
                o.worldPosition = vPosition;
                o.vertex = UnityObjectToClipPos(vPosition);
                
                float2 pixelSize = o.vertex.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                
                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                o.mask = half4(vPosition.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_MaskSoftnessX, _MaskSoftnessY) + abs(pixelSize.xy)));
                
                o.color = v.color * _FaceColor;
                o.texcoord0 = TRANSFORM_TEX(v.texcoord0, _MainTex);
                o.texcoord1 = v.texcoord1;
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Distance Field 샘플링
                half d = tex2D(_MainTex, i.texcoord0).a;
                
                // 기본 텍스트 계산
                half weight = lerp(_WeightNormal, _WeightBold, step(i.texcoord1.y, 0));
                weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;
                
                half sd = (0.5 - weight) - d;
                half faceAlpha = 1.0 - saturate(sd + 0.5);
                
                // 글로우 계산 - 텍스트 바깥쪽에만
                half glowDist = d + _GlowOffset;
                
                // 글로우는 텍스트 바깥 영역에만 적용
                half glow = 0;
                if (faceAlpha < 0.1) // 텍스트가 없는 영역에만 글로우
                {
                    half glow1 = saturate((glowDist + _GlowInner) / _GlowInner);
                    half glow2 = saturate((glowDist + _GlowOuter) / _GlowOuter);
                    
                    glow = lerp(glow2, glow1, 0.3);
                    glow = smoothstep(0.0, 1.0, glow);
                    glow = pow(glow, _GlowPower);
                }
                
                // 최종 색상 - 텍스트가 우선, 글로우는 배경에만
                fixed4 faceColor = i.color;
                faceColor.a *= faceAlpha;
                
                fixed4 glowColor = _GlowColor;
                glowColor.a *= glow;
                
                // 텍스트가 있으면 텍스트, 없으면 글로우 
                fixed4 color = lerp(glowColor, faceColor, faceAlpha);
                
                // UI 마스킹
                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}