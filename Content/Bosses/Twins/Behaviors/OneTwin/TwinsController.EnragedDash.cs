using AcidicBosses.Helpers.NpcHelpers;
using Microsoft.Xna.Framework;
using Terraria;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    private bool Attack_EnragedDash()
    {
        var dashOptions = new DashOptions
        {
            DashLength = 15,
            DashSpeed = 30,
            LookOffset = MathHelper.PiOver2,
            MinimumDistance = 200,
            TrackTime = 10,
            DashAtTime = 20
        };

        var state = Dash(AliveTwin()!, attackManager, Main.player[NPC.target].Center, dashOptions);

        return state == DashState.Done;
    }
}