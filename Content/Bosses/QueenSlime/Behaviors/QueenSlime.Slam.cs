using AcidicBosses.Common.Textures;
using AcidicBosses.Core.Animation;
using AcidicBosses.Core.Graphics.Sprites;
using Luminance.Common.Easings;
using Microsoft.Xna.Framework;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public partial class QueenSlime
{
    private AcidAnimation? slamAnimation;

    private AcidAnimation PrepareSlamAnimation()
    {
        var anim = new AcidAnimation();

        anim.AddInstantEvent(0, () =>
        {
            Npc.noGravity = true;
            drawAfterimages = true;
            Npc.damage = 0;
            
            JumpTo(TargetPlayer.Center, 60);
            
            new FadingEffectLine(
                TextureRegistry.InvertedFadingGlowLine,
                Npc.Bottom + new Vector2(0, -50), MathHelper.PiOver2,
                250f, 30f,
                Color.White, 90
            )
            {
                OnUpdate = line =>
                {
                    line.Position = Npc.Bottom;
                }
            }.Spawn();
        });

        anim.AddSequencedEvent(30, (progress, frame) =>
        {
            anim.Data.Set("startPos", Npc.Bottom);
        });
        
        anim.AddSequencedEvent(30, (progress, frame) =>
        {
            flapping = true;
            FlyTo(TargetPlayer.Center + new Vector2(0f, -300f), 20f, 0.75f);
            anim.Data.Set("startPos", Npc.Bottom);
        });

        var slamPrepCurve = new PiecewiseCurve()
            .Add(EasingCurves.Quadratic, EasingType.Out, 50f, 1f);
        anim.AddSequencedEvent(30, (progress, frame) =>
        {
            var startPos = anim.Data.Get<Vector2>("startPos");
            Npc.velocity = Vector2.Zero;
            Npc.Bottom = startPos - new Vector2(0f, slamPrepCurve.Evaluate(progress));
        });
        
        anim.AddInstantEvent(91, () =>
        {
            Npc.damage = Npc.defDamage;
            grounded = false;
            flapping = false;
            
            Npc.noGravity = false;
            Npc.velocity = new Vector2(0f, 50f);
        });
        
        return anim;
    }
    
    private bool Attack_Slam()
    {
        slamAnimation ??= PrepareSlamAnimation();
        if (slamAnimation.RunAnimation() && grounded)
        {
            drawAfterimages = false;
            NewSmash(Npc.Center);
            slamAnimation.Reset();
            return true;
        }
        
        return false;
    }
}