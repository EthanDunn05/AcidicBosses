using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.KingSlime;

public class SlimeSpikeProjectile : ModProjectile
{
    public override void SetDefaults()
    {
        Projectile.width = 16;
        Projectile.height = 16;
        Projectile.hostile = true;
        Projectile.tileCollide = true;
        Projectile.penetrate = 3;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        // Bounce dust
        for (var i = 0; i < 20; i++)
        {
            var velY = (-Projectile.velocity.Y) * Main.rand.NextFloat(0.75f, 1f);
            var velX = Main.rand.NextFloat(-1f, 1f);
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Water, velX, velY);
        }
        
        // Limit Number of Bounces
        Projectile.penetrate--;
        if (Projectile.penetrate <= 0)
        {
            Projectile.Kill();
            return false;
        }
        
        // Bounce!
        Projectile.velocity.Y = -oldVelocity.Y * 0.8f;
        return false;
    }

    public override void AI()
    {
        Projectile.ai[0] += 1f; // Use a timer to wait 15 ticks before applying gravity.
        if (Projectile.ai[0] >= 15f)
        {
            // Gravity
            Projectile.ai[0] = 15f;
            Projectile.velocity.Y += 0.3f;
        }
        if (Projectile.velocity.Y > 16f) Projectile.velocity.Y = 16f;
        
        // Face the direction it's going
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        
        // Dust
        if (Main.rand.NextBool(3))
        {
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Water);
        }
    }
}