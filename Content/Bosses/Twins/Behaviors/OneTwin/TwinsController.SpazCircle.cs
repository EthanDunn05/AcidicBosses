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
        
        const float distance = 600;
        const float fireballSpeed = 10f;
        const int fireballs = 10;

        const int indicationLength = 60;

        const string targetPosKey = "targetPos";
        const string fireballsSpawnedKey = "fireballsSpawned";
        
        // Teleport into position and start telegraph
        anim.AddInstantEvent(0, () =>
        {
            var target = Main.player[NPC.target].Center;
            
            anim.Data.Set(targetPosKey, target);
            
            Spazmatism.Npc.rotation = MathHelper.Pi;
            Teleport(Spazmatism, target + new Vector2(distance, 0), 0f);
            if (Retinazer.Npc.active) Teleport(Retinazer, target - new Vector2(0, distance + 100), 0f);
            
            Spazmatism.Npc.rotation = 0f;
            if (Retinazer.Npc.active) Retinazer.Npc.rotation = 0f;

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

                new GlowStarParticle(pos, Vector2.Zero, angle, Color.White, 60)
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
                NewSpazCircleIndicator(target, indicationLength);
            }
        });
        
        // Do a spin for visual flair
        var indicateTiming = anim.AddSequencedEvent(indicationLength, (progress, frame) =>
        {
            var spinCurve = new PiecewiseCurve()
                .Add(MoreEasingCurves.Back, EasingType.Out, MathHelper.TwoPi, 1f);

            Spazmatism.Npc.rotation = spinCurve.Evaluate(progress);
        });
        
        anim.AddInstantEvent(indicateTiming.EndTime, () =>
        {
            anim.Data.Set(fireballsSpawnedKey, 0);
            Spazmatism.Npc.rotation = 0f;
            SoundEngine.PlaySound(SoundID.Item89, Spazmatism.Npc.Center);
        });
        
        // Circle around the player
        var circleTiming = anim.AddSequencedEvent(60, (progress, frame) =>
        {
            var target = anim.Data.Get<Vector2>(targetPosKey);
            var fireballsSpawned = anim.Data.Get<int>(fireballsSpawnedKey);
            
            var ease = EasingHelper.QuadOut(progress);
            var npc = Spazmatism.Npc;

            var x = MathF.Cos(ease * MathHelper.TwoPi) * distance;
            var y = MathF.Sin(ease * MathHelper.TwoPi) * distance;
            var rot = MathHelper.WrapAngle(ease * MathHelper.TwoPi);

            npc.Center = new Vector2(x, y) + target;
            npc.rotation = rot;

            var fireballProgress = (float) fireballsSpawned / fireballs;
            if (ease >= fireballProgress && Main.netMode != NetmodeID.MultiplayerClient)
            {
                var angle = MathHelper.TwoPi * fireballProgress;
                var fireX = MathF.Cos(angle) * distance;
                var fireY = MathF.Sin(angle) * distance;
                var pos = target + new Vector2(fireX, fireY);
                var vel = -angle.ToRotationVector2() * fireballSpeed;
                
                NewSpazFireball(pos, vel);
                anim.Data.Operate<int>(fireballsSpawnedKey, n => n + 1);
            }
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