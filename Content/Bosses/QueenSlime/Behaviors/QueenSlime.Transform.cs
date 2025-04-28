using AcidicBosses.Common.Effects;
using AcidicBosses.Content.Particles.Animated;
using AcidicBosses.Core.Animation;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public partial class QueenSlime
{
    private AcidAnimation? transformAnimation;

    private AcidAnimation PrepareTransformAnimation()
    {
        var anim = new AcidAnimation();

        var positionTiming = anim.AddSequencedEvent(60, (progress, frame) =>
        {
            flapping = true;
            Npc.noGravity = true;
            Npc.damage = 0;
            FlyTo(TargetPlayer.Center - new Vector2(0f, 250f), 20f, 1f);
        });

        var transTiming = anim.AddSequencedEvent(30, (progress, frame) =>
        {
            Npc.velocity = Vector2.Zero;
            drawExtraWings = true;

            if (frame == 0)
            {
                SoundEngine.PlaySound(Npc.DeathSound, Npc.Center);
                ScreenShakeSystem.StartShakeAtPoint(Npc.Center, 10f);
                new BigSmokeDisperseParticle(Npc.Top, Vector2.Zero, 0f, Color.Violet, 30)
                {
                    Scale = new Vector2(6f)
                }.Spawn();

                for (var i = 0; i < 50; i++)
                {
                    var angle = Main.rand.NextVector2Circular(10, 10);
                    Dust.NewDust(Npc.Center, 0, 0, DustID.PinkSlime, angle.X, angle.Y);
                }
            }
            EffectsManager.ShockwaveActivate(Npc.Center, 0.075f, 0.15f, Color.Transparent, progress);
        });
        
        anim.AddInstantEvent(transTiming.EndTime, () =>
        {
            flapping = false;
            Npc.noGravity = false;
            Npc.damage = Npc.defDamage;
        });

        return anim;
    }
}