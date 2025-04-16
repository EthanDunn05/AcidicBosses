using System;
using AcidicBosses.Content.Particles.Animated;
using AcidicBosses.Content.ProjectileBases;
using Microsoft.Xna.Framework;
using Terraria;

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

    public override void AI()
    {
        base.AI();
        
        // Kick up block dust
        if (Projectile.timeLeft % 15 == 0 && Projectile.timeLeft >= SwarmMemberLifetime)
        {
            var tilePos = Projectile.position.ToTileCoordinates();
            for (var i = -3; i <= 3; i++)
            {
                var offsetTilePos = tilePos + new Point(i, 1);
                WorldGen.KillTile(offsetTilePos.X, offsetTilePos.Y, true, true);
            }
        }
    }
}