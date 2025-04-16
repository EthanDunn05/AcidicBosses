using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.Twins.Projectiles;

public class TwinsDashLine : BaseLineProjectile
{
    protected override float Length { get; set; } = 1500f;
    protected override float Width { get; set; } = 33f;
    protected override Color Color => GetColor();
    
    protected override Asset<Texture2D> LineTexture { get; } = TextureRegistry.InvertedFadingGlowLine;
    
    private Color GetColor()
    {
        var fadeT = (float) Projectile.timeLeft / MaxTimeLeft;
        var color = Color.Red;
        if (Main.npc[AnchorTo].type == NPCID.Spazmatism) color = Color.Lime;
        
        color *= EasingHelper.CubicOut(fadeT);
        return color;
    }
}