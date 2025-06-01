using AcidicBosses.Common.Textures;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.QueenSlime.Projectiles;

public class QueenSlimeSlamTrail : ModProjectile
{
    public override string Texture => TextureRegistry.InvisPath;

    public override void SetDefaults()
    {
        Projectile.hostile = true;
        Projectile.aiStyle = 0;
        Projectile.timeLeft = 5;
    }

    public static Projectile Create(IEntitySource source, Vector2 bottomPos, float height, int damage, float kb)
    {
        return ProjHelper.NewUnscaledProjectile(source, bottomPos, Vector2.Zero,
            ModContent.ProjectileType<QueenSlimeSlamTrail>(), damage, kb, ai0: height);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        var point = 0f;

        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.position,
            Projectile.position - new Vector2(0, Projectile.ai[0]), 40, ref point);
    }
}