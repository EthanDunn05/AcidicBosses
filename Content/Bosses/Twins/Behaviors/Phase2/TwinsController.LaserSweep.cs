using AcidicBosses.Content.Particles;
using AcidicBosses.Core.Animation;
using AcidicBosses.Helpers;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    // Using an animation for this attack for easier timing control
    private AcidAnimation? LaserSweepAnimation;

    private void CreateLaserSweepAnimation()
    {
        var npc = Retinazer.Npc;

        const int indicateTime = 90;
        const int rayTime = indicateTime + 120;

        const float spreadRadius = MathHelper.PiOver4;
        
        LaserSweepAnimation = new AcidAnimation();
        
        // Come to a stop
        LaserSweepAnimation.AddConstantEvent((progress, frame) =>
        {
            Hover(Spazmatism, 10f, 0.15f);
            npc.SimpleFlyMovement(Vector2.Zero, 0.5f);
            
            if (frame % 60 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NewSpazFireball(Spazmatism.Npc.Center, Spazmatism.Npc.Center.DirectionTo(Main.player[NPC.target].Center) * 10f);
            }
        });
        
        // Start the sweep
        LaserSweepAnimation.AddInstantEvent(0, () =>
        {
            ref var startAngle = ref NPC.localAI[0];
            startAngle = npc.rotation;

            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            
            NewRetSweepIndicator(npc.Center, startAngle + MathHelper.PiOver2, indicateTime);
        });
        
        // Turn to the start of the sweep and play effects
        LaserSweepAnimation.AddTimedEvent(0, indicateTime, (progress, frame) =>
        {
            ref var startAngle = ref NPC.localAI[0];
            var ease = EasingHelper.BackOut(progress);
            
            var offset = spreadRadius * ease;
            npc.rotation = startAngle + offset;
            
            // Collect energy particles
            var pos  = npc.Center + (npc.rotation + MathHelper.PiOver2).ToRotationVector2() * npc.width;
            var spawnPos = Main.rand.NextVector2CircularEdge(20f, 20f) + pos;
            var angVel = Main.rand.NextFloat(-0.1f, 0.1f);
            var rot = Main.rand.NextFloatDirection();
            
            var particle = new GlowStarParticle(spawnPos, Vector2.Zero, rot, Color.White, 30)
            {
                AngularVelocity = angVel,
                IgnoreLighting = true,
                Scale = Vector2.One,
                OnUpdate = p =>
                {
                    var suck = EasingHelper.ExpOut(p.LifetimeRatio);
                    var shrink = EasingHelper.CubicIn(p.LifetimeRatio);
                    p.Position = Vector2.Lerp(spawnPos, pos, suck);
                    p.Scale = Vector2.Lerp(Vector2.One, Vector2.Zero, shrink);
                }
            };

            particle.Spawn();
        });
        
        // Create Deathray
        LaserSweepAnimation.AddInstantEvent(indicateTime, () =>
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            
            var pos = npc.Center + npc.rotation.ToRotationVector2() * npc.width;
            NewRetDeathray(pos, MathHelper.PiOver2, rayTime - indicateTime);
        });
        
        // Sweep deathray
        LaserSweepAnimation.AddTimedEvent(indicateTime, rayTime, (progress, frame) =>
        {
            ref var startAngle = ref NPC.localAI[0];
            var ease = 1f - EasingHelper.QuadInOut(progress);
            
            var offset = spreadRadius * (ease - 0.5f) * 2f;
            npc.rotation = startAngle + offset;
        });
    }
    
    private bool Attack_SweepingLaser()
    {
        if (LaserSweepAnimation is null) CreateLaserSweepAnimation();
        if (!LaserSweepAnimation.RunAnimation()) return false;
        LaserSweepAnimation.Reset();
        return true;
    }
}