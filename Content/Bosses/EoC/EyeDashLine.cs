using System.Linq;
using AcidicBosses.Common.Effects;
using AcidicBosses.Common.Primitive;
using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.EoC;

public class EyeDashLine : ModProjectile, IPrimDrawer
{
    public bool drawBehindNpcs { get; } = true;
    
    public override string Texture => TextureRegistry.InvisPath;
    
    public float Offset => Projectile.ai[0];

    public bool AnchorToBoss => Projectile.ai[1] != 0;

    private MiscShaderData Shader => EffectsRegistry.BasicTexture;

    private LinePrimDrawer lineDrawer;
    
    private float Length => 12000;
    
    private Asset<Texture2D> LineTexture { get; } = TextureRegistry.InvertedGlowLine;

    public override void SetDefaults()
    {
        Projectile.netImportant = true;
        Projectile.width = 0;
        Projectile.height = 0;
    }

    public override void AI()
    {
        if(AnchorToBoss)
        {
            var owner = Main.npc.FirstOrDefault(npc => npc.type == NPCID.EyeofCthulhu);
            if (owner != null)
            {
                Projectile.rotation = owner.rotation + Offset + MathHelper.PiOver2;
                Projectile.position = owner.Center;
            }
        }
        else
        {
            Projectile.rotation = Offset;
        }
    }

    private float WidthFunction(float t) => 40; // Slightly smaller than EoC

    private Color ColorFunction(float t) => Color.Crimson;

    public void DrawPrims(SpriteBatch spriteBatch)
    {
        lineDrawer ??= new LinePrimDrawer(WidthFunction, ColorFunction, specialShader: Shader);
        lineDrawer.Shader.UseImage0(LineTexture);
        
        ProjHelper.DrawPrimRay(Projectile, lineDrawer, Projectile.rotation, Length);
    }
}