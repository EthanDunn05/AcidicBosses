float2 uScreenResolution;

sampler screen : register(s0);
float3 tint;

static const float PI = 3.1415;

float4 BossRage(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(screen, coords);

    // Vignette
    float2 position = (coords / uScreenResolution);
    float distance = length(coords - float2(0.5, 0.5));

    float radius = 1.2;
    float softness = 0.7;
    float vignette = 1.0 - smoothstep(radius, radius - softness, distance);

    // Tint
    color.rgb *= tint;
    color.rgb -= vignette;

    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 BossRage();
    }
}