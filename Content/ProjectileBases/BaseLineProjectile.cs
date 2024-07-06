using System;
using System.Collections.Generic;
using System.IO;
using AcidicBosses.Common;
using AcidicBosses.Common.Textures;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.ProjectileBases;

public abstract class BaseLineProjectile : ModProjectile
{
    public virtual bool DrawBehindNpcs => true;

    public override string Texture => TextureRegistry.InvisPath;

    public float Offset => Projectile.ai[0];

    private Vector2? startoffset;

    public int AnchorTo => (int) Projectile.ai[1] - 1;

    protected virtual MiscShaderData? Shader { get; }

    protected abstract float Length { get; set; }

    protected abstract float Width { get; set; }

    protected abstract Color Color { get; }

    protected abstract Asset<Texture2D> LineTexture { get; }
    
    public virtual int Frames => 1;

    protected virtual bool AnchorPosition => true;
    protected virtual bool AnchorRotation => true;

    private bool doneFirstFrame = false;
    private bool readyToDraw = false;

    protected int maxTimeLeft = 0;
    
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

    public static Projectile Create<T>(IEntitySource spawnSource, Vector2 position, float rotation, int lifetime, int anchorTo = -1) where T : BaseLineProjectile
    {
        return ProjHelper.NewProjectile(spawnSource, position, Vector2.Zero,
            ModContent.ProjectileType<T>(), 0, 0, ai0: rotation, ai1: anchorTo + 1, ai2: lifetime);
    }
    
    public static Projectile Create<T>(IEntitySource spawnSource, Vector2 position, Vector2 velocity, float rotation, int lifetime, int anchorTo = -1) where T : BaseLineProjectile
    {
        return ProjHelper.NewProjectile(spawnSource, position, velocity,
            ModContent.ProjectileType<T>(), 0, 0, ai0: rotation, ai1: anchorTo + 1, ai2: lifetime);
    }
    
    public static Projectile Create<T>(IEntitySource spawnSource, Vector2 position, Vector2 velocity, int damage, float knockback, float rotation, int lifetime, int anchorTo = -1) where T : BaseLineProjectile
    {
        return ProjHelper.NewProjectile(spawnSource, position, velocity,
            ModContent.ProjectileType<T>(), damage, knockback, ai0: rotation, ai1: anchorTo + 1, ai2: lifetime);
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
        
        if (AnchorTo >= 0)
        {
            var owner = Main.npc[AnchorTo];
            if (owner != null)
            {
                startoffset ??= owner.Center - Projectile.position;

                if (AnchorRotation) Projectile.rotation = owner.rotation + Offset;
                else Projectile.rotation = Offset;

                if (AnchorPosition && AnchorRotation)
                {
                    var rotatedOffset = startoffset * Offset.ToRotationVector2();
                    Projectile.position = (Vector2) (owner.Center + rotatedOffset)!;
                }
                else if (AnchorPosition)
                {
                    Projectile.position = (Vector2) (owner.Center + startoffset)!;
                }

            }
        }
        else
        {
            Projectile.rotation = Offset;
        }
    }

    public virtual void FirstFrame()
    {
        Projectile.timeLeft = (int) Projectile.ai[2];
        maxTimeLeft = (int) Projectile.ai[2];
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
        
        var pos = Projectile.position - Main.screenPosition;
        var rect = LineTexture.Frame(horizontalFrames: Frames,  frameX: Projectile.frame);
        var origin = new Vector2(rect.Width / 2f, 0);
        var scale = new Vector2(Width * 2, Length) / rect.Size();
        
        if (Shader != null) Main.spriteBatch.EnterShader(effect: Shader.Shader);

        Main.EntitySpriteDraw(
            LineTexture.Value, pos, rect, Color, 
            Projectile.rotation - MathHelper.PiOver2, origin, scale,
            SpriteEffects.None
        );
        
        if (Shader != null) Main.spriteBatch.ExitShader();

        return false;
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