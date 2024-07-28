sampler screen : register(s0);
float offset;

float4 CA(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = float4(0.0, 0.0, 0.0, 0.0);

    color.r = tex2D(screen, float2(coords.x + offset, coords.y)).r;
    color.g = tex2D(screen, coords).g;
    color.b = tex2D(screen, float2(coords.x - offset, coords.y)).b;
    color.a = tex2D(screen, coords).a;

    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 CA();
    }
}