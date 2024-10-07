using System;
using AcidicBosses.Content.Particles;
using AcidicBosses.Content.Particles.Animated;
using AcidicBosses.Content.ProjectileBases;
using Microsoft.Xna.Framework;

namespace AcidicBosses.Content.Bosses.QueenBee;

public class BeeCircle : BaseSwarmProjectile
{
    protected override float Length => 1000f;
    protected override float CollisionWidth => 25f;

    protected override ParticleConstructor SwarmParticleConstructor =>
        (position, velocity, rotation, color, lifetime) =>
            new BigBeeParticle(position, velocity, rotation, color, lifetime);

    protected override int SwarmMemberLifetime => 120;

    protected override int SwarmSpawnInterval => 2;

    protected override Vector2 PathEquation(float t)
    {
        var progress = LengthProgress(t);
        var y = MathF.Sin(progress * MathHelper.TwoPi) * 250f;
        var x = MathF.Cos(progress * MathHelper.TwoPi) * 250f;
        return new Vector2(x, y);
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Projectile.hostile = true;
    }
}