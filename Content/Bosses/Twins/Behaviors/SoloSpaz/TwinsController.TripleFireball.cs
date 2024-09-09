using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    public bool Attack_TripleFireball()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return true;

        var offset = MathHelper.Pi / 12f;
        for (var i = -1; i <= 1; i++)
        {
            var angle = Spazmatism.Npc.rotation + MathHelper.PiOver2;
            angle += offset * i;
            NewSpazFireball(Spazmatism.Front, angle.ToRotationVector2() * 5f);
        }
        
        return true;
    }
}