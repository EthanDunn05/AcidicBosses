using AcidicBosses.Core.Animation;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    private AcidAnimation? laserSpreadAnimation;

    private AcidAnimation CreateLaserSpreadAnimation()
    {
        var anim = new AcidAnimation();

        const int lasers = 8;
        const int spreadLength = 30;
        const float spread = MathHelper.Pi / 6f;
        
        const string startingAngleKey = "startAngle";
        const string spawnedLasersKey = "spawnedLasers";
        
        anim.AddConstantEvent((progress, frame) =>
        {
            if (Spazmatism.Npc.active)
            {
                Hover(Spazmatism, 10f, 0.15f);
            }
        });

        var posTiming = anim.AddSequencedEvent(15, (progress, frame) =>
        {
            Retinazer.Npc.velocity = Vector2.Lerp(Retinazer.Npc.velocity, Vector2.Zero, progress);
            Retinazer.LookTowards(Main.player[NPC.target].Center, 0.5f);
        });
        
        anim.AddInstantEvent(posTiming.EndTime, () =>
        {
            anim.Data.Set(startingAngleKey, Retinazer.Npc.rotation);
            anim.Data.Set(spawnedLasersKey, 0);
        });

        anim.AddSequencedEvent(15, (progress, frame) =>
        {
            var startingAngle = anim.Data.Get<float>(startingAngleKey);
            
            var ease = EasingHelper.QuadIn(progress);
            Retinazer.Npc.rotation = MathHelper.Lerp(startingAngle, startingAngle - spread, ease);
        });

        var indicateTiming = anim.AddSequencedEvent(spreadLength, (progress, frame) =>
        {
            var startingAngle = anim.Data.Get<float>(startingAngleKey);
            var spawnedLasers = anim.Data.Get<int>(spawnedLasersKey);

            var ease = EasingHelper.QuadInOut(progress);

            var angle = MathHelper.Lerp(-spread, spread, ease);
            Retinazer.Npc.rotation = MathHelper.WrapAngle(startingAngle + angle);

            var laserProgress = (float) spawnedLasers / lasers;
            if (ease >= laserProgress && Main.netMode != NetmodeID.MultiplayerClient)
            {
                var rot = MathHelper.Lerp(-spread, spread, laserProgress) + startingAngle + MathHelper.PiOver2;
                NewRetLazer(Retinazer.Front, rot.ToRotationVector2() * 30f, rot, spreadLength);

                anim.Data.Set(spawnedLasersKey, spawnedLasers + 1);
            }
        });
            
        anim.AddInstantEvent(indicateTiming.EndTime, () => anim.Data.Set(spawnedLasersKey, 0));
        
        anim.AddSequencedEvent(spreadLength, (progress, frame) =>
        {
            var startingAngle = anim.Data.Get<float>(startingAngleKey);
            var spawnedLasers = anim.Data.Get<int>(spawnedLasersKey);
            
            var ease = EasingHelper.QuadInOut(progress);
            
            var angle = MathHelper.Lerp(-spread, spread, ease);
            Retinazer.Npc.rotation = MathHelper.WrapAngle(startingAngle + angle);
        });
        
        return anim;
    }
    
    private bool Attack_LaserSpread()
    {
        laserSpreadAnimation ??= CreateLaserSpreadAnimation();
        if (!laserSpreadAnimation.RunAnimation()) return false;
        laserSpreadAnimation.Reset();
        return true;
    }
}