﻿using AcidicBosses.Common.Textures;
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

    public static bool BossRageActivate(Color tintColor)
    {
        if (Main.netMode == NetmodeID.Server || EffectsRegistry.BossRage.IsActive) return false;
        EffectsRegistry.BossRage.TrySetParameter("tint", tintColor);
        EffectsRegistry.BossRage.Activate();
        return true;
    }

    public static bool AberrationActivate(float offset)
    {
        if (Main.netMode == NetmodeID.Server || EffectsRegistry.ChromaticAberration.IsActive) return false;

        EffectsRegistry.ChromaticAberration.TrySetParameter("offset", offset);
        EffectsRegistry.ChromaticAberration.Activate();
        return true;
    }
    
    public static bool BlackHoleActivate(float eventHorizonRadius, Vector2 position)
    {
        if (Main.netMode == NetmodeID.Server || EffectsRegistry.BlackHole.IsActive) return false;
        
        EffectsRegistry.BlackHole.TrySetParameter("eventHorizonRadius", eventHorizonRadius);
        EffectsRegistry.BlackHole.TrySetParameter("targetPos", position);
        EffectsRegistry.BlackHole.Activate();
        return true;
    }

    public static void BloomActivate(RenderTarget2D texture)
    {
        EffectsRegistry.Bloom.SetTexture(texture, 0);
        EffectsRegistry.Bloom.Apply();
    }

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
    public static void SlimeRageApply(Asset<Texture2D> texture, Color lightColor)
    {
        EffectsRegistry.SlimeRage.SetTexture(texture, 0);
        EffectsRegistry.SlimeRage.SetTexture(TextureRegistry.RgbPerlin, 1);
        EffectsRegistry.SlimeRage.TrySetParameter("lightColor", lightColor);
        EffectsRegistry.SlimeRage.Apply();
    }
    
    public static void ShieldApply(Asset<Texture2D> texture, Color lightColor, float alpha, int frames = 1)
    {
        EffectsRegistry.Shield.SetTexture(texture, 0);
        EffectsRegistry.Shield.TrySetParameter("lighting", lightColor);
        EffectsRegistry.Shield.TrySetParameter("opacity", alpha);
        EffectsRegistry.Shield.TrySetParameter("texFrames", frames);
        EffectsRegistry.Shield.Apply();
    }

    public static void IndicatorColorApply(Asset<Texture2D> texture, Color color)
    {
        EffectsRegistry.IndicatorColor.SetTexture(texture, 0);
        EffectsRegistry.IndicatorColor.TrySetParameter("color", color);
        
        EffectsRegistry.IndicatorColor.Apply();
    }
}