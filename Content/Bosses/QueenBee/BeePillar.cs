using System;
using AcidicBosses.Content.Particles.Animated;
using AcidicBosses.Content.ProjectileBases;
using Microsoft.Xna.Framework;

namespace AcidicBosses.Content.Bosses.QueenBee;

public class BeePillar : BaseSwarmProjectile
{
    protected override float Length => 2000;
    protected override float CollisionWidth => 100f;
    protected override ParticleConstructor SwarmParticleConstructor =>
        (position, velocity, rotation, color, lifetime) =>
            new BigBeeParticle(position, velocity, rotation, color, lifetime);

    protected override int SwarmMemberLifetime => 60;
    protected override int SwarmSpawnInterval => 1;
    protected override int MembersPerSpawn => 3;

    protected override Vector2 PathEquation(float t)
    {
        return new Vector2(t, 10 * MathF.Sin(t / MathHelper.TwoPi / 10));
    }
    
    public override void SetDefaults()
    {
        base.SetDefaults();
        Projectile.hostile = true;
    }
}