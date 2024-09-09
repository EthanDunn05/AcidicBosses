using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    private bool Attack_FlamethrowerChase()
    {
        attackManager.CountUp = true;

        Hover(10f, 0.2f);

        if (attackManager.AiTimer % 30 == 0)
        {
            NewSpazFlamethrower(Spazmatism.Front, MathHelper.Pi);
        }

        if (attackManager.AiTimer >= 60 * 5)
        {
            attackManager.CountUp = false;
            return true;
        }
        return false;
    }
}