using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;

namespace AcidicBosses.Content.Bosses.EoC;

public class EyeDashLine : BaseLineProjectile
{
    protected override float Length { get; set; } = 12000f;
    protected override float Width { get; set; } = 33f;
    protected override Color Color => GetColor();
    
    protected override Asset<Texture2D> LineTexture { get; } = TextureRegistry.InvertedGlowLine;
    
    private Color GetColor()
    {
        var fadeT = (float) Projectile.timeLeft / MaxTimeLeft;
        var color = Color.Crimson;
        color *= EasingHelper.CubicOut(fadeT);
        return color;
    }
}