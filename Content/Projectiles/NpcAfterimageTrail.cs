using System.Collections.Generic;
using System.IO;
using AcidicBosses.Common;
using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Projectiles;

public class NpcAfterimageTrail : ModProjectile
{
    public override string Texture => TextureRegistry.InvisPath;

    private Vector2? startoffset;

    private bool doneFirstFrame = false;

    protected int maxTimeLeft = 0;

    private Rectangle frame;
    private Vector2 endPos;
    private float rotation;
    
    private const int Images = 20;

    public override void SetDefaults()
    {
        Projectile.width = 0;
        Projectile.height = 0;
        Projectile.tileCollide = false;
        Projectile.hide = true;
    }

    // The position is the start and the npc's position is the end
    public static Projectile Create(IEntitySource spawnSource, Vector2 startPos, Vector2 endPos, int creatorId)
    {
        return ProjHelper.NewUnscaledProjectile(spawnSource, startPos, endPos,
            ModContent.ProjectileType<NpcAfterimageTrail>(), 0, 0, ai0: creatorId);
    }


    public override void AI()
    {
        if (!doneFirstFrame)
        {
            FirstFrame();
            Projectile.netUpdate = true;
            doneFirstFrame = true;
        }
    }

    public virtual void FirstFrame()
    {
        Projectile.timeLeft = (int) Images;
        maxTimeLeft = (int) Images;
        endPos = Projectile.velocity;
        Projectile.velocity = Vector2.Zero;
        
        var creator = Main.npc[(int) Projectile.ai[0]];
        frame = creator.frame;
        rotation = creator.rotation;

    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        behindNPCs.Add(index);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        var creator = Main.npc[(int) Projectile.ai[0]];

        // Draw it :)
        var progress = 1f - Utils.GetLerpValue(maxTimeLeft, 0, Projectile.timeLeft, true);
        
        var startPos = Projectile.Center - Main.screenPosition;
        var texture = TextureAssets.Npc[creator.type].Value;
        var origin = frame.Size() / 2f;
        
        var effects = SpriteEffects.None;
        if (creator.spriteDirection < 1) effects = SpriteEffects.FlipHorizontally;
        
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