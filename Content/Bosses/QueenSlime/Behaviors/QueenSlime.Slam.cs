using System;
using AcidicBosses.Common.Textures;
using AcidicBosses.Core.Animation;
using AcidicBosses.Core.Graphics.Sprites;
using AcidicBosses.Core.StateManagement;
using AcidicBosses.Helpers;
using Luminance.Common.Easings;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public partial class QueenSlime
{
    private AcidAnimation? slamAnimation;

    private AcidAnimation PrepareSlamAnimation()
    {
        var anim = new AcidAnimation();

        anim.AddInstantEvent(0, () =>
        {
            drawAfterimages = true;
            Npc.damage = 0;
            
            JumpTo(TargetPlayer.Center + new Vector2(0f, -250f), 45);
            
            // new FadingEffectLine(
            //     TextureRegistry.InvertedFadingGlowLine,
            //     Npc.Bottom + new Vector2(0, -50), MathHelper.PiOver2,
            //     300f, 40f,
            //     Color.White, 90
            // )
            // {
            //     OnUpdate = line =>
            //     {
            //         line.Position = Npc.Bottom;
            //     }
            // }.Spawn();
        });

        anim.AddSequencedEvent(30, (progress, frame) =>
        {
            anim.Data.Set("startPos", Npc.Bottom);
        });
        
        anim.AddSequencedEvent(30, (progress, frame) =>
        {
            flapping = true;
            Npc.noGravity = true;
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
            grounded = true;
            flapping = false;
            drawAfterimages = false;
            
            Npc.noGravity = false;

            var startY = Npc.Bottom.Y;

            var searchPos = new Vector2(Npc.Bottom.X, TargetPlayer.Center.Y);
            var ground = AcidUtils.FindGroundVertical(searchPos.ToTileCoordinates()).ToWorldCoordinates();
            Teleport(ground);
            
            NewSlamTrail(Npc.Bottom, Npc.Bottom.Y - startY);
            OnLand(50f);
        });

        anim.AddSequencedEvent(12, (progress, frame) =>
        {
            var scale = MathHelper.Lerp(0.25f, 1.25f, EasingHelper.QuadOut(progress));
            var angle = -MathHelper.PiOver2;
            var rightPos = Npc.Center + new Vector2(32, 0) * frame;
            rightPos = AcidUtils.FindGroundVertical(rightPos.ToTileCoordinates()).ToWorldCoordinates();
            NewCrystalSpike(rightPos - new Vector2(0, 8 * scale), angle, scale);

            var leftPos = Npc.Center - new Vector2(32, 0) * frame;
            leftPos = AcidUtils.FindGroundVertical(leftPos.ToTileCoordinates()).ToWorldCoordinates();
            NewCrystalSpike(leftPos - new Vector2(0, 8 * scale), angle, scale);
        });
        
        return anim;
    }
    
    private bool Attack_Slam(bool useSpikes)
    {
        slamAnimation ??= PrepareSlamAnimation();
        slamAnimation.Data.Set("useSpikes", useSpikes);
        if (slamAnimation.RunAnimation())
        {
            slamAnimation.Reset();
            return true;
        }
        
        return false;
    }
}