sampler screen : register(s0);
float3 tintColor;
float amount;
float width;
float progress;
float2 targetPos;
float2 screenPosition;
float2 screenSize;

// Stolen from https://forums.terraria.org/index.php?threads/tutorial-shockwave-effect-for-tmodloader.81685/
float4 Shockwave(float2 coords : TEXCOORD0) : COLOR0
{
    float spread = progress * 2.0;

    float2 targetCoords = (targetPos - screenPosition) / screenSize;
    float2 centerCoords = (coords - targetCoords) * (screenSize / screenSize.y);

    float distance = length(centerCoords);

    // Map the ripple shape
    float outerMap = 1.0 - smoothstep(spread - width, spread, distance);
    float innerMap = smoothstep(spread - width * 2.0, spread - width, distance);
    float map = outerMap * innerMap;

    float2 displacement = normalize(centerCoords) * amount * map;

    float4 color = tex2D(screen, coords - displacement);

    color.rgb += tintColor * map * distance * (1.0 - progress);

    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 Shockwave();
    }
}