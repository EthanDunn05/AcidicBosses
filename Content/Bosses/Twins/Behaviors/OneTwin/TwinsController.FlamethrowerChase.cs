using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    private bool Attack_FlamethrowerChase()
    {
        attackManager.CountUp = true;

        var target = Main.player[NPC.target].Center;
        FlyTo(Spazmatism.Npc, target, 30, 0.2f);
        Spazmatism.LookTowards(target, 0.05f);
        
        if (attackManager.AiTimer % 30 == 0 && attackManager.AiTimer < 60 * 4)
        {
            var pos = Spazmatism.Npc.Center + (Spazmatism.Npc.rotation + MathHelper.PiOver2).ToRotationVector2() * Spazmatism.Npc.width / 3f;
            NewSpazFlamethrower(pos, MathHelper.Pi);
        }

        if (attackManager.AiTimer >= 60 * 5)
        {
            attackManager.CountUp = false;
            return true;
        }
        return false;
    }
}