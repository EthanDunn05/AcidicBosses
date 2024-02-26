using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;

namespace AcidicBosses.Common.Effects;

public static class EffectsRegistry
{
    public struct Names
    {
        public const string Shockwave = "Shockwave";
        public const string BossRage = "BossRage";
        public const string KsCrownLaser = "KsCrownLaser";
        public const string BasicTexture = "BasicTexture";
        public const string UndergroundOutline = "UndergroundOutline";
        public const string SlimeRage = "SlimeRage";
    }

    public static Filter Shockwave => Filters.Scene[Names.Shockwave];
    public static Filter BossRage => Filters.Scene[Names.BossRage];
    
    public static MiscShaderData KsCrownLaser => GameShaders.Misc[Names.KsCrownLaser];
    public static MiscShaderData BasicTexture => GameShaders.Misc[Names.BasicTexture];
    public static MiscShaderData UndergroundOutline => GameShaders.Misc[Names.UndergroundOutline];
    public static MiscShaderData SlimeRage => GameShaders.Misc[Names.SlimeRage];
}