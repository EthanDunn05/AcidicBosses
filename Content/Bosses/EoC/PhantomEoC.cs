using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.EoC;

public class PhantomEoC : ModProjectile
{
    // Steal the EoC's drip
    public override string Texture => $"Terraria/Images/NPC_{NPCID.EyeofCthulhu}";

    private int MoveDelay => (int) Projectile.ai[0];

    private int aiTimer = 0;
    private Vector2 oldVel;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 6;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
    }

    public override void SetDefaults()
    {
        // Same as EoC
        Projectile.width = 100;
        Projectile.height = 110;
        Projectile.tileCollide = false;
        Projectile.hostile = true;
        Projectile.netImportant = true;
    }

    public override void AI()
    {
        if (aiTimer == 0)
        {
            oldVel = Projectile.velocity;
            Projectile.velocity = Vector2.Zero;
        }
        
        Projectile.rotation = oldVel.ToRotation() - MathHelper.PiOver2;

        if (aiTimer == MoveDelay)
        {
            Projectile.velocity = oldVel;
        }

        // EoC's Mouth Animation
        Projectile.frameCounter++;
        if (Projectile.frameCounter < 7.0)
        {
            Projectile.frame = 3;
        }
        else if (Projectile.frameCounter < 14.0)
        {
            Projectile.frame = 4;
        }
        else if (Projectile.frameCounter < 21.0)
        {
            Projectile.frame = 5;
        }
        else
        {
            Projectile.frameCounter = 0;
            Projectile.frame = 3;
        }

        aiTimer++;
    }

    public override void Kill(int timeLeft)
    {
        // Yoinked from vanilla code
        SoundEngine.PlaySound(SoundID.NPCDeath1, Projectile.Center);
        
        Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, Main.rand.NextVector2Square(-30, 31) * 0.2f, 7);
        Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, Main.rand.NextVector2Square(-30, 31) * 0.2f, 7);

        for (var i = 0; i < 20; i++)
        {
            var speed = Main.rand.NextVector2Square(-30, 31) * 0.2f;
            Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, DustID.Blood, speed.X, speed.Y);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        var effects = SpriteEffects.None;
        if (Projectile.spriteDirection == 1)
            effects = SpriteEffects.FlipHorizontally;
        
        var eyeTexture = TextureAssets.Npc[NPCID.EyeofCthulhu].Value;
        var eyeFrame = eyeTexture.Frame(verticalFrames: Main.projFrames[Projectile.type], frameY: Projectile.frame);
        var eyeOrigin = eyeTexture.Size() / new Vector2(1f, Main.projFrames[Projectile.type]) * 0.5f;
        
        // Afterimages
        for (var i = 1; i < Projectile.oldPos.Length; i += 2)
        {
            // Adapted from EoC NPC Afterimages
            var fade = 0.5f * (10 - i) / 20f;
            var afterImageColor = Color.Multiply(lightColor, fade);
            
            var pos = Projectile.oldPos[i] + new Vector2(Projectile.width, Projectile.height) / 2f - Main.screenPosition;
            Main.spriteBatch.Draw(eyeTexture, pos, eyeFrame, afterImageColor, Projectile.rotation, eyeOrigin, Projectile.scale, effects, 0f);
        }

        return true;
    }
}