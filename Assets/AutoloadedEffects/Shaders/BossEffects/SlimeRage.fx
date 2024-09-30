sampler slimeTex : register(s0);
sampler noiseTex : register(s1);
float globalTime;
float4 lightColor;

float intensity = 0.05;
float scale = 0.025;
float speed = 1;

float4 SampleNoise(float2 coords)
{
    coords.x += globalTime * speed;
    coords.y += globalTime * speed;
    float4 noise = tex2D(noiseTex, coords * scale);

    return noise;
}

float2 Distort(float2 coords, float2 noiseCoords)
{
    float2 noise = SampleNoise(noiseCoords).xy;

    float2 offset = (-1.0 + noise * 2.0) * intensity;
    coords += offset;

    return coords;
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    // Account for KS frames
    int frame = floor(coords.y * 6.0);
    coords.y = coords.y * 6.0 % 1.0;

    float2 noiseCoords = coords;

    coords = Distort(coords, noiseCoords);
    coords = Distort(coords, noiseCoords + 0.1);
    coords = Distort(coords, noiseCoords + 0.2);

    coords.x = clamp(coords.x, 0, 1);
    coords.y = clamp(coords.y, 0, 1);

    coords.y = (coords.y / 6.0 + frame / 6.0);

    float4 color = tex2D(slimeTex, coords);
    color.r /= 0.5;
    color.bg *= float2(1.0, 1.0) * 0.3;
    color.rgb *= lightColor.rgb;
    color.a = tex2D(slimeTex, coords).a * 0.75;

    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}