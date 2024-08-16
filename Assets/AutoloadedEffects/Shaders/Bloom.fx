// Parameters proveded by Luminance
sampler screen : register(s0);
int globalTime;
float opacity;
float2 focusPosition;
float2 screenPosition;
float2 textureSize0;

float weights[8] = { 0.1974, 0.1747, 0.1210, 0.0656, 0.0278, 0.0092, 0.0024, 0.0005 };

// Frag's color with a blur applied
float4 GaussianBlur(float2 coords : TEXCOORD0) : COLOR0 
{
    float2 fragSize = 2.0 / textureSize0;
    float4 result = tex2D(screen, coords) * weights[0];

    // Horizontal Pass
    for (int i = 1; i < 8; i++) 
    {
        result += tex2D(screen, coords + float2(fragSize.x * i, 0.0)) * weights[i];
        result += tex2D(screen, coords - float2(fragSize.x * i, 0.0)) * weights[i];
    }

    // Vertical Pass
    for (int i = 1; i < 8; i++) 
    {
        result += tex2D(screen, coords + float2(0.0, fragSize.x * i)) * weights[i];
        result += tex2D(screen, coords - float2(0.0, fragSize.x * i)) * weights[i];
    }
    return result;
}

// Applies bloom to the whole screen. For use with a render target for isolating bloom.
// This is not meant for use on the main render target.
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float gamma = 2.2;
    float exposure = 5.0;

    // Apply the blur
    float4 color = tex2D(screen, coords);
    float4 blur = GaussianBlur(coords);

    if (color.a < 0.1) color = blur;

    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}