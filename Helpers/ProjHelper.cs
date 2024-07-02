using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AcidicBosses.Helpers;

public static class ProjHelper
{
    public static void DrawAfterimages(Projectile projectile, Texture2D texture, ref Color lightColor, int afterimageInterval = 1)
    {
        var rect = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
        var origin = texture.Size() / new Vector2(1f, Main.projFrames[projectile.type]) * 0.5f;

        var effects = SpriteEffects.None;
        if (projectile.spriteDirection == -1) effects = SpriteEffects.FlipHorizontally;
        
        // More Spaced out trail
        for (var i = 1; i < projectile.oldPos.Length; i += afterimageInterval)
        {
            // All of this is heavily simplified from decompiled vanilla  
            var fade = 0.5f * (projectile.oldPos.Length - i) / 20f;
            var afterImageColor = Color.Multiply(lightColor, fade);
            
            var pos = projectile.oldPos[i] + new Vector2(projectile.width, projectile.height) / 2f - Main.screenPosition;
            Main.spriteBatch.Draw(texture, pos, rect, afterImageColor,
                projectile.oldRot[i], origin, projectile.scale, effects, 0f);
        }
    }
    
    public static void Draw(Projectile projectile, Texture2D texture, ref Color lightColor)
    {
        var rect = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
        var origin = texture.Size() / new Vector2(1f, Main.projFrames[projectile.type]) * 0.5f;

        var effects = SpriteEffects.None;
        if (projectile.spriteDirection == -1) effects = SpriteEffects.FlipHorizontally;
        
        // All of this is heavily simplified from decompiled vanilla
        
        var pos = projectile.position + new Vector2(projectile.width, projectile.height) / 2f - Main.screenPosition;
        Main.spriteBatch.Draw(texture, pos, rect, lightColor,
            projectile.rotation, origin, projectile.scale, effects, 0f);
    }
    
    public static Projectile FindProjectile(int identity) => Main.projectile.FirstOrDefault(p => p.identity == identity);
}