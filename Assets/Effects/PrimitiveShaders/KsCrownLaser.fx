sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float2 uImageSize2;
matrix uWorldViewProjection;
float4 uShaderSpecificData;

float uDistance;

float stretchAmount;
float scrollSpeed;
bool reverseDirection;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

// Converts trail coordinates to coordinates on a deathray sprite
float2 DeathrayCoords(float rayLength, int imageLength, float2 coords)
{
    int stepSize = imageLength / 3.0;
    int steps = rayLength / stepSize;
    float step = coords.x * steps - 1;
    float stepProgress = step % 1.0;

    float2 DeathrayCoords = coords;
    
    // Head
    if(step < 1.0)
    {
        DeathrayCoords.x = stepProgress / 3.0;
    }
    // Tail
    else if(step > steps - 2) 
    {
        DeathrayCoords.x = (2 + stepProgress) / 3.0;
    }
    // Body
    else
    {
        DeathrayCoords.x = (1 + stepProgress) / 3.0;
    }

    return DeathrayCoords;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float rayLength = uDistance;
    int imageLength = uImageSize1.x;

    float2 deathrayCoords = DeathrayCoords(rayLength, imageLength, coords);
    float4 deathrayMap = tex2D(uImage1, deathrayCoords);

    float4 color = deathrayMap;
    color.a *= deathrayMap.r;

    float brightness = (sin(coords.x * 100.0 - uTime * 10.0) + 1.0) / 4.0 + 0.5;
    color.rgb *= float3(brightness, 0.1, 0.1);

    return color;
}

technique Technique1
{
    pass TrailPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}