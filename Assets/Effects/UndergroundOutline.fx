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

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);

    float2 up = float2(0, 1 / uImageSize0.y); 
    float2 right = float2(1 / uImageSize0.x, 0); 

    float px_up = tex2D(uImage0, coords + up).a;
    float px_down = tex2D(uImage0, coords - up).a;
    float px_right = tex2D(uImage0, coords + right).a;
    float px_left = tex2D(uImage0, coords - right).a;

    float light = max(max(uSecondaryColor.r, uSecondaryColor.g), uSecondaryColor.b);
    
    //float outline = max(max(px_up, px_down), max(px_left, px_right)) - color.a;
    float outline = (1 - px_up * px_down * px_left * px_right) * color.a * (1 - light);

    float4 outline_color = float4(uColor, 1 - light);

    color.rgb *= uSecondaryColor;

    return lerp(color, outline_color, outline);
}

technique Technique1
{
    pass UndergroundOutlinePass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}