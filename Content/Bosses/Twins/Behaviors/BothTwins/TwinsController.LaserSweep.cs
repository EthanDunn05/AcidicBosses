using AcidicBosses.Content.Particles;
using AcidicBosses.Core.Animation;
using AcidicBosses.Helpers;
using AcidicBosses.Helpers.NpcHelpers;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    // Using an animation for this attack for easier timing control
    private AcidAnimation? laserSweepAnimation;

    private void CreateLaserSweepAnimation()
    {
        var npc = Retinazer.Npc;

        const int indicateLength = 90;
        const int rayLength =  120;

        const float spreadRadius = MathHelper.PiOver4;

        const string startAngleKey = "startAngle";
        
        laserSweepAnimation = new AcidAnimation();
        
        // Come to a stop
        laserSweepAnimation.AddConstantEvent((progress, frame) =>
        {
            npc.SimpleFlyMovement(Vector2.Zero, 0.5f);
        });
        
        // Start the sweep
        laserSweepAnimation.AddInstantEvent(0, () =>
        {
            var startAngle = npc.rotation;
            laserSweepAnimation.Data.Set(startAngleKey, npc.rotation);

            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            
            NewRetSweepIndicator(npc.Center, startAngle + MathHelper.PiOver2, indicateLength);
        });
        
        // Turn to the start of the sweep and play effects
        var indicateTiming = laserSweepAnimation.AddSequencedEvent(indicateLength, (progress, frame) =>
        {
            var startAngle = laserSweepAnimation.Data.Get<float>(startAngleKey);
            var ease = EasingHelper.BackOut(progress);
            
            var offset = spreadRadius * ease;
            npc.rotation = startAngle + offset;
            
            // Collect energy particles
            var pos = Retinazer.Front;
            var spawnPos = Main.rand.NextVector2CircularEdge(20f, 20f) + pos;
            var angVel = Main.rand.NextFloat(-0.1f, 0.1f);
            var rot = Main.rand.NextFloatDirection();
            
            new GlowStarParticle(spawnPos, Vector2.Zero, rot, Color.White, 30)
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
            }.Spawn();
            
            Hover(Spazmatism, 10f, 0.15f);
            
            if (frame % 60 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NewSpazFireball(Spazmatism.Npc.Center, Spazmatism.Npc.Center.DirectionTo(Main.player[NPC.target].Center) * 10f);
            }
        });
        
        // Create Deathray
        laserSweepAnimation.AddInstantEvent(indicateTiming.EndTime, () =>
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            
            var pos = npc.Center + npc.rotation.ToRotationVector2() * npc.width;
            NewRetDeathray(pos, MathHelper.PiOver2, rayLength);
        });
        
        // Sweep deathray
        var rayTiming = laserSweepAnimation.AddSequencedEvent(rayLength, (progress, frame) =>
        {
            var startAngle = laserSweepAnimation.Data.Get<float>(startAngleKey);
            var ease = 1f - EasingHelper.QuadInOut(progress);
            
            var offset = spreadRadius * (ease - 0.5f) * 2f;
            npc.rotation = startAngle + offset;
        });
        
        // First Spaz Dash
        laserSweepAnimation.AddTimedEvent(indicateTiming.EndTime, indicateTiming.EndTime + 60, (progress, frame) =>
        {
            var dashSettings = new DashOptions
            {
                DashLength = 30,
                DashSpeed = 30,
                LookOffset = MathHelper.PiOver2,
                MinimumDistance = 250,
                TrackTime = 15,
                DashAtTime = 30,
            };

            Dash(Spazmatism, Spazmatism.AttackManager, Main.player[NPC.target].Center, dashSettings);
        });
        
        // Reset for second dash
        laserSweepAnimation.AddInstantEvent(indicateTiming.EndTime + 60, () =>
        {
            Spazmatism.AttackManager.Reset();
        });
        
        // Second Spaz dash
        laserSweepAnimation.AddTimedEvent(indicateTiming.EndTime + 60, rayTiming.EndTime, (progress, frame) =>
        {
            var dashSettings = new DashOptions
            {
                DashLength = 30,
                DashSpeed = 30,
                LookOffset = MathHelper.PiOver2,
                MinimumDistance = 250,
                TrackTime = 15,
                DashAtTime = 30,
            };

            Dash(Spazmatism, Spazmatism.AttackManager, Main.player[NPC.target].Center, dashSettings);
        });
        
        // Reset after second dash
        laserSweepAnimation.AddInstantEvent(rayTiming.EndTime, () =>
        {
            Spazmatism.AttackManager.Reset();
        });
    }
    
    private bool Attack_SweepingLaser()
    {
        if (laserSweepAnimation is null) CreateLaserSweepAnimation();
        if (!laserSweepAnimation.RunAnimation()) return false;
        laserSweepAnimation.Reset();
        return true;
    }
}