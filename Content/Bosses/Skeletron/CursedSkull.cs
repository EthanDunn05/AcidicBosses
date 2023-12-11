using AcidicBosses.Common.Textures;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.Skeletron;

public class CursedSkull : ModProjectile
{
    public override string Texture => TextureRegistry.TerrariaProjectile(ProjectileID.Skull);
    public override string GlowTexture => Texture;
    
    private Vector2 oldVel;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 3;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
    }

    public override void SetDefaults()
    {
        // Copied from skull projectile code
        Projectile.width = 26;
        Projectile.height = 26;
        Projectile.alpha = 255;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
    }

    public override void AI()
    {
        Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0).ToDirectionInt();
        Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi);

        // Yes this is supposed to happen every frame
        Projectile.frame = ++Projectile.frame % Main.projFrames[Projectile.type];
        
        // Fire
        for (var i = 0; i < 2; i++)
        {
            var dust = Dust.NewDustDirect(new Vector2(Projectile.position.X + 4f, Projectile.position.Y + 4f), Projectile.width - 8, Projectile.height - 8, 6, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), 2f);
            dust.position -= Projectile.velocity;
            dust.noGravity = true;
            dust.velocity.X *= 0.3f;
            dust.velocity.Y *= 0.3f;
        }
    }

    public override Color? GetAlpha(Color lightColor)
    {
        return new Color(255, 255, 255, (int)Utils.WrappedLerp(0f, 255f, (float)(Projectile.timeLeft % 40) / 40f));
    }

    public override void Kill(int timeLeft)
    {
        // Just taken straight from vanilla
        for (int num398 = 0; num398 < 20; num398++)
        {
            int num399 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 26, 0f, 0f, 100);
            Main.dust[num399].noGravity = true;
            Dust dust21 = Main.dust[num399];
            Dust dust315 = dust21;
            dust315.velocity *= 1.2f;
            Main.dust[num399].scale = 1.3f;
            dust21 = Main.dust[num399];
            dust315 = dust21;
            dust315.velocity -= Projectile.oldVelocity * 0.3f;
            num399 = Dust.NewDust(new Vector2(Projectile.position.X + 4f, Projectile.position.Y + 4f), Projectile.width - 8, Projectile.height - 8, 5, 0f, 0f, 100, default(Color), 1.5f);
            Main.dust[num399].noGravity = true;
            dust21 = Main.dust[num399];
            dust315 = dust21;
            dust315.velocity *= 3f;
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        var texture = ModContent.Request<Texture2D>(Texture).Value;
        ProjHelper.DrawAfterimages(Projectile, texture, ref lightColor, 2);

        return true;
    }
}