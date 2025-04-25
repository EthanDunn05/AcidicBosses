using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.QueenSlime.Projectiles;

public class CrystalSpike : ModProjectile
{
    private short[] DustIds =
    [
        DustID.PurpleCrystalShard,
        DustID.PinkCrystalShard,
        DustID.BlueCrystalShard,
        DustID.PurpleCrystalShard,
        DustID.BlueCrystalShard,
    ];

    public override void SetDefaults()
    {
        Projectile.width = 32;
        Projectile.height = 32;
        Projectile.hostile = true;
        Projectile.alpha = 255;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
    }

    public override void AI()
    {
        // Taken from Sharp Tears AI
        const float dustVelocityMult = 1f;
        const int burstDustInstant = 10;
        const int endTime = 20;
        const int frames = 5;

        ref var timer = ref Projectile.ai[0];
        ref var scale = ref Projectile.ai[1];
        ref var doneFirstFrame = ref Projectile.localAI[0];

        var isDead = timer >= endTime;

        timer += 1f;

        if (doneFirstFrame == 0f)
        {
            doneFirstFrame= 1f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.frame = Main.rand.Next(frames);
            var dustId = DustIds[Projectile.frame];

            for (var i = 0; i < burstDustInstant; i++)
            {
                var dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f),
                    dustId,
                    Projectile.velocity * dustVelocityMult * MathHelper.Lerp(0.2f, 0.7f, Main.rand.NextFloat())
                );
                dust.velocity += Main.rand.NextVector2Circular(0.5f, 0.5f);
                dust.scale = 0.8f + Main.rand.NextFloat() * 0.5f;
            }

            SoundEngine.PlaySound(SoundID.DeerclopsIceAttack, Projectile.Center);
        }

        if (!isDead)
        {
            Projectile.Opacity += 0.1f;
            Projectile.scale = Projectile.Opacity * scale;
        }
        else
        {
            Projectile.Kill();
        }
    }

    public override void OnKill(int timeLeft)
    {
        for (var i = 0f; i < 1f; i += 0.025f)
        {
            var dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(16f, 16f) * Projectile.scale +
                Projectile.velocity.SafeNormalize(Vector2.UnitY) * i * 200f * Projectile.scale,
                DustIds[Projectile.frame], Main.rand.NextVector2Circular(3f, 3f));
            dust.velocity.Y += -0.3f;
            dust.velocity += Projectile.velocity * 0.2f;
            dust.scale = 1f;
            dust.alpha = 100;
        }
    }

    public override Color? GetAlpha(Color lightColor)
    {
        return Color.Lerp(lightColor, Color.Black, 0.25f);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float collisionPoint = 0f;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
            Projectile.Center + Projectile.velocity.SafeNormalize(-Vector2.UnitY) * 200f * Projectile.scale,
            22f * Projectile.scale, ref collisionPoint
        );
    }

    public override bool PreDraw(ref Color lightColor)
    {
        var dir = SpriteEffects.None;
        if (Projectile.spriteDirection == -1)
            dir = SpriteEffects.FlipHorizontally;

        var texture = ModContent.Request<Texture2D>(Texture).Value;
        var frame = texture.Frame(1, 5, 0, Projectile.frame);
        var origin = new Vector2(16f, frame.Height / 2);
        var alpha = Projectile.GetAlpha(lightColor);
        var scale = new Vector2(Projectile.scale);
        var growLerp = Utils.GetLerpValue(30f, 25f, Projectile.ai[0], clamped: true);

        scale.Y *= growLerp;

        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
            frame, alpha, Projectile.rotation, origin, scale, dir);
        return false;
    }

    public override bool? CanCutTiles()
    {
        return true;
    }

    public override bool ShouldUpdatePosition()
    {
        return false;
    }

    public override void CutTiles()
    {
        Utils.PlotTileLine(Projectile.Center,
            Projectile.Center + Projectile.velocity.SafeNormalize(-Vector2.UnitY) * 200f * Projectile.scale,
            22f * Projectile.scale, DelegateMethods.CutTiles);
    }
}