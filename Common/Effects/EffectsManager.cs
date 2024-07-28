using AcidicBosses.Common.Textures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Common.Effects;

/// <summary>
/// Contains functions for managing shaders effects and proving more understandable
/// parameters for those shaders.
/// </summary>
[Autoload(Side = ModSide.Client)]
public static class EffectsManager
{
    // Shockwave //
    
    public static bool ShockwaveActivate(Vector2 source, float intensity, float width, Color tintColor, float progress)
    {
        if (Main.netMode == NetmodeID.Server || EffectsRegistry.Shockwave.IsActive) return false; 
        EffectsRegistry.Shockwave.TrySetParameter("tintColor", tintColor);
        EffectsRegistry.Shockwave.TrySetParameter("amount", intensity);
        EffectsRegistry.Shockwave.TrySetParameter("width", width);
        EffectsRegistry.Shockwave.TrySetParameter("targetPos", source);
        EffectsRegistry.Shockwave.TrySetParameter("progress", progress);
        EffectsRegistry.Shockwave.Activate();
        return true;
    }
    
    // Boss Rage //

    public static bool BossRageActivate(Color tintColor)
    {
        if (Main.netMode == NetmodeID.Server || EffectsRegistry.BossRage.IsActive) return false;
        EffectsRegistry.BossRage.TrySetParameter("tint", tintColor);
        EffectsRegistry.BossRage.Activate();
        return true;
    }
    
    // Chromatic Aberration //

    public static bool AberrationActivate(float offset)
    {
        if (Main.netMode == NetmodeID.Server || EffectsRegistry.ChromaticAberration.IsActive) return false;

        EffectsRegistry.ChromaticAberration.TrySetParameter("offset", offset);
        EffectsRegistry.ChromaticAberration.Activate();
        return true;
    }
    
    // Other Shaders //

    /// <summary>
    /// Outlines a texture while in 0 light
    /// </summary>
    public static void UndergroundOutlineApply(Asset<Texture2D> texture, Color outlineColor, Color lightColor)
    {
        EffectsRegistry.UndergroundOutline.SetTexture(texture, 0, SamplerState.PointClamp);
        EffectsRegistry.UndergroundOutline.TrySetParameter("outlineColor", outlineColor);
        EffectsRegistry.UndergroundOutline.TrySetParameter("lightColor", lightColor);
        EffectsRegistry.UndergroundOutline.Apply();
    }
    
    /// <summary>
    /// Applies the slime rage effect to king slime.
    /// This will not work with another npc because there's specifically 6 frames of animation.
    /// </summary>
    public static void SlimeRageApply(Asset<Texture2D> texture)
    {
        EffectsRegistry.SlimeRage.SetTexture(texture, 0, SamplerState.PointClamp);
        EffectsRegistry.SlimeRage.SetTexture(TextureRegistry.RgbPerlin, 1, SamplerState.PointClamp);
        EffectsRegistry.SlimeRage.Apply();
    }
    
    public static void ShieldApply(Asset<Texture2D> texture, Color lightColor, float alpha, int frames = 1)
    {
        EffectsRegistry.Shield.SetTexture(texture, 0, SamplerState.PointClamp);
        EffectsRegistry.Shield.TrySetParameter("lighting", lightColor);
        EffectsRegistry.Shield.TrySetParameter("opacity", alpha);
        EffectsRegistry.Shield.TrySetParameter("texFrames", frames);
        EffectsRegistry.Shield.Apply();
    }
}