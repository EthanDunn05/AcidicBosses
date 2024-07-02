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
    // The name of each shader. Must match filename
    public struct Names
    {
        public const string Shockwave = "Shockwave";
        public const string BossRage = "BossRage";
        public const string ChromaticAberration = "ChromaticAbberation"; // Yes this is misspelled
        public const string KsCrownLaser = "KsCrownLaser";
        public const string BasicTexture = "BasicTexture";
        public const string UndergroundOutline = "UndergroundOutline";
        public const string SlimeRage = "SlimeRage";
    }

    // Screen shaders
    public static Filter Shockwave => Filters.Scene[Names.Shockwave];
    public static Filter BossRage => Filters.Scene[Names.BossRage];
    public static Filter ChromaticAberration => Filters.Scene[Names.ChromaticAberration];
    
    // Misc Shaders
    public static MiscShaderData KsCrownLaser => GameShaders.Misc[Names.KsCrownLaser];
    public static MiscShaderData BasicTexture => GameShaders.Misc[Names.BasicTexture];
    public static MiscShaderData UndergroundOutline => GameShaders.Misc[Names.UndergroundOutline];
    public static MiscShaderData SlimeRage => GameShaders.Misc[Names.SlimeRage];
    
    // Load Shaders
    private static AssetRepository assets; // To avoid passing the asset repository around functions

    /// <summary>
    /// Loads all shaders from into assets
    /// </summary>
    /// <param name="assets">The game's asset repository</param>
    public static void LoadShaders(AssetRepository assets)
    {
        EffectsRegistry.assets = assets;
        
        LoadScreenShader(Names.Shockwave, EffectPriority.High);
        LoadScreenShader(Names.BossRage, EffectPriority.VeryHigh);
        LoadScreenShader(Names.ChromaticAberration, EffectPriority.High);
        
        LoadPrimShader(Names.BasicTexture, "TrailPass");
        
        LoadBossShader(Names.UndergroundOutline, "UndergroundOutlinePass");
        LoadBossShader(Names.SlimeRage, "SlimeRagePass");
    }
    
    // Loads a shader from the file
    private static Ref<Effect> LoadEffect(string name)
    {
        var asset = new Ref<Effect>(assets.Request<Effect>("Assets/Effects/" + name, AssetRequestMode.ImmediateLoad).Value);
        return asset;
    }

    // Loads a shader from the PrimitiveShaders folder
    private static void LoadPrimShader(string name, string pass)
    {
        var effect = LoadEffect($"PrimitiveShaders/{name}");
        GameShaders.Misc[name] = new MiscShaderData(effect, pass);
    }

    // Loads a shader from the BossEffects folder
    private static void LoadBossShader(string name, string pass)
    {
        var effect = LoadEffect($"BossEffects/{name}");
        GameShaders.Misc[name] = new MiscShaderData(effect, pass);
    }

    // Loads a shader from the base folder
    private static void LoadMiscShader(string name, string pass)
    {
        var effect = LoadEffect($"{name}");
        GameShaders.Misc[name] = new MiscShaderData(effect, pass);
    }

    // Loads a screen shader from the base folder
    private static void LoadScreenShader(string name, EffectPriority priority)
    {
        var effect = LoadEffect(name);
        Filters.Scene[name] = new Filter(new ScreenShaderData(effect, name), priority);
        Filters.Scene[name].Load();
    }
}