using System;
using AcidicBosses.Content.Particles.Animated;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Helpers;
using AcidicBosses.Helpers.ProjectileHelpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.QueenBee;

public class BeeCircle : BaseSwarmProjectile, IAnchoredProjectile
{
    // Swarm Data
    protected override float Length => 1000f;
    protected override float CollisionWidth => 25f;
    protected override ParticleConstructor SwarmParticleConstructor =>
        (position, velocity, rotation, color, lifetime) =>
            new BigBeeParticle(position, velocity, rotation, color, lifetime);
    protected override int SwarmMemberLifetime => 60;
    protected override int SwarmSpawnInterval => 2;

    // Anchor Data
    public float Offset => 0f;
    public int AnchorTo => (int) Projectile.ai[2];
    public bool AnchorPosition => true;
    public bool AnchorRotation => false;
    public bool RotateAroundCenter => false;
    public Vector2? StartOffset { get; set; }
    
    public static Projectile Create(IEntitySource spawnSource, Vector2 position, float rotation, int damage, int knockback, int lifetime, int anchorNpcId)
    {
        return ProjHelper.NewUnscaledProjectile(spawnSource, position, Vector2.Zero,
            ModContent.ProjectileType<BeeCircle>(), damage, knockback, ai0: rotation, ai1: lifetime, ai2: anchorNpcId);
    }

    public override void AI()
    {
        this.Anchor(Projectile);
        base.AI();
    }

    protected override Vector2 PathEquation(float t)
    {
        var progress = LengthProgress(t);
        var y = MathF.Sin(progress * MathHelper.TwoPi) * 100f;
        var x = MathF.Cos(progress * MathHelper.TwoPi) * 100f;
        return new Vector2(x, y);
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Projectile.hostile = true;
    }
}