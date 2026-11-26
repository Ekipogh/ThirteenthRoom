Shader "Hidden/ThirteenthRoom/InventoryBlur"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        float _SampleRadius;

        half4 Blur(Varyings input, float2 direction)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 step = direction * _BlitTexture_TexelSize.xy * _SampleRadius;
            float2 uv = input.texcoord;
            half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv) * 0.227027;
            color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + step * 1.384615) * 0.316216;
            color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - step * 1.384615) * 0.316216;
            color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + step * 3.230769) * 0.070270;
            color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - step * 3.230769) * 0.070270;
            return color;
        }
        half4 Horizontal(Varyings input) : SV_Target { return Blur(input, float2(1, 0)); }
        half4 Vertical(Varyings input) : SV_Target { return Blur(input, float2(0, 1)); }
        ENDHLSL
        Pass
        {
            Name "Horizontal"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Horizontal
            ENDHLSL
        }
        Pass
        {
            Name "Vertical"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Vertical
            ENDHLSL
        }
    }
}
