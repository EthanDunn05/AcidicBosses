using AcidicBosses.Helpers.NpcHelpers;
using Microsoft.Xna.Framework;
using Terraria;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    private bool Attack_PlusDash(int dashLength, float speed, int dashAtTime)
    {
        var dashOptions = new DashOptions
        {
            DashLength = dashLength,
            DashSpeed = speed,
            LookOffset = MathHelper.PiOver2,
            DashAtTime = dashAtTime,
            MinimumDistance = 0,
            DontReposition = true,
            TrackTime = 0
        };

        var target = Main.player[NPC.target];

        if (attackManager.AiTimer == 0)
        {
            var spazPos = target.Center + new Vector2(350, 0);
            if (Spazmatism.Npc.Center.X < target.Center.X) spazPos = target.Center - new Vector2(350, 0);
            var retPos = target.Center + new Vector2(0, 350);
            if (Retinazer.Npc.Center.Y > target.Center.Y) retPos = target.Center - new Vector2(0, 350);
            
            Spazmatism.LookTowards(spazPos, 1f);
            Retinazer.LookTowards(retPos, 1f);
            
            Teleport(Spazmatism, spazPos, 0);
            Teleport(Retinazer, retPos, 0);
            
            Spazmatism.LookTowards(target.Center, 1f);
            Retinazer.LookTowards(target.Center, 1f);
            
            Spazmatism.Npc.velocity = Vector2.Zero;
            Retinazer.Npc.velocity = Vector2.Zero;
        }
        
        // Dashes are perfectly synced together because there's no repositioning
        var dashState = Dash(Spazmatism, attackManager, target.Center, dashOptions);
        Dash(Retinazer, attackManager, target.Center, dashOptions);

        return dashState == DashState.Done;
    }
}