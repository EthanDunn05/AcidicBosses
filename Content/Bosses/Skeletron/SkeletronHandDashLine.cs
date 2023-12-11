using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace AcidicBosses.Content.Bosses.Skeletron;

public class SkeletronHandDashLine : BaseLineProjectile
{
    protected override float Length => 12000f;
    protected override float Width => 18f;
    protected override Color Color => Color.LightGray;
    protected override Asset<Texture2D> LineTexture { get; } = TextureRegistry.InvertedGlowLine;
}