using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace AcidicBosses.Content.Bosses.Skeletron;

public class MurasamaLine : BaseLineProjectile
{
    protected override float Length { get; set; } = 12000f;
    protected override float Width { get; set; } = 10f;
    protected override Color Color => GetColor();
    protected override Asset<Texture2D> LineTexture => TextureRegistry.GlowLine;
    private int? maxTimeLeft;
    
    // Fade over Time
    private Color GetColor()
    {
        var fadeT = (float) Projectile.timeLeft / maxTimeLeft ?? 3600;
        return Color.Blue * EasingHelper.QuadOut(fadeT);
    }

    public override void AI()
    {
        // Set the max time if it hasn't been set already
        maxTimeLeft ??= Projectile.timeLeft;
        base.AI();
    }
}