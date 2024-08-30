using System;
using AcidicBosses.Content.Particles;
using AcidicBosses.Core.Animation;
using AcidicBosses.Helpers;
using Luminance.Common.Easings;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    private AcidAnimation? SpazCircleAnimation;

    private void CreateSpazCircleAnimation()
    {
        SpazCircleAnimation = new AcidAnimation();
        var anim = SpazCircleAnimation;

        const int indicateTime = 60;
        const int circleTime = 60 + indicateTime;
        
        const float distance = 600;
        const float fireballSpeed = 10f;
        const int fireballs = 10;
        
        // Teleport into position and start telegraph
        anim.AddInstantEvent(0, () =>
        {
            var target = Main.player[NPC.target].Center;
            Spazmatism.Npc.rotation = MathHelper.Pi;
            Teleport(Spazmatism, target + new Vector2(distance, 0), 0f);
            Teleport(Retinazer, target - new Vector2(0, distance + 200), 0f);

            NPC.localAI[0] = target.X;
            NPC.localAI[1] = target.Y;

            Spazmatism.Npc.rotation = 0f;
            Retinazer.Npc.rotation = 0f;

            SoundEngine.PlaySound(SoundID.ForceRoarPitched, Spazmatism.Npc.Center);
            
            // Fireball indicators
            var scaleCurve = new PiecewiseCurve()
                .Add(EasingCurves.Quadratic, EasingType.In, 8f, 0.1f)
                .Add(EasingCurves.Quadratic, EasingType.Out, 0f, 1f);

            for (var i = 0; i < fireballs; i++)
            {
                var fireballProgress = (float) i / fireballs;
                
                var angle = MathHelper.TwoPi * fireballProgress;
                var fireX = MathF.Cos(angle) * distance;
                var fireY = MathF.Sin(angle) * distance;
                var pos = target + new Vector2(fireX, fireY);

                new GlowStarParticle(pos, Vector2.Zero, angle, Color.White, 30)
                {
                    IgnoreLighting = true,
                    OnUpdate = p =>
                    {
                        var scale = scaleCurve.Evaluate(p.LifetimeRatio);
                        p.Scale = Vector2.One * scale;
                        p.AngularVelocity = p.LifetimeRatio * MathHelper.Pi / 16f;
                    }
                }.Spawn();
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NewSpazCircleIndicator(target, indicateTime);
            }
        });
        
        // Do a spin for visual flair
        anim.AddTimedEvent(0, indicateTime, (progress, frame) =>
        {
            var spinCurve = new PiecewiseCurve()
                .Add(MoreEasingCurves.Back, EasingType.Out, MathHelper.TwoPi, 1f);

            Spazmatism.Npc.rotation = spinCurve.Evaluate(progress);
        });
        
        anim.AddInstantEvent(indicateTime, () =>
        {
            Spazmatism.Npc.rotation = 0f;
            SoundEngine.PlaySound(SoundID.Item89, Spazmatism.Npc.Center);
        });
        
        // Circle around the player
        anim.AddTimedEvent(indicateTime, circleTime, (progress, frame) =>
        {
            ref var targetX = ref NPC.localAI[0];
            ref var targetY = ref NPC.localAI[1];
            var target = new Vector2(targetX, targetY);
            
            ref var fireballsSpawned = ref NPC.localAI[2];
            
            var ease = EasingHelper.QuadOut(progress);
            var npc = Spazmatism.Npc;

            var x = MathF.Cos(ease * MathHelper.TwoPi) * distance;
            var y = MathF.Sin(ease * MathHelper.TwoPi) * distance;
            var rot = MathHelper.WrapAngle(ease * MathHelper.TwoPi);

            npc.Center = new Vector2(x, y) + target;
            npc.rotation = rot;

            var fireballProgress = fireballsSpawned / fireballs;
            if (ease >= fireballProgress && Main.netMode != NetmodeID.MultiplayerClient)
            {
                var angle = MathHelper.TwoPi * fireballProgress;
                var fireX = MathF.Cos(angle) * distance;
                var fireY = MathF.Sin(angle) * distance;
                var pos = target + new Vector2(fireX, fireY);
                var vel = -angle.ToRotationVector2() * fireballSpeed;
                
                NewSpazFireball(pos, vel);
                fireballsSpawned++;
            }
        });
        
        anim.AddInstantEvent(circleTime + 1, () =>
        {
            NPC.localAI[0] = 0;
            NPC.localAI[1] = 0;
            NPC.localAI[2] = 0;
        });
    }
    
    private bool Attack_SpazCircle()
    {
        if (SpazCircleAnimation is null) CreateSpazCircleAnimation();
        if (!SpazCircleAnimation.RunAnimation()) return false;
        SpazCircleAnimation.Reset();
        return true;
    }
}