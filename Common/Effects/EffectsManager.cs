﻿using AcidicBosses.Common.Textures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
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

    public static void UndergroundOutline(Asset<Texture2D> texture, Color outlineColor, Color lightColor)
    {
        EffectsRegistry.UndergroundOutline.UseImage0(texture);
        EffectsRegistry.UndergroundOutline.UseColor(outlineColor);
        EffectsRegistry.UndergroundOutline.UseSecondaryColor(lightColor);
        EffectsRegistry.UndergroundOutline.Apply();
    }
    
    public static void SlimeRage(Asset<Texture2D> texture)
    {
        EffectsRegistry.SlimeRage.UseImage0(texture);
        EffectsRegistry.SlimeRage.UseImage1(TextureRegistry.RgbPerlin);
        EffectsRegistry.SlimeRage.Apply();
    }
}