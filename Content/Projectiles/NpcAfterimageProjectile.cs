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

public class NpcAfterimageProjectile : ModProjectile
{
    public override string Texture => TextureRegistry.InvisPath;

    

    private Vector2? startoffset;

    private bool doneFirstFrame = false;

    protected int maxTimeLeft = 0;

    public override void SetDefaults()
    {
        Projectile.width = 0;
        Projectile.height = 0;
        Projectile.tileCollide = false;
        Projectile.hide = true;
    }

    public static Projectile Create(IEntitySource spawnSource, Vector2 position, float rotation, int npcMimic, SpriteEffects effects, int lifetime)
    {
        return ProjHelper.NewUnscaledProjectile(spawnSource, position, rotation.ToRotationVector2(),
            ModContent.ProjectileType<NpcAfterimageProjectile>(), 0, 0, ai0: npcMimic, ai1: (float) effects, ai2: lifetime);
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
        Projectile.timeLeft = (int) Projectile.ai[2];
        maxTimeLeft = (int) Projectile.ai[2];
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.velocity = Vector2.Zero;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        behindNPCs.Add(index);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        var effects = (SpriteEffects) Projectile.ai[1];
        var npcMimic = (int)Projectile.ai[0];

        // Draw it :)
        var progress = 1f - Utils.GetLerpValue(maxTimeLeft, 0, Projectile.timeLeft, true);
        var alpha = Color.Multiply(lightColor, EasingHelper.QuadIn(progress) * 0.5f);
        
        var drawPos = Projectile.Center - Main.screenPosition;
        var texture = TextureAssets.Npc[npcMimic].Value;
        var frame = texture.Frame(1, Main.npcFrameCount[npcMimic]);
        var origin = frame.Center.ToVector2();
        
        Main.spriteBatch.Draw(
            texture, drawPos,
            frame, alpha,
            Projectile.rotation, origin, Projectile.scale,
            effects, 0f);
        
        return false;
    }
}