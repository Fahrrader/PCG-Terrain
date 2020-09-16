Shader "Custom/Terrain"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        const static int max_color_count = 8;
        const static float eps = 1e-4;
        
        int color_count;
        float3 colors[max_color_count];
        float start_heights[max_color_count];
        float blends[max_color_count];
        
        float min_height;
        float max_height;

        struct Input
        {
            float3 worldPos;
        };

        float inverse_lerp(float value, float a, float b)
        {
            return saturate((value - a)/(b - a));
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float height_percent = inverse_lerp(IN.worldPos.y, min_height, max_height);
            //o.Albedo = float3(.3, .7, .1);
            for (int i = 0; i < color_count; i++)
            {
                float draw_strength = inverse_lerp(height_percent - start_heights[i], -blends[i] / 2 - eps, blends[i] / 2);
                o.Albedo = o.Albedo * (1 - draw_strength) + colors[i] * draw_strength;
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}
