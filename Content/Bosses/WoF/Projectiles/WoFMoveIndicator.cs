using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace AcidicBosses.Content.Bosses.WoF.Projectiles;

public class WoFMoveIndicator : BaseLineProjectile
{
    protected override float Length { get; set; } = 50000f;
    protected override float Width { get; set; } = 25f;
    protected override Color Color => GetColor();
    protected override Asset<Texture2D> LineTexture => TextureRegistry.SideGlowLine;
    public override bool AnchorRotation => false;

    // Fade over Time
    private Color GetColor()
    {
        var fadeT = (float) Projectile.timeLeft / MaxTimeLeft;
        return Color.Aquamarine * EasingHelper.QuadOut(fadeT);
    }
}