using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;

namespace AcidicBosses.Common.Effects;

public static class EffectsManager
{
    // Shockwave //
    
    public static bool ShockwaveActive(Vector2 source, float intensity, float width, Color tintColor)
    {
        if (Main.netMode == NetmodeID.Server || EffectsRegistry.Shockwave.IsActive()) return false;
        
        Filters.Scene.Activate(EffectsRegistry.Names.Shockwave, source).GetShader()
            .UseColor(tintColor)
            .UseIntensity(intensity)
            .UseOpacity(width)
            .UseTargetPosition(source);
        return true;
    }

    public static bool ShockwaveProgress(float progress)
    {
        if (Main.netMode == NetmodeID.Server || !EffectsRegistry.Shockwave.IsActive()) return false;
        EffectsRegistry.Shockwave.GetShader()
            .UseProgress(progress);
        return true;
    }

    public static bool ShockwaveKill()
    {
        if (Main.netMode == NetmodeID.Server || !EffectsRegistry.Shockwave.IsActive()) return false;
        EffectsRegistry.Shockwave.Deactivate();
        return true;
    }
    
    // Boss Rage //

    public static bool BossRageActivate(Color tintColor)
    {
        if (Main.netMode == NetmodeID.Server || EffectsRegistry.BossRage.IsActive()) return false;
        Filters.Scene.Activate(EffectsRegistry.Names.BossRage).GetShader()
            .UseColor(tintColor);
        return true;
    }
    
    public static bool BossRageKill()
    {
        if (Main.netMode == NetmodeID.Server || !EffectsRegistry.BossRage.IsActive()) return false;
        EffectsRegistry.BossRage.Deactivate();
        return true;
    }
}