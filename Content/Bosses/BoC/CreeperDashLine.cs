using AcidicBosses.Content.ProjectileBases;
using Microsoft.Xna.Framework;

namespace AcidicBosses.Content.Bosses.BoC;

public class CreeperDashLine : DashLineBase
{
    protected override float Length { get; } = 12000f;
    protected override float Width { get; } = 14;
    protected override Color Color { get; } = Color.Crimson;
}