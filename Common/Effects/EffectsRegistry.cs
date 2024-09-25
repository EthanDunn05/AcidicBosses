using Luminance.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
// ReSharper disable MemberHidesStaticFromOuterClass

namespace AcidicBosses.Common.Effects;

/// <summary>
/// For registering a shader make sure to do ALL 3 of these things:
/// 1. Add the name to the Names struct
/// 2. Add a variable that gets the shader data
/// 3. Load the shader in the LoadShaders function
/// 
/// It's also a good idea to make a function in EffectsRegistry.cs
/// for making the use of the shader easier.
/// </summary>
public static class EffectsRegistry
{
    public static ManagedScreenFilter Shockwave => ScreenShader("Shockwave");
    public static ManagedScreenFilter BossRage => ScreenShader("BossRage");
    public static ManagedScreenFilter ChromaticAberration => ScreenShader("ChromaticAbberation");
    public static ManagedShader UndergroundOutline => Shader("UndergroundOutline");
    public static ManagedShader SlimeRage => Shader("SlimeRage");
    public static ManagedShader Shield => Shader("Shield");
    public static ManagedShader Bloom => Shader("Bloom");
    public static ManagedShader IndicatorColor => Shader("IndicatorColor");

    private static ManagedShader Shader(string name)
    {
        return ShaderManager.GetShader($"AcidicBosses.{name}");
    }

    private static ManagedScreenFilter ScreenShader(string name)
    {
        return ShaderManager.GetFilter($"AcidicBosses.{name}");
    }
}