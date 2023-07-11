using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;

namespace AcidicBosses.Shaders;

public static class Effects
{
    private const string Shockwave = "Shockwave";
    private const string BossRage = "BossRage";
    private const string NeonBox = "NeonBox";
    
    // Shockwave //
    
    public static bool ShockwaveActive(Vector2 source, float intensity, float width, Color tintColor)
    {
        if (Main.netMode == NetmodeID.Server || Filters.Scene[Shockwave].IsActive()) return false;
        
        Filters.Scene.Activate(Shockwave, source).GetShader()
            .UseColor(tintColor)
            .UseIntensity(intensity)
            .UseOpacity(width)
            .UseTargetPosition(source);
        return true;
    }

    public static bool ShockwaveProgress(float progress)
    {
        if (Main.netMode == NetmodeID.Server || !Filters.Scene[Shockwave].IsActive()) return false;
        Filters.Scene[Shockwave].GetShader()
            .UseProgress(progress);
        return true;
    }

    public static bool ShockwaveKill()
    {
        if (Main.netMode == NetmodeID.Server || !Filters.Scene[Shockwave].IsActive()) return false;
        Filters.Scene[Shockwave].Deactivate();
        return true;
    }
    
    // Boss Rage //

    public static bool BossRageActivate(Color tintColor)
    {
        if (Main.netMode == NetmodeID.Server || Filters.Scene[BossRage].IsActive()) return false;
        Filters.Scene.Activate(BossRage).GetShader()
            .UseColor(tintColor);
        return true;
    }
    
    public static bool BossRageKill()
    {
        if (Main.netMode == NetmodeID.Server || !Filters.Scene[BossRage].IsActive()) return false;
        Filters.Scene[BossRage].Deactivate();
        return true;
    }
    
    // Neon Box //

    public static bool NeonBoxCreate(Vector2 pos1, Vector2 pos2, float thickness, Color glowColor, float glowRadius)
    {
        if (Main.netMode == NetmodeID.Server || Filters.Scene[NeonBox].IsActive()) return false;
        Filters.Scene.Activate(NeonBox).GetShader()
            .UseSecondaryColor(new Vector3(pos1, 0f))
            .UseTargetPosition(pos2)
            .UseIntensity(thickness)
            .UseColor(glowColor)
            .UseOpacity(glowRadius);
        return true;
    }
    
    public static bool NeonBoxUpdate(Vector2 pos1, Vector2 pos2, float thickness, Color glowColor, float glowRadius)
    {
        if (Main.netMode == NetmodeID.Server || !Filters.Scene[NeonBox].IsActive()) return false;
        Filters.Scene[NeonBox].GetShader()
            .UseSecondaryColor(new Vector3(pos1, 0f))
            .UseTargetPosition(pos2)
            .UseIntensity(thickness)
            .UseColor(glowColor)
            .UseOpacity(glowRadius);
        return true;
    }
    
    public static bool NeonBoxKill()
    {
        if (Main.netMode == NetmodeID.Server || !Filters.Scene[NeonBox].IsActive()) return false;
        Filters.Scene[NeonBox].Deactivate();
        return true;
    }
}