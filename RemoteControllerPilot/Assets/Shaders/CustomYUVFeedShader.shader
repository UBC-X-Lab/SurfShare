// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Simple shader mapping a YUV video feed without any lighting model.
Shader "Video/CustomYUVFeedShader"
{
    Properties
    {
        [Toggle(MIRROR)] _Mirror("Horizontal Mirror", Float) = 0
        [HideInEditor][NoScaleOffset] _YPlane("Y plane", 2D) = "black" {}
        [HideInEditor][NoScaleOffset] _UPlane("U plane", 2D) = "grey" {}
        [HideInEditor][NoScaleOffset] _VPlane("V plane", 2D) = "grey" {}

        [HideInEditor][NoScaleOffset] _FirstYPlane("First Y plane", 2D) = "black" {}
        [HideInEditor][NoScaleOffset] _FirstUPlane("First U plane", 2D) = "grey" {}
        [HideInEditor][NoScaleOffset] _FirstVPlane("First V plane", 2D) = "grey" {}

        // [HideInEditor][NoScaleOffset] _Background("Background", 2D) = "black" {} // a rgb texture storing the background
    }
    
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Opaque" }
        CGPROGRAM

        #pragma surface surf Lambert alpha
        #pragma multi_compile_instancing
        #pragma multi_compile __ MIRROR

        struct Input
        {
            float2 uv_YPlane;
        };

        sampler2D _YPlane;
        sampler2D _UPlane;
        sampler2D _VPlane;
        // sampler2D _Background;
        sampler2D _FirstYPlane;
        sampler2D _FirstUPlane;
        sampler2D _FirstVPlane;

        half3 yuv2rgb(half3 yuv)
        {
            // The YUV to RBA conversion, please refer to: http://en.wikipedia.org/wiki/YUV
            // Y'UV420p (I420) to RGB888 conversion section.
            half y_value = yuv[0];
            half u_value = yuv[1];
            half v_value = yuv[2];
            half r = y_value + 1.370705 * (v_value - 0.5);
            half g = y_value - 0.698001 * (v_value - 0.5) - (0.337633 * (u_value - 0.5));
            half b = y_value + 1.732446 * (u_value - 0.5);
            return half3(r, g, b); // this out puts rgb in [0, 1]
        }

        inline half3 rgb2hsv(half3 rgb)
        {
            half R = rgb[0];
            half G = rgb[1];
            half B = rgb[2];
            half X_max = max(R, max(G, B)); // x_max is V
            half X_min = min(R, min(G, B));
            half C = X_max - X_min;

            half H;
            if (C == 0.0) {
                H = 0.0;
            }
            else if (X_max == R)
            {
                H = 1.0 / 6.0 * (G - B) / C;
            }
            else if (X_max == G)
            {
                H = 1.0 / 6.0 * (2 + (B - R) / C);
            }
            else if (X_max == B)
            {
                H = 1.0 / 6.0 * (4 + (R - G) / C);
            }

            half S;
            if (X_max == 0) {
                S = 0;
            }
            else {
                S = C / X_max;
            }

            return half3(H, S, X_max);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            half3 yuv;

            // Flip texture coordinates vertically.
            // Texture2D.LoadRawTextureData() always expects a bottom-up image, but the MediaPlayer
            // upload code always get a top-down frame from WebRTC. The most efficient is to upload
            // as is (inverted) and revert here.
            IN.uv_YPlane.y = 1 - IN.uv_YPlane.y;

#ifdef MIRROR
            // Optional left-right mirroring (horizontal flipping)
            IN.uv_YPlane.x = 1 - IN.uv_YPlane.x;
#endif

            float threshold = 0.045;
            yuv.x = tex2D(_YPlane, IN.uv_YPlane).r;
            yuv.y = tex2D(_UPlane, IN.uv_YPlane).r;
            yuv.z = tex2D(_VPlane, IN.uv_YPlane).r;

            half3 bg_yuv;
            bg_yuv.x = tex2D(_FirstYPlane, IN.uv_YPlane).r;
            bg_yuv.y = tex2D(_FirstUPlane, IN.uv_YPlane).r;
            bg_yuv.z = tex2D(_FirstVPlane, IN.uv_YPlane).r;

            half3 rgb = yuv2rgb(yuv);
            // half3 bg_rgb = tex2D(_Background, i.uv).rgb;
            half3 bg_rgb = yuv2rgb(bg_yuv);

            half3 hsv = rgb2hsv(rgb);
            half3 bg_hsv = rgb2hsv(bg_rgb);

            // return rgb;
            // return bg_rgb;

            if (abs(hsv[0] - bg_hsv[0]) > threshold && abs(hsv[1] - bg_hsv[1]) > threshold) { // foreground
                o.Albedo = rgb;
                o.Alpha = 1;
            }
            else {
                o.Albedo = half3(0, 0, 0);
                o.Alpha = 0;
            }
        }

        ENDCG
     }

     Fallback "Diffuse"
}