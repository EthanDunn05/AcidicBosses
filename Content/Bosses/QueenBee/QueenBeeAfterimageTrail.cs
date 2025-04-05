using AcidicBosses.Content.Projectiles;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.QueenBee;

public class QueenBeeAfterimageTrail : NpcAfterimageTrail
{
    public new static Projectile Create(IEntitySource spawnSource, Vector2 startPos, Vector2 endPos, int creatorId)
    {
        return ProjHelper.NewUnscaledProjectile(spawnSource, startPos, endPos,
            ModContent.ProjectileType<QueenBeeAfterimageTrail>(), 0, 0, ai0: creatorId);
    }
    
    public override bool PreDraw(ref Color lightColor)
    {
        var creator = Main.npc[(int) Projectile.ai[0]];

        // Draw it :)
        var progress = 1f - Utils.GetLerpValue(maxTimeLeft, 0, Projectile.timeLeft, true);
        
        var startPos = Projectile.Center - Main.screenPosition;
        var texture = TextureAssets.Npc[creator.type].Value;
        var origin = frame.Size() / 2f;
        
        var effects = SpriteEffects.FlipHorizontally;
        if (creator.rotation.ToRotationVector2().X < 0) effects |= SpriteEffects.FlipVertically;
        
        for (var i = 0; i <= Images; i++)
        {
            var fade = EasingHelper.QuadIn(progress) * 0.5f * (i) / Images;
            var pos = Vector2.Lerp(startPos, endPos - Main.screenPosition, EasingHelper.QuadIn((float) i / Images));

            var light = Lighting.GetColor((pos + Main.screenPosition).ToTileCoordinates());
            var afterImageColor = Color.Multiply(light, fade);
            
            Main.spriteBatch.Draw(
                texture, pos,
                frame, afterImageColor,
                rotation, origin, Projectile.scale,
                effects, 0f);
        }
        
        return false;
    }
}