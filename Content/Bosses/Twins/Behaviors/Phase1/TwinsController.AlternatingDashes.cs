using AcidicBosses.Helpers.NpcHelpers;
using Microsoft.Xna.Framework;
using Terraria;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    private bool Attack_AlternatingDashes(int length, int dashLength, float speed, int trackTime, int dashAtTime)
    {
        attackManager.CountUp = true;
        
        ref var turn = ref NPC.localAI[0];
        Twin dashingTwin = turn == 0 ? Spazmatism : Retinazer;
        Twin notDashingTwin = turn == 0 ? Retinazer : Spazmatism;

        Hover(notDashingTwin, 20f, 0.2f);
        
        var target = Main.player[NPC.target];
        var dashOptions = new DashOptions
        {
            DashLength = dashLength,
            DashSpeed = speed,
            LookOffset = MathHelper.PiOver2,
            DashAtTime = dashAtTime,
            MinimumDistance = 250,
            TrackTime = trackTime
        };

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