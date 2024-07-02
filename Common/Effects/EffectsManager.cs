using AcidicBosses.Common.Textures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;

namespace AcidicBosses.Common.Effects;

/// <summary>
/// Contains functions for managing shaders effects and proving more understandable
/// parameters for those shaders.
/// </summary>
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
    
    // Chromatic Aberration //

    public static bool AberrationActivate(float offset)
    {
        if (Main.netMode == NetmodeID.Server || EffectsRegistry.ChromaticAberration.IsActive()) return false;

        Filters.Scene.Activate(EffectsRegistry.Names.ChromaticAberration).GetShader()
            .UseIntensity(offset);
        return true;
    }
    
    public static bool AberrationProgress(float offset)
    {
        if (Main.netMode == NetmodeID.Server || !EffectsRegistry.ChromaticAberration.IsActive()) return false;
        EffectsRegistry.ChromaticAberration.GetShader()
            .UseIntensity(offset);
        return true;
    }

    public static bool AberrationKill()
    {
        if (Main.netMode == NetmodeID.Server || !EffectsRegistry.ChromaticAberration.IsActive()) return false;
        EffectsRegistry.ChromaticAberration.Deactivate();
        return true;
    }
    
    // Other Shaders //

    /// <summary>
    /// Outlines a texture while in 0 light
    /// </summary>
    public static void UndergroundOutlineApply(Asset<Texture2D> texture, Color outlineColor, Color lightColor)
    {
        EffectsRegistry.UndergroundOutline.UseImage0(texture);
        EffectsRegistry.UndergroundOutline.UseColor(outlineColor);
        EffectsRegistry.UndergroundOutline.UseSecondaryColor(lightColor);
        EffectsRegistry.UndergroundOutline.Apply();
    }
    
    /// <summary>
    /// Applies the slime rage effect to king slime.
    /// This will not work with another npc because there's specifically 6 frames of animation.
    /// </summary>
    public static void SlimeRageApply(Asset<Texture2D> texture)
    {
        EffectsRegistry.SlimeRage.UseImage0(texture);
        EffectsRegistry.SlimeRage.UseImage1(TextureRegistry.RgbPerlin);
        EffectsRegistry.SlimeRage.Apply();
    }
    
    public static void ShieldApply(Asset<Texture2D> texture, Color lightColor, float alpha, int frames = 1)
    {
        EffectsRegistry.Shield.UseImage0(texture);
        EffectsRegistry.Shield.UseSecondaryColor(lightColor);
        EffectsRegistry.Shield.UseOpacity(alpha);
        EffectsRegistry.Shield.Shader.Parameters["uFrames"].SetValue(frames);
        EffectsRegistry.Shield.Apply();
    }
}