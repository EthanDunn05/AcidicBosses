sampler screen : register(s0);
float globalTime;
float opacity;
float2 focusPosition;
float2 screenPosition;
float2 screenSize;

// Custom params
float2 targetPos;
float eventHorizonRadius;

float circleSdf(float2 pos, float radius) {
    return length(pos) - radius;
}

float4 BlackHole(float2 coords : TEXCOORD0) : COLOR0
{
    // The target position converted to screen UV
    float2 targetCoords = (targetPos - screenPosition) / screenSize;

    // The UV using the target as the center
    float2 centeredCoords = (coords - targetCoords) * (screenSize / screenSize.y);

    // The SDF of the black hole at this frag
    float blackHoleSdf = circleSdf(centeredCoords, eventHorizonRadius / screenSize.y);

    float darkness = smoothstep(0, 0.005, blackHoleSdf);
    float distortion = 1.0 - smoothstep(0, 0.25, blackHoleSdf);

    float2 offset = normalize(centeredCoords) * 0.07 * distortion;

    float4 color = tex2D(screen, coords - offset);

    color *= darkness;
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 BlackHole();
    }
}