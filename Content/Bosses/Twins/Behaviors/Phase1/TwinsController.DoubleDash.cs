using AcidicBosses.Helpers.NpcHelpers;
using Microsoft.Xna.Framework;
using Terraria;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    private DashState Attack_Dash(Twin twin, int dashLength, float speed, int trackTime, int dashAtTime, float distance)
    {
        var options = new DashOptions
        {
            MinimumDistance = distance,
            DashSpeed = speed,
            DashLength = dashLength,
            LookOffset = MathHelper.PiOver2,
            TrackTime = trackTime,
            DashAtTime = dashAtTime
        };
        
        var target = Main.player[NPC.target];

        var dashState = Dash(twin, twin.AttackManager, target.Center, options);

        return dashState;
    }
    
    private DashState Attack_TrackingDash(Twin twin, int dashLength, float speed, int trackTime, int dashAtTime, float distance)
    {
        var options = new DashOptions
        {
            MinimumDistance = distance,
            DashSpeed = speed,
            DashLength = dashLength,
            LookOffset = MathHelper.PiOver2,
            TrackTime = trackTime,
            DashAtTime = dashAtTime
        };
        
        var npc = twin.Npc;
        var target = Main.player[NPC.target];
        
        var trackedPos = target.Center;
        trackedPos += target.velocity * trackTime; // Lead player pos
        var travelTime = npc.Distance(trackedPos) / speed;
        trackedPos += target.velocity * travelTime; // Account for travel time

        var dashState = Dash(twin, twin.AttackManager, trackedPos, options);
        
        return dashState;
    }
    
    private bool Attack_DoubleDash(int dashLength, float speed, int trackTime, int dashAtTime)
    {
        attackManager.CountUp = true;

        ref var spazDone = ref NPC.localAI[0];
        ref var retDone = ref NPC.localAI[1];
        if (attackManager.AiTimer == 0)
        {
            Spazmatism.AttackManager.AiTimer = 0;
            Retinazer.AttackManager.AiTimer = 0;
            spazDone = 0;
            retDone = 0;
        }
        
        // Spaz has a normal dash while Ret tracks the player and dashes later
        if (spazDone == 0)
        {
            var spazDashState = Attack_Dash(Spazmatism, dashLength, speed, trackTime, dashAtTime, 300);
            if (spazDashState == DashState.Done) spazDone = 1;
        }

        if (retDone == 0)
        {
            var retDashState = Attack_TrackingDash(Retinazer, dashLength, speed, trackTime * 2, (int) (dashAtTime * 2f), 300);
            if (retDashState == DashState.Done) retDone = 1;
        }
        
        if (spazDone != 0) Hover(Spazmatism, 20, 0.25f);
        if (retDone != 0) Hover(Retinazer, 20, 0.25f);

        if (spazDone == 0 || retDone == 0) return false;
        
        spazDone = 0;
        retDone = 0;
        attackManager.CountUp = false;
        return true;

    }
}