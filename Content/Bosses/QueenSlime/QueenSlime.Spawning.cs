using AcidicBosses.Content.Bosses.QueenSlime.Projectiles;
using AcidicBosses.Content.Bosses.Twins;
using AcidicBosses.Content.Bosses.Twins.Projectiles;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public partial class QueenSlime
{
    private Projectile? NewSmash(Vector2 position)
    {
        if (!AcidUtils.IsServer()) return null;
        return ProjHelper.NewUnscaledProjectile(Npc.GetSource_FromAI(), position, Vector2.Zero,
            ProjectileID.QueenSlimeSmash, Npc.damage, 6f);
    }

    private Projectile? NewRoyalGel(Vector2 position, Vector2 velocity)
    {
        if (!AcidUtils.IsServer()) return null;
        var proj = ProjHelper.NewUnscaledProjectile(Npc.GetSource_FromAI(), position, velocity,
            ProjectileID.QueenSlimeGelAttack, Npc.damage, 6f);
        proj.velocity = velocity;
        return proj;
    }
    
    private Projectile NewDeathray(Vector2 position, float rotation, int lifetime)
    {
        return DeathrayBase.Create<QueenSlimeDeathray>(Npc.GetSource_FromAI(), position, Npc.damage * 2, 3, rotation,
            lifetime, Npc.whoAmI);
    }

    private Projectile? NewCrystalVile(Vector2 position, float angle)
    {
        if (!AcidUtils.IsServer()) return null;
        var pos = position - (angle.ToRotationVector2() * 30);
        var proj = ProjHelper.NewUnscaledProjectile(Npc.GetSource_FromAI(), pos, angle.ToRotationVector2() * 30f,
            ProjectileID.CrystalVileShardShaft, Npc.damage, 6f);
        proj.hostile = true;
        return proj;
    }
    
    private Projectile? NewCrystalShard(Vector2 position, Vector2 velocity)
    {
        if (!AcidUtils.IsServer()) return null;
        var proj = ProjHelper.NewUnscaledProjectile(Npc.GetSource_FromAI(), position, velocity,
            ProjectileID.CrystalShard, Npc.damage, 3f);
        return proj;
    }
    
    private Projectile? NewCrystalSpike(Vector2 position, float angle, float scale)
    {
        if (!AcidUtils.IsServer()) return null;
        var proj = ProjHelper.NewUnscaledProjectile(Npc.GetSource_FromAI(), position, angle.ToRotationVector2(),
            ModContent.ProjectileType<CrystalSpike>(), Npc.damage, 4f, ai1: scale);
        return proj;
    }

    private NPC? NewHeavenlySlime(Vector2 position)
    {
        if (!AcidUtils.IsServer()) return null;

        return NPC.NewNPCDirect(
            Npc.GetSource_FromAI(),
            position,
            NPCID.QueenSlimeMinionPurple,
            Npc.whoAmI
        );
    }
    
    private NPC? NewBouncySlime(Vector2 position)
    {
        if (!AcidUtils.IsServer()) return null;

        return NPC.NewNPCDirect(
            Npc.GetSource_FromAI(),
            position,
            NPCID.QueenSlimeMinionPink,
            Npc.whoAmI
        );
    }
    
    private NPC? NewCrystalSlime(Vector2 position)
    {
        if (!AcidUtils.IsServer()) return null;

        return NPC.NewNPCDirect(
            Npc.GetSource_FromAI(),
            position,
            NPCID.QueenSlimeMinionBlue,
            Npc.whoAmI
        );
    }
    
    private NPC? NewRandomSlime(Vector2 position)
    {
        if (!AcidUtils.IsServer()) return null;
        
        return Main.rand.Next(3) switch
        {
            0 => NewHeavenlySlime(position),
            1 => NewBouncySlime(position),
            2 => NewCrystalSlime(position),
            _ => null
        };
    }
}