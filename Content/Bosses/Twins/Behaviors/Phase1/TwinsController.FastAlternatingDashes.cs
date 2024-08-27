using AcidicBosses.Helpers.NpcHelpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    private bool Attack_AlternatingFastDashes(int length, int dashLength, float speed, int trackTime, int dashAtTime)
    {
        attackManager.CountUp = true;
        
        if (attackManager.AiTimer == 0) SoundEngine.PlaySound(SoundID.ForceRoarPitched);
        
        ref var turn = ref NPC.localAI[0];
        Twin dashingTwin = turn == 0 ? Spazmatism : Retinazer;
        Twin notDashingTwin = turn == 0 ? Retinazer : Spazmatism;
        
        var target = Main.player[NPC.target];
        var dashOptions = new DashOptions
        {
            DashLength = dashLength,
            DashSpeed = speed,
            LookOffset = MathHelper.PiOver2,
            DashAtTime = dashAtTime,
            MinimumDistance = 0,
            DontReposition = true,
            TrackTime = trackTime
        };

        if (dashingTwin.AttackManager.AiTimer == 0)
        {
            var dest = new Vector2();
            if (dashingTwin is Spazmatism) dest = target.Center + new Vector2(0, 350);
            else dest = target.Center + new Vector2(0, -350);
            
            dashingTwin.LookTowards(dest, 1f);
            Teleport(dashingTwin, dest, 0);
            dashingTwin.LookTowards(target.Center, 1f);

            dashingTwin.Npc.velocity = Vector2.Zero;
        }
        
        var dashState = Dash(dashingTwin, dashingTwin.AttackManager, target.Center, dashOptions);

        if (dashState == DashState.Done)
        {
            turn = ((int) turn + 1) % 2;
            dashingTwin.AttackManager.Reset();

            if (attackManager.AiTimer >= length)
            {
                attackManager.CountUp = false;
                return true;
            }
        }

        return false;
    }
}