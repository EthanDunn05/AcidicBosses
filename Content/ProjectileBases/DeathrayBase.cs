using System;
using System.IO;
using AcidicBosses.Helpers;
using AcidicBosses.Helpers.ProjectileHelpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.ProjectileBases;

public abstract class DeathrayBase : ModProjectile, IAnchoredProjectile
{
    public float Offset => Projectile.ai[0];
    public int AnchorTo => (int) Projectile.ai[1] - 1;
    public virtual bool AnchorPosition => true;
    public virtual bool AnchorRotation => true;
    public virtual bool RotateAroundCenter => false;
    public Vector2? StartOffset { get; set; }
    
    public abstract float Distance { get; }
    
    protected abstract int CollisionWidth { get; }
    
    protected abstract Color Color { get; }
    
    protected abstract Asset<Texture2D> DrTexture { get; }
    public virtual int Frames => 1;
    
    protected virtual bool StartAtEnd => false;

    private bool doneFirstFrame = false;

    private bool readyToDraw = false;
    
    protected int maxTimeLeft = 0;
    protected float widthScale = 1f;

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

    public static Projectile Create<T>(IEntitySource spawnSource, Vector2 position, int damage, float knockback, float rotation, int lifetime, int anchorTo = 0) where T : DeathrayBase
    {
        return ProjHelper.NewUnscaledProjectile(spawnSource, position, Vector2.Zero,
            ModContent.ProjectileType<T>(), damage, knockback, ai0: rotation, ai1: anchorTo + 1, ai2: lifetime);
    }

    public virtual void FirstFrame()
    {
        SoundEngine.PlaySound(SoundID.Zombie104, Projectile.Center);
        Projectile.timeLeft = (int) Projectile.ai[2];
        maxTimeLeft = (int) Projectile.ai[2];
    }
    
    public override void AI()
    {
        // Avoid flickering when spawning
        if (doneFirstFrame && !readyToDraw) readyToDraw = true;
        
        if (!doneFirstFrame)
        {
            FirstFrame();
            Projectile.netUpdate = true;
            doneFirstFrame = true;
        }

        this.Anchor(Projectile);
        
        AiEffects();
        
        var rotation = Projectile.rotation.ToRotationVector2();
        for (var i = 0; i < Distance; i += CollisionWidth)
        {
            var position = Projectile.position + rotation * i;
            
            // Don't draw dust offscreen
            var screenPos = position - Main.screenPosition;
            if (screenPos.X > Main.screenWidth + CollisionWidth || screenPos.Y > Main.screenHeight + CollisionWidth) continue;
            if (screenPos.X < -CollisionWidth * 2 || screenPos.Y < -CollisionWidth * 2) continue;
            
            var rect = new Rectangle();
            rect.X = (int) position.X;
            rect.Y = (int) position.Y;
            rect.Inflate(CollisionWidth / 2, CollisionWidth / 2);

            var randPos = Main.rand.NextVector2FromRectangle(rect);
            SpawnDust(randPos);
        }
        
        DelegateMethods.v3_1 = Color.ToVector3();
        Utils.PlotTileLine(Projectile.Center, Projectile.Center + (Projectile.rotation).ToRotationVector2() * Distance, CollisionWidth, DelegateMethods.CastLight);
    }
    
    protected virtual void AiEffects() {}
    
    protected virtual void SpawnDust(Vector2 position) {}

    public override bool PreDraw(ref Color lightColor)
    {
        // Have to rotate the visual for vertically aligned stuff
        var rotation = Projectile.rotation.ToRotationVector2();
        
        DrawDr(Projectile.position, Projectile.rotation, Color.White);
        
        return false;
    }

    protected void DrawDr(Vector2 pos, float rotation, Color col)
    {
        if (!readyToDraw) return;
        var r = rotation - MathHelper.PiOver2;
        var scale = new Vector2(widthScale, 1f);

        // Game sometimes crashes trying to read the texture, so here's a hacky fix
        try
        {
            var test = DrTexture.Value;
        }
        catch (Exception e)
        {
            return;
        }

        var frames = Main.projFrames[Type];
        var headRect = DrTexture.Frame(frames, 3, Projectile.frame, 0);
        var bodyRect = DrTexture.Frame(frames, 3, Projectile.frame, 1);
        var tailRect = DrTexture.Frame(frames, 3, Projectile.frame, 2);

        var step = bodyRect.Height;
        var origin = headRect.Center();
        if (StartAtEnd) origin = new Vector2(headRect.Width / 2f, 0f);

        // Draw the head
        Main.spriteBatch.Draw(DrTexture.Value, pos - Main.screenPosition, headRect, Color.MultiplyRGBA(col), r, origin, scale, SpriteEffects.None, 0f);
        
        // Body
        for (var i = 1; i <= Distance / step - 1; i++)
        {
            var position = pos + rotation.ToRotationVector2() * i * step;
            Main.spriteBatch.Draw(DrTexture.Value, position - Main.screenPosition, bodyRect, Color.MultiplyRGBA(col), r, origin, scale, SpriteEffects.None, 0f);
        }
        
        // Tail
        Main.spriteBatch.Draw(DrTexture.Value, (pos + rotation.ToRotationVector2() * Distance) - Main.screenPosition, tailRect, Color.MultiplyRGBA(col), r, origin, scale, SpriteEffects.None, 0f);

    }

    // Change the way of collision check of the projectile
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
        var point = 0f;
        
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
            Projectile.Center + Projectile.rotation.ToRotationVector2() * Distance, CollisionWidth, ref point);
    }
    
    public virtual void SendAI(BinaryWriter writer)
    {
    }

    public sealed override void SendExtraAI(BinaryWriter writer)
    {
        SendAI(writer);
    }
    
    public virtual void RecieveAI(BinaryReader reader)
    {
    }

    public sealed override void ReceiveExtraAI(BinaryReader reader)
    {
        RecieveAI(reader);
    }
}