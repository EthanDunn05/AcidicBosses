using System.Linq;
using AcidicBosses.Common.Effects;
using AcidicBosses.Common.Primitive;
using AcidicBosses.Common.Textures;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.ProjectileBases;

public abstract class BaseLineProjectile : ModProjectile, IPrimDrawer
{
    public bool drawBehindNpcs { get; } = true;
    
    public override string Texture => TextureRegistry.InvisPath;
    
    public float Offset => Projectile.ai[0];

    private Vector2? startoffset;

    public int AnchorTo => (int) Projectile.ai[1];

    private LinePrimDrawer lineDrawer;
    
    private MiscShaderData Shader => EffectsRegistry.BasicTexture;

    protected abstract float Length { get; }

    protected abstract float Width { get; }
    
    protected abstract Color Color { get; }
    
    protected abstract Asset<Texture2D> LineTexture { get; }
    
    protected virtual bool anchorPosition => true;
    protected virtual bool anchorRotation => true;
    
    public override void SetDefaults()
    {
        Projectile.netImportant = true;
        Projectile.width = 0;
        Projectile.height = 0;
        Projectile.tileCollide = false;
    }
    
    public override void AI()
    {
        if(AnchorTo > 0)
        {
            var owner = Main.npc[AnchorTo];
            if (owner != null)
            {
                startoffset ??= owner.Center - Projectile.position;
                
                if (anchorRotation) Projectile.rotation = owner.rotation + Offset;
                else Projectile.rotation = Offset;
                
                if (anchorPosition)
                    Projectile.position = (Vector2) (owner.Center + startoffset);
            }
        }
        else
        {
            Projectile.rotation = Offset;
        }
    }
    
    public void DrawPrims(SpriteBatch spriteBatch)
    {
        lineDrawer ??= new LinePrimDrawer(x => Width, x => Color, specialShader: Shader);
        lineDrawer.Shader.UseImage0(LineTexture);
        
        ProjHelper.DrawPrimRay(Projectile, lineDrawer, Projectile.rotation, Length);
    }
}