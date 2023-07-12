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
    }

    public static Filter Shockwave => Filters.Scene[Names.Shockwave];
    public static Filter BossRage => Filters.Scene[Names.BossRage];
    
    public static MiscShaderData KsCrownLaser => GameShaders.Misc[Names.KsCrownLaser];
}