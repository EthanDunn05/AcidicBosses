using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace AcidicBosses.Content.Bosses.WoF.Projectiles;

public class WoFDeathrayIndicator : BaseLineProjectile
{
    protected override float Length { get; set; } = 25000f;
    protected override float Width { get; set; } = 28;

    protected override Color Color => Color.Purple;
    protected override Asset<Texture2D> LineTexture => TextureRegistry.InvertedGlowLine;

    public override bool AnchorRotation => false;
}