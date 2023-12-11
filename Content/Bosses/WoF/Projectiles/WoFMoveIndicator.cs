using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace AcidicBosses.Content.Bosses.WoF.Projectiles;

public class WoFMoveIndicator : BaseLineProjectile
{
    protected override float Length => 50000f;
    protected override float Width => 25f;
    protected override Color Color => GetColor();
    protected override Asset<Texture2D> LineTexture => TextureRegistry.SideGlowLine;
    
    // Nullable to make setting a single time easier
    private int? maxTimeLeft;

    // Fade over Time
    private Color GetColor()
    {
        var fadeT = (float) Projectile.timeLeft / maxTimeLeft ?? 3600;
        return Color.Aquamarine * EasingHelper.QuadOut(fadeT);
    }

    public override void AI()
    {
        // Set the max time if it hasn't been set already
        maxTimeLeft ??= Projectile.timeLeft;
        base.AI();
    }
}