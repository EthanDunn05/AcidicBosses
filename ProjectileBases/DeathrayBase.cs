using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AcidicBosses.ProjectileBases;

public abstract class DeathrayBase : ModProjectile
{
    public abstract float Distance { get; }
    protected abstract int CollisionWidth { get; }
    protected  abstract float LaserRotation { get; set; }
    
    public virtual void DrawLaser(Texture2D texture, Vector2 start)
    {
        // Have to rotate the visual for vertically aligned stuff
        var rotation = LaserRotation.ToRotationVector2();
        var r = LaserRotation - MathHelper.PiOver2;

        var headRect = texture.Frame(verticalFrames: 3, frameY: 0);
        var bodyRect = texture.Frame(verticalFrames: 3, frameY: 1);
        var tailRect = texture.Frame(verticalFrames: 3, frameY: 2);

        var step = bodyRect.Height;

        // Draw the head
        Main.EntitySpriteDraw(texture, start - Main.screenPosition, headRect, Color.White, r, headRect.Center(), 1f, SpriteEffects.None);
        
        // Body
        for (var i = 1; i <= Distance / step - 1; i++)
        {
            var position = start + rotation * i * step;
            Main.EntitySpriteDraw(texture, position - Main.screenPosition, bodyRect, Color.White, r, bodyRect.Center(), 1f, SpriteEffects.None);
        }
        
        // Tail
        Main.EntitySpriteDraw(texture, (start + rotation * Distance) - Main.screenPosition, tailRect, Color.White, r, tailRect.Center(), 1f, SpriteEffects.None);
    }
    
    // Change the way of collision check of the projectile
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
        var point = 0f;
        
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
            Projectile.Center + LaserRotation.ToRotationVector2() * Distance, CollisionWidth, ref point);
    }
}