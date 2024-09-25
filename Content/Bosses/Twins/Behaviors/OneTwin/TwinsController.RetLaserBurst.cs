using System;
using AcidicBosses.Content.Particles;
using AcidicBosses.Core.Animation;
using AcidicBosses.Helpers;
using Luminance.Common.Easings;
using Luminance.Common.Utilities;
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

        const int lasers = 24;
        const int spinLength = 60;

        const string startingAngleKey = "startAngle";
        const string spawnedLasersKey = "spawnedLasers";

        anim.AddConstantEvent((progress, frame) =>
        {
            if (!Spazmatism.Npc.active) return;
            Hover(Spazmatism, 10f, 0.15f);
        });

        anim.AddInstantEvent(0, () =>
        {
            anim.Data.Set(startingAngleKey, Retinazer.Npc.rotation);
            anim.Data.Set(spawnedLasersKey, 0);

            Retinazer.Npc.velocity = Vector2.Zero;
        });

        // Indicators
        var indicatorTiming = anim.AddSequencedEvent(spinLength, (progress, frame) =>
        {
            var startingAngle = anim.Data.Get<float>(startingAngleKey);
            var spawnedLasers = anim.Data.Get<int>(spawnedLasersKey);

            var ease = EasingHelper.QuadInOut(progress);

            var angle = MathHelper.Lerp(0, MathHelper.TwoPi, ease);
            Retinazer.Npc.rotation = MathHelper.WrapAngle(startingAngle + angle);

            var laserProgress = (float) spawnedLasers / lasers;
            if (ease >= laserProgress && Main.netMode != NetmodeID.MultiplayerClient)
            {
                var rot = MathHelper.Lerp(0, MathHelper.TwoPi, laserProgress) + startingAngle + MathHelper.PiOver2;
                NewRetLazer(Retinazer.Front, rot.ToRotationVector2() * 30f, rot, spinLength);
                
                anim.Data.Set(spawnedLasersKey, spawnedLasers + 1);
            }

            if (!Spazmatism.Npc.active) return;
            if (frame % 15 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NewSpazFireball(Spazmatism.Npc.Center, Spazmatism.Npc.Center.DirectionTo(Main.player[NPC.target].Center).RotatedBy(Main.rand.NextFloat(-0.5f, 0.5f)) * 10f);
            }
        });

        // Reset laser count
        anim.AddInstantEvent(indicatorTiming.EndTime, () => anim.Data.Set(spawnedLasersKey, 0));

        // Spin with the lasers
        anim.AddSequencedEvent(spinLength, (progress, frame) =>
        {
            var startingAngle = anim.Data.Get<float>(startingAngleKey);

            var ease = EasingHelper.QuadInOut(progress);

            var angle = MathHelper.Lerp(0, MathHelper.TwoPi, ease);
            Retinazer.Npc.rotation = MathHelper.WrapAngle(startingAngle + angle);
        });

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