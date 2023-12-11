using System.Linq;
using AcidicBosses.Common.Primitive;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace AcidicBosses.Helpers;

public static class ProjHelper
{
    public static void DrawPrimRay(Projectile projectile, LinePrimDrawer lineDrawer, float direction, float length)
    {
        var points = new Vector2[3];
        
        // 3 points because 2 isn't enough geometry
        for (var i = 0; i < 3; i++)
        {
            points[i] = projectile.Center + direction.ToRotationVector2() * i * length / 3f;
        }
        
        lineDrawer.Draw(points, -Main.screenPosition, 3);
    }

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