using AcidicBosses.Common.Textures;
using AcidicBosses.Content.Particles.Animated;
using AcidicBosses.Core.Animation;
using AcidicBosses.Core.Graphics.Sprites;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public partial class QueenSlime
{
    private AcidAnimation? laserBurstAnimation;

    private AcidAnimation PrepareLaserBurstAnimation()
    {
        var anim = new AcidAnimation();

        anim.AddSequencedEvent(60, (progress, frame) =>
        {
            if (frame != 1) return;
            Npc.noGravity = true;
            Npc.velocity = Vector2.Zero;
            if (!grounded) flapping = true;
            
            anim.Data.Set("target", TargetPlayer.Center);

            SoundEngine.PlaySound(SoundID.Item8, GetCrownPos());
            new GatherEnergyParticle(GetCrownPos(), Vector2.Zero, 0f, Color.Violet, 60) { Scale = new Vector2(4) }.Spawn();
            var angle = GetCrownPos().DirectionTo(TargetPlayer.Center);
            new FadingEffectLine(
                TextureRegistry.GlowLine,
                GetCrownPos(),
                angle.ToRotation(),
                12000,
                5,
                Color.Violet,
                60
            ).Spawn();
        });
        
        anim.AddSequencedEvent(30, (progress, frame) =>
        {
            if (frame != 0) return;
            var target = anim.Data.Get<Vector2>("target");

            var angle = GetCrownPos().DirectionTo(target);
            NewDeathray(GetCrownPos(), angle.ToRotation(), 60);
            
            anim.Data.Set("target", TargetPlayer.Center);
            
            angle = GetCrownPos().DirectionTo(TargetPlayer.Center);
            new FadingEffectLine(
                TextureRegistry.GlowLine,
                GetCrownPos(),
                angle.ToRotation(),
                12000,
                5,
                Color.Violet,
                30
            ).Spawn();
        });
        
        anim.AddSequencedEvent(30, (progress, frame) =>
        {
            if (frame != 0) return;
            var target = anim.Data.Get<Vector2>("target");

            var angle = GetCrownPos().DirectionTo(target);
            NewDeathray(GetCrownPos(), angle.ToRotation(), 60);
            
            anim.Data.Set("target", TargetPlayer.Center);
            
            angle = GetCrownPos().DirectionTo(TargetPlayer.Center);
            new FadingEffectLine(
                TextureRegistry.GlowLine,
                GetCrownPos(),
                angle.ToRotation(),
                12000,
                5,
                Color.Violet,
                30
            ).Spawn();
        });
        
        anim.AddSequencedEvent(60, (progress, frame) =>
        {
            if (frame != 0) return;
            var target = anim.Data.Get<Vector2>("target");

            var angle = GetCrownPos().DirectionTo(target);
            NewDeathray(GetCrownPos(), angle.ToRotation(), 60);
        });
        
        return anim;
    }

    private bool Attack_LaserBurst()
    {
        laserBurstAnimation ??= PrepareLaserBurstAnimation();
        if (laserBurstAnimation.RunAnimation())
        {
            Npc.noGravity = false;
            flapping = false;
            
            laserBurstAnimation.Reset();
            return true;
        }

        return false;
    }
}