using System.Collections.Generic;
using AcidicBosses.Common.Textures;
using AcidicBosses.Helpers;
using AcidicBosses.Helpers.ProjectileHelpers;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.ProjectileBases;

public abstract class BaseSweep : ModProjectile, IAnchoredProjectile
{
    public virtual bool DrawBehindNpcs => true;

    public override string Texture => TextureRegistry.InvisPath;

    public float Offset => Projectile.ai[0];
    public int AnchorTo => (int) Projectile.ai[1] - 1;
    public virtual bool AnchorPosition => true;
    public virtual bool AnchorRotation => true;
    public virtual bool RotateAroundCenter => false;
    public Vector2? StartOffset { get; set; }

    protected abstract float Length { get; set; }
    protected abstract float Width { get; set; }
    protected abstract Color Color { get; set; }
    protected abstract float Radius { get; set; }
    
    private bool doneFirstFrame = false;
    private bool readyToDraw = false;
    protected int MaxTimeLeft = 0;
    
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = (int) Length;
    }

    public override void SetDefaults()
    {
        Projectile.netImportant = true;
        Projectile.width = 0;
        Projectile.height = 0;
        Projectile.tileCollide = false;
        Projectile.hide = DrawBehindNpcs;
    }

    public static Projectile Create<T>(IEntitySource spawnSource, Vector2 position, float rotation, int lifetime, int anchorTo = -1) where T : BaseSweep
    {
        return ProjHelper.NewUnscaledProjectile(spawnSource, position, Vector2.Zero,
            ModContent.ProjectileType<T>(), 0, 0, ai0: rotation, ai1: anchorTo + 1, ai2: lifetime);
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
    }

    public virtual void FirstFrame()
    {
        Projectile.timeLeft = (int) Projectile.ai[2];
        MaxTimeLeft = (int) Projectile.ai[2];
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        if (DrawBehindNpcs) behindNPCs.Add(index);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (!readyToDraw) return false;

        var tex = TextureRegistry.SideGlowLine;
        var pos = Projectile.position - Main.screenPosition;
        var rect = tex.Frame();
        var scale = new Vector2(Width * 2, Length) / rect.Size();
        var leftOrigin = new Vector2(0, 0);
        var rightOrigin = new Vector2(rect.Width, 0);
        
        Main.EntitySpriteDraw(
            tex.Value, pos, rect, Color, 
            Projectile.rotation - MathHelper.PiOver2 + Radius, leftOrigin, scale,
            SpriteEffects.None
        );
        
        Main.EntitySpriteDraw(
            tex.Value, pos, rect, Color, 
            Projectile.rotation - MathHelper.PiOver2 - Radius, rightOrigin, scale,
            SpriteEffects.FlipHorizontally
        );

        return false;
    }
}