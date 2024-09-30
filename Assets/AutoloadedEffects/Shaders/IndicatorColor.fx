sampler sprite : register(s0);
float4 color;
float globalTime;

// This shader applies colors in a weird way that looks nice

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 texColor = tex2D(sprite, coords);

    float brightness = texColor.r;

    float x = smoothstep(1.4, 0.75, brightness);
    float4 overlay = lerp(float4(1.0, 1.0, 1.0, color.a), color, x);

    return overlay * texColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}