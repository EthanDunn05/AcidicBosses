using AcidicBosses.Content.ProjectileBases;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.Twins.Projectiles;

public class SpazFlamethrower : BaseBetsyFlame
{
    public override Color StartFlameColor => new(180, 255, 200);
    public override Color EndFlameColor => new(30, 130, 30);
    public override short FlameDust => DustID.CursedTorch;
}