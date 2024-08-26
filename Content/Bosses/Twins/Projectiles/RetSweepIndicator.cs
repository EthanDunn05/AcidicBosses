using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace AcidicBosses.Content.Bosses.Twins.Projectiles;

public class RetSweepIndicator : BaseSweep
{
    protected override float Length => 12000;
    protected override float Width => 25;
    protected override Color Color { get; } = Color.Red;
    protected override float Radius => MathHelper.PiOver4;
    public override bool AnchorRotation => false;
}