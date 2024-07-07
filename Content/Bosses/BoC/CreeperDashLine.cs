using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace AcidicBosses.Content.Bosses.BoC;

public class CreeperDashLine : BaseLineProjectile
{
    protected override float Length { get; set; } = 12000f;
    protected override float Width { get; set; } = 14;
    protected override Color Color => GetColor();
    protected override Asset<Texture2D> LineTexture { get; } = TextureRegistry.InvertedGlowLine;
    
    private Color GetColor()
    {
        var fadeT = (float) Projectile.timeLeft / maxTimeLeft;
        var color = Color.Crimson;
        color *= EasingHelper.CubicOut(fadeT);
        return color;
    }
}