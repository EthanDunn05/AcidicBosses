using System;
using AcidicBosses.Core.Animation;
using AcidicBosses.Helpers;
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

        const int circleTime = 60;
        
        const float distance = 450;
        const float fireballSpeed = 10f;
        
        // Teleport into position
        anim.AddInstantEvent(0, () =>
        {
            var target = Main.player[NPC.target].Center;
            Spazmatism.Npc.rotation = MathHelper.Pi;
            Teleport(Spazmatism, target + new Vector2(distance, 0), 0f);

            NPC.localAI[0] = target.X;
            NPC.localAI[1] = target.Y;

            SoundEngine.PlaySound(SoundID.ForceRoarPitched, Spazmatism.Npc.Center);
        });
        
        // Circle around the player
        anim.AddTimedEvent(0, circleTime, (progress, frame) =>
        {
            ref var targetX = ref NPC.localAI[0];
            ref var targetY = ref NPC.localAI[1];
            var target = new Vector2(targetX, targetY);
            
            var ease = EasingHelper.QuadOut(progress);
            var npc = Spazmatism.Npc;

            var x = MathF.Cos(ease * MathHelper.TwoPi) * distance;
            var y = MathF.Sin(ease * MathHelper.TwoPi) * distance;
            var rot = MathHelper.WrapAngle(ease * MathHelper.TwoPi);

            npc.Center = new Vector2(x, y) + target;
            npc.rotation = rot;
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