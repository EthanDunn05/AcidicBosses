using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses;

public class AcidicBosses : Mod
{
    public override void Load()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            var shockwaveRef = LoadEffect("Shockwave", EffectPriority.High);
            var bossrageRef = LoadEffect("BossRage", EffectPriority.VeryHigh);
            var neonboxRef = LoadEffect("NeonBox", EffectPriority.High);
        }
    }

    private Ref<Effect> LoadEffect(string name, EffectPriority priority)
    {
        var asset = new Ref<Effect>(Assets.Request<Effect>("Effects/" + name, AssetRequestMode.ImmediateLoad).Value);
        Filters.Scene[name] = new Filter(new ScreenShaderData(asset, name), priority);
        Filters.Scene[name].Load();
        return asset;
    }
}