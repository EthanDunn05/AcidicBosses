sampler uImage0 : register(s0); // The contents of the screen.
sampler uImage1 : register(s1); // Up to three extra textures you can use for various purposes (for instance as an overlay).
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);
float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition; // The position of the camera.
float2 uTargetPosition; // The "target" of the shader, what this actually means tends to vary per shader.
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
float4 uSourceRect; // Doesn't seem to be used, but included for parity.
float2 uZoom;

// Stolen from https://forums.terraria.org/index.php?threads/tutorial-shockwave-effect-for-tmodloader.81685/
float4 Shockwave(float2 coords : TEXCOORD0) : COLOR0
{
    float amount =  uIntensity;
    float spread = uProgress * 2.0;
    float width = uOpacity;

    float2 targetCoords = (uTargetPosition - uScreenPosition) / uScreenResolution;
    float2 centerCoords = (coords - targetCoords) * (uScreenResolution / uScreenResolution.y);

    float distance = length(centerCoords);

    // Map the ripple shape
    float outerMap = 1.0 - smoothstep(spread - width, spread, distance);
    float innerMap = smoothstep(spread - width * 2.0, spread - width, distance);
    float map = outerMap * innerMap;

    float2 displacement = normalize(centerCoords) * amount * map;

    float4 color = tex2D(uImage0, coords - displacement);

    color.rgb += uColor * map * distance * (1.0 - uProgress);

    return color;
}

technique Technique1
{
    pass Shockwave
    {
        PixelShader = compile ps_2_0 Shockwave();
    }
}