sampler npcTex : register(s0);
float2 textureSize0;
float3 outlineColor;
float3 lightColor;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(npcTex, coords);

    float2 up = float2(0, 1 / textureSize0.y); 
    float2 right = float2(1 / textureSize0.x, 0); 

    float px_up = tex2D(npcTex, coords + up).a;
    float px_down = tex2D(npcTex, coords - up).a;
    float px_right = tex2D(npcTex, coords + right).a;
    float px_left = tex2D(npcTex, coords - right).a;

    float light = max(max(lightColor.r, lightColor.g), lightColor.b);
    
    // float outline = max(max(px_up, px_down), max(px_left, px_right)) - color.a;
    float outline = (1 - px_up * px_down * px_left * px_right) * color.a;

    float4 outline_color = float4(outlineColor, color.a);

    color.rgb *= lightColor;
    outline_color.rgb *= 0.75 - lightColor;
    if (light > 0.75) {
        outline = 0;
    }

    return lerp(color, outline_color, outline);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}