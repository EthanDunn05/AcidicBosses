using AcidicBosses.Content.Bosses.Twins.Projectiles;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Content.Projectiles;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    private Projectile NewDashLine(Twin twin, Vector2 position, float offset, int lifetime, bool anchorToBoss = true)
    {
        var ai1 = anchorToBoss ? twin.Npc.whoAmI : -1;
        return BaseLineProjectile.Create<TwinsDashLine>(NPC.GetSource_FromAI(), position, offset, lifetime, ai1);
    }
    
    private Projectile NewAfterimage(Twin twin, Vector2 startPos, Vector2 endPos)
    {
        return NpcAfterimageTrail.Create(NPC.GetSource_FromAI(), startPos, endPos, twin.Npc.whoAmI);
    }

    private Projectile NewSpazFlamethrower(Vector2 pos, float rotation)
    {
        return BaseBetsyFlame.Create<SpazFlamethrower>(NPC.GetSource_FromAI(), pos, rotation - MathHelper.PiOver2, Spazmatism.Npc.damage, 4, Spazmatism.Npc.whoAmI);
    }

    private Projectile NewSpazFireball(Vector2 pos, Vector2 vel)
    {
        return ProjHelper.NewUnscaledProjectile(NPC.GetSource_FromAI(), pos, vel, ProjectileID.CursedFlameHostile,
            Spazmatism.Npc.damage, 3);
    }

    private Projectile NewRetDeathray(Vector2 position, float rotation, int lifetime)
    {
        return DeathrayBase.Create<RetDeathray>(NPC.GetSource_FromAI(), position, Retinazer.Npc.damage, 3, rotation,
            lifetime, Retinazer.Npc.whoAmI);
    }

    private Projectile NewRetSweepIndicator(Vector2 pos, float rotation, int lifetime)
    {
        return BaseSweep.Create<RetSweepIndicator>(NPC.GetSource_FromAI(), pos, rotation, lifetime,
            Retinazer.Npc.whoAmI);
    }
    
    private Projectile NewRetLazer(Vector2 pos, Vector2 vel, float rotation, int lifetime, int anchor = -1)
    {
        return BaseLineProjectile.Create<RetLaserIndicator>(NPC.GetSource_FromAI(), pos, vel, Retinazer.Npc.damage, 3, rotation, lifetime, anchor);
    }
}