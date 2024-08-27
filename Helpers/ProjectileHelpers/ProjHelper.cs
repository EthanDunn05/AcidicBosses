using System;
using System.Linq;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;

namespace AcidicBosses.Helpers;

public static class ProjHelper
{
    public static Projectile NewUnscaledProjectile(IEntitySource source, float spawnX, float spawnY, float velocityX, float velocityY, int type, int damage, float knockback, int owner = -1, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f)
    {
        // Blatantly stolen from luminance
        // The only difference between this and Luminance's implementation is that this doesn't default to the player
        // as the owner and defaults to the server like normal
        float damageJankCorrectionFactor = 0.5f;
        if (Main.expertMode)
            damageJankCorrectionFactor = 0.25f;
        if (Main.masterMode)
            damageJankCorrectionFactor = 0.1667f;
        damage = (int)(damage * damageJankCorrectionFactor);

        int index = Projectile.NewProjectile(source, spawnX, spawnY, velocityX, velocityY, type, damage, knockback, owner, ai0, ai1, ai2);
        if (index >= 0 && index < Main.maxProjectiles)
            Main.projectile[index].netUpdate = true;

        return Main.projectile[index];
    }
    
    public static Projectile NewUnscaledProjectile(IEntitySource source, Vector2 center, Vector2 velocity, int type, int damage, float knockback, int owner = -1, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f)
    {
        return NewUnscaledProjectile(source, center.X, center.Y, velocity.X, velocity.Y, type, damage, knockback, owner, ai0, ai1, ai2);
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

    public delegate void AfterimageDraw(Vector2 oldPos, float oldRot, float fade);
    
    public static void DrawAfterimages(Projectile projectile, AfterimageDraw drawCall, int afterimageInterval = 1)
    {
        // More Spaced out trail
        for (var i = 1; i < projectile.oldPos.Length; i += afterimageInterval)
        {
            // All of this is heavily simplified from decompiled vanilla  
            var fade = 0.5f * (projectile.oldPos.Length - i) / 20f;
            drawCall(projectile.oldPos[i], projectile.oldRot[i], fade);
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
    
    public static void Draw(Projectile projectile, AtlasTexture texture, ref Color lightColor)
    {
        var rect = texture.Frame;
        var origin = texture.Frame.Size() / 2f;

        var effects = SpriteEffects.None;
        if (projectile.spriteDirection == -1) effects = SpriteEffects.FlipHorizontally;
        
        // All of this is heavily simplified from decompiled vanilla
        
        var pos = projectile.position + new Vector2(projectile.width, projectile.height) / 2f - Main.screenPosition;
        Main.spriteBatch.Draw(texture, pos, rect, lightColor * projectile.Opacity, projectile.rotation, origin, Vector2.One * projectile.scale, effects);
    }
    
    public static Projectile FindProjectile(int identity) => Main.projectile.FirstOrDefault(p => p.identity == identity);
}