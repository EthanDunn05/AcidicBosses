using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.ProjectileBases;

public abstract class DeathrayBase : ModProjectile
{
    public float Offset => Projectile.ai[0];

    private Vector2? startOffset;

    public int AnchorTo => (int) Projectile.ai[1];
    
    public abstract float Distance { get; }
    
    
    protected abstract int CollisionWidth { get; }
    
    protected abstract Color Color { get; }
    
    protected abstract Asset<Texture2D> DrTexture { get; }
    public virtual int Frames => 1;

    protected virtual bool AnchorPosition => true;
    protected virtual bool AnchorRotation => true;

    private bool doneFirstFrame = false;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = (int) Distance;
    }

    public override void SetDefaults()
    {
        Projectile.netImportant = true;
        Projectile.width = 0;
        Projectile.height = 0;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
    }

    public virtual void FirstFrame()
    {
        SoundEngine.PlaySound(SoundID.Zombie104, Projectile.Center);
    }
    
    public override void AI()
    {
        if (!doneFirstFrame)
        {
            FirstFrame();
            doneFirstFrame = true;
        }

        if(AnchorTo > 0)
        {
            var owner = Main.npc[AnchorTo];
            if (owner != null)
            {
                startOffset ??= owner.Center - Projectile.position;
                
                if (AnchorRotation)
                    Projectile.rotation = owner.rotation + Offset;
                else Projectile.rotation = Offset;
                
                if (AnchorPosition)
                    Projectile.position = (Vector2) (owner.Center + startOffset);
            }
        }
        else
        {
            Projectile.rotation = Offset;
        }

        
        var rotation = Projectile.rotation.ToRotationVector2();
        for (var i = 0; i < Distance; i += CollisionWidth)
        {

            var position = Projectile.position + rotation * i * CollisionWidth;
            var rect = new Rectangle();
            rect.X = (int) position.X;
            rect.Y = (int) position.Y;
            rect.Inflate(CollisionWidth / 2, CollisionWidth / 2);

            var randPos = Main.rand.NextVector2FromRectangle(rect);
            SpawnDust(randPos);
        }
    }
    
    protected virtual void SpawnDust(Vector2 position) {}

    public override bool PreDraw(ref Color lightColor)
    {
        // Have to rotate the visual for vertically aligned stuff
        var rotation = Projectile.rotation.ToRotationVector2();
        var r = Projectile.rotation - MathHelper.PiOver2;

        var frames = Main.projFrames[Type];
        var headRect = DrTexture.Frame(frames, 3, Projectile.frame, 0);
        var bodyRect = DrTexture.Frame(frames, 3, Projectile.frame, 1);
        var tailRect = DrTexture.Frame(frames, 3, Projectile.frame, 2);

        var step = bodyRect.Height;

        // Draw the head
        Main.spriteBatch.Draw(DrTexture.Value, Projectile.position - Main.screenPosition, headRect, Color, r, headRect.Center(), 1f, SpriteEffects.None, 0f);
        
        // Body
        for (var i = 1; i <= Distance / step - 1; i++)
        {
            var position = Projectile.position + rotation * i * step;
            Main.spriteBatch.Draw(DrTexture.Value, position - Main.screenPosition, bodyRect, Color, r, headRect.Center(), 1f, SpriteEffects.None, 0f);
        }
        
        // Tail
        Main.spriteBatch.Draw(DrTexture.Value, (Projectile.position + rotation * Distance) - Main.screenPosition, tailRect, Color, r, tailRect.Center(), 1f, SpriteEffects.None, 0f);

        return false;
    }

    // Change the way of collision check of the projectile
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
        var point = 0f;
        
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
            Projectile.Center + Projectile.rotation.ToRotationVector2() * Distance, CollisionWidth, ref point);
    }
}