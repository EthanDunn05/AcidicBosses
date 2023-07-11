sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);
float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;

const float PI = 3.1415;

float4 BossRage(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);

    // Vignette
    float2 position = (coords / uScreenResolution);
    float distance = length(coords - float2(0.5, 0.5));

    float radius = 1;
    float softness = 0.8;
    float vignette = 1.0 - smoothstep(radius, radius - softness, distance);

    // Tint
    color.rgb *= uColor;
    color.rgb -= vignette;

    return color;
}

technique Technique1
{
    pass BossRage
    {
        PixelShader = compile ps_2_0 BossRage();
    }
}