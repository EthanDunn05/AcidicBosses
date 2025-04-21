using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

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
        return ProjHelper.NewUnscaledProjectile(Npc.GetSource_FromAI(), position, velocity,
            ProjectileID.QueenSlimeGelAttack, Npc.damage, 6f);
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