using AcidicBosses.Common.Textures;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.Skeletron;

public class Muramasa : ModProjectile
{
    public override string Texture => TextureRegistry.TerrariaItem(ItemID.Muramasa);
    public override string GlowTexture => Texture;

    private Vector2 oldVel = Vector2.Zero;
    private bool setOldVel = false;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
    }

    public override void SetDefaults()
    {
        Projectile.scale = 1.5f;
        Projectile.width = (int) (60 * 1.5);
        Projectile.height = (int) (60 * 1.5);
        Projectile.alpha = 255;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
    }

    public override void AI()
    {
        const int rotationTime = 60;

        ref var aiTime = ref Projectile.ai[0];

        if (!setOldVel)
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            oldVel = Projectile.velocity;
            Projectile.velocity = Vector2.Zero;
            setOldVel = true;
            Projectile.damage = (int) (Projectile.damage * 0.25f);
        }

        if (aiTime == 0)
        {
            SoundEngine.PlaySound(SoundID.Item1);
        }

        // Spin
        if (aiTime < rotationTime)
        {
            var baseRot = oldVel.ToRotation() + MathHelper.PiOver4;
            var rotT = EasingHelper.QuadOut(aiTime / rotationTime);
            var offset = MathHelper.TwoPi * rotT;
            Projectile.rotation = MathHelper.WrapAngle(baseRot + offset);
        }
        else
        {
            Projectile.velocity = oldVel;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Projectile.damage = Projectile.originalDamage;
        }

        aiTime++;
    }

    // Glow
    public override Color? GetAlpha(Color lightColor)
    {
        return new Color(255, 255, 255, 255);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        var texture = ModContent.Request<Texture2D>(Texture).Value;
        lightColor = (Color) GetAlpha(lightColor);
        ProjHelper.DrawAfterimages(Projectile, texture, ref lightColor, 2);
        ProjHelper.Draw(Projectile, texture, ref lightColor);

        return false;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        var point = 0f;

        // Offset from center to top right
        var centerOffset = projHitbox.Size() / 2;
        centerOffset.Y *= -1;
        centerOffset = centerOffset.RotatedBy(Projectile.rotation);

        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center + centerOffset, Projectile.Center - centerOffset, 15, ref point
        );
    }
}