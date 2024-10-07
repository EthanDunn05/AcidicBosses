using System;
using AcidicBosses.Common.Textures;
using AcidicBosses.Content.Particles;
using AcidicBosses.Helpers;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace AcidicBosses.Content.ProjectileBases;

public abstract class BaseSwarmProjectile : ModProjectile
{
    protected delegate BetterParticle ParticleConstructor(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime);
    
    public override string Texture => TextureRegistry.InvisPath;

    /// <summary>
    /// The length of the swarm line.
    /// </summary>
    protected abstract float Length { get; }
    
    /// <summary>
    /// The width of the collision line
    /// </summary>
    protected abstract float CollisionWidth { get; }
    
    /// <summary>
    /// The constructor of the particle
    /// </summary>
    protected abstract ParticleConstructor SwarmParticleConstructor { get; }
    
    protected abstract int SwarmMemberLifetime { get; }

    protected abstract int SwarmSpawnInterval { get; }

    /// <summary>
    /// How many pixels are between each collision line segment. Defaults to 32 pixels.
    /// A smaller interval means a more accurate hitbox, but less performant collision detection.
    /// </summary>
    protected virtual int CollisionInterval=> 32;
    
    private int timer = 0;
    private bool doneFirstFrame = false;

    public override void SetDefaults()
    {
        Projectile.netImportant = true;
        Projectile.width = 0;
        Projectile.height = 0;
        Projectile.tileCollide = false;
    }
    
    public static Projectile Create<T>(IEntitySource spawnSource, Vector2 position, float rotation, int damage, int knockback, int lifetime) where T : BaseSwarmProjectile
    {
        return ProjHelper.NewUnscaledProjectile(spawnSource, position, Vector2.Zero,
            ModContent.ProjectileType<T>(), damage, knockback, ai0: rotation, ai1: lifetime);
    }

    public override void AI()
    {
        if (!doneFirstFrame)
        {
            doneFirstFrame = true;
            Projectile.rotation = Projectile.ai[0];
            Projectile.timeLeft = (int) Projectile.ai[1];
        }
        
        timer++;

        // Spawn a swarm particle
        if (timer % SwarmSpawnInterval == 0 && Projectile.timeLeft > SwarmMemberLifetime)
        {
            // Add a bit of variance to the speed
            var speedOffset = Main.rand.NextFloat(-2f, 2f);

            var posOffset = Main.rand.NextFloat(0, CollisionWidth);
            
            var particle = SwarmParticleConstructor(Projectile.position, Vector2.Zero, Projectile.rotation, Color.White, SwarmMemberLifetime);
            particle.OnUpdate = p =>
            {
                var dist = (float) p.Time / (SwarmMemberLifetime + speedOffset) * Length;
                var d1 = dist + 0.01f;
                
                var nextPos = MakeWorldPos(d1);
                var currentPos = MakeWorldPos(dist);

                var dir = currentPos.DirectionTo(nextPos).RotatedBy(MathHelper.PiOver2);
                p.Position = currentPos + dir * posOffset;
            };

            particle.Spawn();
        }
    }

    /// <summary>
    /// Gets the offset from the center for the swarm's path.
    /// </summary>
    /// <param name="t">The pixels down the line</param>
    /// <returns> An offset from the center line.</returns>
    protected abstract Vector2 PathEquation(float t);

    /// <summary>
    /// Converts a distance to the progress 0-1 down the swarm line.
    /// Useful for lerps and evaluating piecewise functions.
    /// </summary>
    /// <param name="x">The pixels down the swarm line</param>
    /// <returns>A value from 0-1 proportional to the distance down the swarm line</returns>
    protected float LengthProgress(float x) => x / Length;

    private Vector2 MakeWorldPos(float lineDist)
    {
        // Get the point for if the lie was facing right
        var pointPos = Projectile.position + PathEquation(lineDist);
        
        // Return the point rotated around the center
        return pointPos.RotatedBy(Projectile.rotation, Projectile.position);
    }

    private void DoOnSegments(Action<float, float> action)
    {
        // Lerp in and out so that the collision doesn't instantly hit
        var end = MathHelper.Lerp(0, Length, Utils.GetLerpValue(0, SwarmMemberLifetime, timer, true));
       
        // Lerp out
        var start = MathHelper.Lerp(0, Length, Utils.GetLerpValue(SwarmMemberLifetime, 0, Projectile.timeLeft, true));
        
        for (var i = CollisionInterval + start; i < end; i += CollisionInterval)
        {
            action(i - CollisionInterval, i);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        // Draw collision for debug
        // DoOnSegments((i1, i2) =>
        // {
        //     Utils.DrawLine(Main.spriteBatch, MakeWorldPos(i1), MakeWorldPos(i2), Color.Red, Color.Red, CollisionWidth);
        // });
        
        return false;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        // Divide the line into segments and check each of their collisions individually
        var collided = false;
        
        DoOnSegments((i1, i2) =>
        {
            if (collided) return; // Don't bother checking more if already found a collision
            
            var startPos = MakeWorldPos(i1);
            var endPos = MakeWorldPos(i2);

            var point = 0f;
            var colliding = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), startPos,
                endPos, CollisionWidth, ref point);
            if (colliding) collided = true;
        });

        return collided;
    }
}