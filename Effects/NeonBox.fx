sampler uImage0 : register(s0);
sampler uImage1 : register(s1); // Automatically Images/Misc/Perlin via Force Shader testing option
sampler uImage2 : register(s2); // Automatically Images/Misc/noise via Force Shader testing option
sampler uImage3 : register(s3);
float3 uColor; // Neon Color
float3 uSecondaryColor; // xy of source position
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition; // Target position
float2 uDirection;
float uOpacity; // Glow radius
float uTime;
float uIntensity; // Rectangle thickness
float uProgress; 
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation; 
float4 uSourceRect;
float2 uZoom;

//https://www.shadertoy.com/view/3s3GDn
float getGlow(float dist, float radius, float intensity){
    return pow(radius/dist, intensity);
}

// Adapted from https://iquilezles.org/articles/distfunctions2d/
float sdOrientedBox(float2 p, float2 a, float2 b, float th )
{
    float l = length(b - a);
    float2  d = (b - a) / l;
    float2  q = (p - (a + b) * 0.5);
    q = mul(float2x2(d.x, -d.y, d.y, d.x), q);
    q = abs(q) - float2(l,th) * 0.5;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float2 sourcePos = uSecondaryColor.xy;

    float2 position = coords * uScreenResolution;

    float dist = sdOrientedBox(position, sourcePos, uTargetPosition, uIntensity);
    float glow = getGlow(dist, uOpacity, 0.75);

    float3 color = tex2D(uImage0, coords);

    float core = 10.0 * smoothstep(0.006, 0.003, dist);
    color += core;
    color += glow * uColor;

    return float4(color, 1.0);
}

technique Technique1
{
    pass ModdersToolkitShaderPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}