using System;
using AcidicBosses.Content.Particles;
using AcidicBosses.Core.Animation;
using AcidicBosses.Helpers;
using Luminance.Common.Easings;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    private AcidAnimation? laserBurstAnimation;

    private AcidAnimation CreateRetLaserBurstAnimation()
    {
        var anim = new AcidAnimation();

        const int lasers = 32;
        const int spinTime = 60;
        const int laserSpinTime = spinTime * 2;

        anim.AddConstantEvent((progress, frame) => { Hover(Spazmatism, 5f, 0.05f); });

        anim.AddInstantEvent(0, () =>
        {
            ref var startingAngle = ref NPC.localAI[0];
            NPC.localAI[1] = 0;

            Retinazer.Npc.velocity = Vector2.Zero;

            startingAngle = Retinazer.Npc.rotation;
        });

        // Indicators
        anim.AddTimedEvent(0, spinTime, (progress, frame) =>
        {
            ref var startingAngle = ref NPC.localAI[0];
            ref var spawnedLasers = ref NPC.localAI[1];

            var ease = EasingHelper.QuadInOut(progress);

            var angle = MathHelper.Lerp(0, MathHelper.TwoPi, ease);
            Retinazer.Npc.rotation = MathHelper.WrapAngle(startingAngle + angle);

            var laserProgress = spawnedLasers / lasers;
            if (ease >= laserProgress && Main.netMode != NetmodeID.MultiplayerClient)
            {
                var rot = MathHelper.Lerp(0, MathHelper.TwoPi, laserProgress) + startingAngle + MathHelper.PiOver2;
                NewRetLazer(Retinazer.Front, rot.ToRotationVector2() * 30f, rot, spinTime);
                
                spawnedLasers++;
            }
        });

        // Reset laser count
        anim.AddInstantEvent(spinTime + 1, () => { NPC.localAI[1] = 0; });

        // Spin with the lasers
        anim.AddTimedEvent(spinTime, laserSpinTime, (progress, frame) =>
        {
            ref var startingAngle = ref NPC.localAI[0];
            ref var spawnedLasers = ref NPC.localAI[1];

            var ease = EasingHelper.QuadInOut(progress);

            var angle = MathHelper.Lerp(0, MathHelper.TwoPi, ease);
            Retinazer.Npc.rotation = MathHelper.WrapAngle(startingAngle + angle);
            
            
            var laserProgress = spawnedLasers / lasers;
            if (ease >= laserProgress && Main.netMode != NetmodeID.MultiplayerClient)
            {
                var rot = MathHelper.Lerp(0, MathHelper.TwoPi, laserProgress) + startingAngle + MathHelper.PiOver2;

                var scaleCurve = new PiecewiseCurve()
                    .Add(EasingCurves.Quadratic, EasingType.Out, 0f, 1f, 4f);

                new GlowStarParticle(Retinazer.Front, Vector2.Zero, rot, Color.White, 30)
                {
                    IgnoreLighting = true,
                    OnUpdate = p =>
                    {
                        var scale = scaleCurve.Evaluate(p.LifetimeRatio);
                        p.Scale = Vector2.One * scale;
                        p.AngularVelocity = p.LifetimeRatio * MathHelper.Pi / 16f;
                    }
                }.Spawn();
                
                spawnedLasers++;
            }
        });
        
        // Reset local ai
        anim.AddInstantEvent(laserSpinTime + 1, () => { NPC.localAI[1] = 0; });

        return anim;
    }

    private bool Attack_RetLaserBurst()
    {
        laserBurstAnimation ??= CreateRetLaserBurstAnimation();

        if (!laserBurstAnimation.RunAnimation()) return false;
        laserBurstAnimation.Reset();
        return true;
    }
}