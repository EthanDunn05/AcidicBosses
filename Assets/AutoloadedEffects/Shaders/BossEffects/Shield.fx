sampler npcTex : register(s0);
float3 lighting;
float globalTime;
float opacity;
int texFrames;

float screen(float a, float b) 
{
    return 1.0 - (1.0 - a) * (1.0 - b);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float speed = 10.0;
    float scale = 50.0;

    float4 color = tex2D(npcTex, coords);

    coords.y = (coords.y * asfloat(texFrames)) % 1.0;

    float intensity = (sin(globalTime * speed + ((coords.x - 0.5) * (coords.y - 0.5)) * scale) + 1.0) * 0.5;
    float shieldPower = max(intensity * 0.75, 0.3);

    float3 shieldOverlay = float3(0.0, 0.5 * shieldPower, 1.0 * shieldPower);

    color.r = screen(color.r, shieldOverlay.r);
    color.g = screen(color.g, shieldOverlay.g);
    color.b = screen(color.b, shieldOverlay.b);

    float light = max(max(lighting.r, lighting.g), lighting.b);
    
    color.rgb *= light;
    color.rgb *= color.a;

    color.a *= (256.0 - opacity) / 256.0;
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}