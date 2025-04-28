using AcidicBosses.Common.Configs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public class BouncyGelOverride : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        return entity.type == ProjectileID.QueenSlimeMinionPinkBall && BossToggleConfig.Get().EnableQueenSlime;
    }

    public override void SetDefaults(Projectile entity)
    {
        if (!ShouldOverride()) return;
        
        // This should make them more bearable
        entity.penetrate = 2;
    }

    public bool ShouldOverride()
    {
        return BossToggleConfig.Get().EnableQueenSlime && !AcidicBosses.DisableReworks();
    }
}