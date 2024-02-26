using System.IO;
using AcidicBosses.Common.Effects;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses;

partial class AcidicBosses : Mod
{
    public override void Load()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            LoadScreenShader(EffectsRegistry.Names.Shockwave, EffectPriority.High);
            LoadScreenShader(EffectsRegistry.Names.BossRage, EffectPriority.VeryHigh);
            LoadPrimShader(EffectsRegistry.Names.BasicTexture, "TrailPass");
            LoadBossShader(EffectsRegistry.Names.UndergroundOutline, "UndergroundOutlinePass");
            LoadBossShader(EffectsRegistry.Names.SlimeRage, "SlimeRagePass");
        }
    }

    private Ref<Effect> LoadEffect(string name)
    {
        var asset = new Ref<Effect>(Assets.Request<Effect>("Assets/Effects/" + name, AssetRequestMode.ImmediateLoad).Value);
        return asset;
    }

    private void LoadPrimShader(string name, string pass)
    {
        var effect = LoadEffect($"PrimitiveShaders/{name}");
        GameShaders.Misc[name] = new MiscShaderData(effect, pass);
    }

    private void LoadBossShader(string name, string pass)
    {
        var effect = LoadEffect($"BossEffects/{name}");
        GameShaders.Misc[name] = new MiscShaderData(effect, pass);
    }

    private void LoadMiscShader(string name, string pass)
    {
        var effect = LoadEffect($"{name}");
        GameShaders.Misc[name] = new MiscShaderData(effect, pass);
    }

    private void LoadScreenShader(string name, EffectPriority priority)
    {
        var effect = LoadEffect(name);
        Filters.Scene[name] = new Filter(new ScreenShaderData(effect, name), priority);
        Filters.Scene[name].Load();
    }
}