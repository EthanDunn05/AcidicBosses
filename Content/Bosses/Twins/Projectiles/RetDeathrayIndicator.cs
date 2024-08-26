using System.Linq;
using AcidicBosses.Common.Effects;
using AcidicBosses.Common.Textures;
using AcidicBosses.Content.Bosses.KingSlime;
using AcidicBosses.Content.Particles;
using AcidicBosses.Content.ProjectileBases;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.Twins.Projectiles;

public class RetDeathrayIndicator : BaseLineProjectile
{
    protected override float Length { get; set; } = 12000;
    protected override float Width { get; set; } = 5;
    protected override Color Color => Color.Red;
    protected override Asset<Texture2D> LineTexture => TextureRegistry.GlowLine;
    public override bool RotateAroundCenter => true;
}