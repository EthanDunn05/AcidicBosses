sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity : register(C0);
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float4 uShaderSpecificData;
int uFrames;

float screen(float a, float b) 
{
    return 1.0 - (1.0 - a) * (1.0 - b);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float speed = 10.0;
    float scale = 50.0;

    float4 color = tex2D(uImage0, coords);

    coords.y = (coords.y * asfloat(uFrames)) % 1.0;

    float intensity = (sin(uTime * speed + ((coords.x - 0.5) * (coords.y - 0.5)) * scale) + 1.0) * 0.5;
    float shieldPower = max(intensity * 0.75, 0.3);

    float3 shieldOverlay = float3(0.0, 0.5 * shieldPower, 1.0 * shieldPower);

    color.r = screen(color.r, shieldOverlay.r);
    color.g = screen(color.g, shieldOverlay.g);
    color.b = screen(color.b, shieldOverlay.b);

    float light = max(max(uSecondaryColor.r, uSecondaryColor.g), uSecondaryColor.b);
    
    color.rgb *= light;
    color.rgb *= color.a;

    color.a *= (256.0 - uOpacity) / 256.0;
    
    return color;
}

technique Technique1
{
    pass ShieldPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}