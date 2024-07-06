using System;
using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace AcidicBosses.Content.Bosses.Skeletron;

public class CursedSkullLine : BaseLineProjectile
{
    protected override float Length { get; set; } = 12000f;
    protected override float Width { get; set; } = 10f;
    protected override Color Color => GetColor();

    protected override Asset<Texture2D> LineTexture { get; } = TextureRegistry.GlowLine;
    // Fade over Time
    private Color GetColor()
    {
        var fadeT = (float) Projectile.timeLeft / maxTimeLeft;
        return Color.OrangeRed * EasingHelper.QuadOut(fadeT);
    }
}