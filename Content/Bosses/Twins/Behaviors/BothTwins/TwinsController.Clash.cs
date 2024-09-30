using System;
using AcidicBosses.Content.Particles;
using AcidicBosses.Content.Particles.Animated;
using AcidicBosses.Core.Animation;
using AcidicBosses.Helpers;
using AcidicBosses.Helpers.NpcHelpers;
using Luminance.Common.Easings;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    private AcidAnimation? clashAnimation;

    private AcidAnimation CreateClashAnimation()
    {
        var anim = new AcidAnimation();

        const float distance = 600f; // Distance from center
        const float speed = 50f;
        var dashLen = (int) MathF.Ceiling((distance - 50f) / speed);

        const string centerPosKey = "centerPos";
        const string rotationKey = "rotation";
        
        // Initialize data and teleoprt into position
        anim.AddInstantEvent(0, () =>
        {
            var target = Main.player[NPC.target];
            
            anim.Data.Set(rotationKey, target.velocity.SafeNormalize(Vector2.UnitX).ToRotation());
            
            Spazmatism.AttackManager.Reset();
            Retinazer.AttackManager.Reset();

            var offset = target.velocity.SafeNormalize(Vector2.UnitX) * distance;
            
            Teleport(Spazmatism, target.Center + offset, 0f);
            Teleport(Retinazer, target.Center - offset, 0f);
            
            Spazmatism.LookTowards(target.Center, 1f);
            Retinazer.LookTowards(target.Center, 1f);
        });
        
        // Spin Indication
        var spinTiming = anim.AddSequencedEvent(90, (progress, frame) =>
        {
            var startingRot = anim.Data.Get<float>(rotationKey);
            
            var distCurve = new PiecewiseCurve()
                .Add(EasingCurves.Quadratic, EasingType.Out, 100, 0.5f)
                .Add(EasingCurves.Quadratic, EasingType.In, 0, 1f);

            var spinEase = EasingHelper.BackOut(progress);
            
            var target = Main.player[NPC.target];

            var rot = startingRot + MathHelper.TwoPi * spinEase;
            var offset = rot.ToRotationVector2() * (distance + distCurve.Evaluate(progress));

            Spazmatism.Npc.Center = target.Center + offset;
            Retinazer.Npc.Center = target.Center - offset;
            
            Spazmatism.LookTowards(target.Center, 1f);
            Retinazer.LookTowards(target.Center, 1f);
        });
        
        anim.AddInstantEvent(spinTiming.EndTime, () =>
        {
            var target = Main.player[NPC.target];
            anim.Data.Set(centerPosKey, target.Center);
            
            DashFx(Spazmatism);
            DashFx(Retinazer);
        });
        
        // Dash
        var clashTiming = anim.AddSequencedEvent(dashLen, (progress, frame) =>
        {
            var centerPos = anim.Data.Get<Vector2>(centerPosKey);
            
            var options = new DashOptions
            {
                DashLength = dashLen,
                DashSpeed = speed,
                LookOffset = MathHelper.PiOver2,
                MinimumDistance = 0,
                TrackTime = 0,
                DashAtTime = 0
            };

            DashHelper.Dash(Spazmatism.Npc, Spazmatism.AttackManager, centerPos, options);
            DashHelper.Dash(Retinazer.Npc, Retinazer.AttackManager, centerPos, options);
        });
        
        // Clash Effects
        anim.AddInstantEvent(clashTiming.EndTime, () =>
        {
            var centerPos = anim.Data.Get<Vector2>(centerPosKey);
            var rotation = anim.Data.Get<float>(rotationKey);

            Spazmatism.AttackManager.Reset();
            Retinazer.AttackManager.Reset();

            // Bounce
            Spazmatism.Npc.velocity = -Spazmatism.Npc.velocity / 2f;
            Retinazer.Npc.velocity = -Retinazer.Npc.velocity / 2f;

            SoundEngine.PlaySound(Spazmatism.Npc.HitSound!.Value with
            {
                Volume = 1.5f,
                Pitch = -0.25f
            }, centerPos);

            new RingBurstParticle(centerPos, Vector2.Zero, 0f, Color.White, 30).Spawn();
            new InternalCircleParticle(centerPos, Vector2.Zero, 0f, Color.White, 60)
            {
                OnUpdate = p =>
                {
                    var scaleEase = EasingHelper.ExpOut(p.LifetimeRatio);
                    var fadeEase = EasingHelper.ExpIn(p.LifetimeRatio);
                    p.Scale = Vector2.Lerp(Vector2.Zero, new Vector2(4f, 4f), scaleEase);
                    p.Opacity = MathHelper.Lerp(0.5f, 0f, fadeEase);
                }
            }.Spawn();

            var burst = anim.Data.Get<bool>("burst");
            if (burst)
            {
                var balls = 8;
                for (var i = 0; i < balls; i++)
                {
                    var dir = (float) i / balls * MathHelper.TwoPi;
                    NewSpazFireball(centerPos, dir.ToRotationVector2() * 5f);
                }
                
                var lasers = 4;
                for (var i = 0; i < lasers; i++)
                {
                    var dir = (float) i / lasers * MathHelper.TwoPi;
                    NewRetLazer(centerPos, dir.ToRotationVector2() * 30f, dir, 30);
                }
            }

            var useSparks = anim.Data.Get<bool>("useSparks");
            if (useSparks)
            {

                var scaleCurve = new PiecewiseCurve()
                    .Add(EasingCurves.Cubic, EasingType.Out, 1f, 0.5f)
                    .Add(EasingCurves.Cubic, EasingType.In, 0f, 1f);

                for (var i = 0; i < 20; i++)
                {
                    var dir = Main.rand.NextVector2Circular(1f, 10f).RotatedBy(rotation);
                    new GlowStarParticle(centerPos, dir, 0f, Color.White, 15)
                    {
                        AngularVelocity = Main.rand.NextFloat(-1f, 1f),
                        OnUpdate = p => { p.Scale = Vector2.One * scaleCurve.Evaluate(p.LifetimeRatio); }
                    }.Spawn();

                    dir = Main.rand.NextVector2Circular(1f, 20f).RotatedBy(rotation);
                    Dust.NewDustDirect(centerPos, 0, 0, DustID.MinecartSpark, dir.X, dir.Y, Scale: 3);
                }
            }
        });

        return anim;
    }
    
    private bool Attack_Clash(bool useSparks, bool burst)
    {
        clashAnimation ??= CreateClashAnimation();
        clashAnimation.Data.Set("useSparks", useSparks);
        clashAnimation.Data.Set("burst", burst);
        if (!clashAnimation.RunAnimation()) return false;
        clashAnimation.Reset();
        return true;
    }
}