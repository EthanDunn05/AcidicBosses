using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace AcidicBosses.Content.Bosses.Twins.Projectiles;

public class RetSweepIndicator : BaseLineProjectile
{
    protected override float Length { get; set; } = 12000;
    protected override float Width { get; set; } = 25;
    protected override Color Color { get; } = Color.Red;
    protected override Asset<Texture2D> LineTexture => TextureRegistry.SideGlowLine;
    public override bool AnchorRotation => false;
}