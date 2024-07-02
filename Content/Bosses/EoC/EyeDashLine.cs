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
    protected override float Length { get; } = 12000f;
    protected override float Width { get; } = 35f;
    protected override Color Color => Color.Crimson;
    
    protected override Asset<Texture2D> LineTexture { get; } = TextureRegistry.InvertedGlowLine;
    
    // Nullable to make setting a single time easier
    private int? maxTimeLeft;
    
    private Color GetColor()
    {
        var fadeT = (float) Projectile.timeLeft / maxTimeLeft ?? 3600;
        var color = Color.Crimson;
        color *= EasingHelper.CubicOut(fadeT);
        return color;
    }

    public override void AI()
    {
        base.AI();
        
        // Set the max time if it hasn't been set already
        maxTimeLeft ??= Projectile.timeLeft;
    }
}