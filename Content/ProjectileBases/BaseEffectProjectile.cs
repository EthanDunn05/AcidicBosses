using AcidicBosses.Common.Textures;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace AcidicBosses.Content.ProjectileBases;

public abstract class BaseEffectProjectile : ModProjectile
{
    public override string Texture => TextureRegistry.InvisPath;
    
    private bool doneFirstFrame = false;
    protected int MaxTimeLeft = 0;
    
    public override void SetDefaults()
    {
        Projectile.netImportant = true;
        Projectile.width = 0;
        Projectile.height = 0;
        Projectile.tileCollide = false;
    }
    
    public static Projectile Create<T>(IEntitySource spawnSource, Vector2 position, float rotation, int lifetime) where T : BaseEffectProjectile
    {
        return ProjHelper.NewUnscaledProjectile(spawnSource, position, Vector2.Zero,
            ModContent.ProjectileType<T>(), 0, 0, ai0: rotation, ai1: lifetime);
    }
    
    public override void AI()
    {
        // Avoid flickering when spawning
        if (!doneFirstFrame)
        {
            FirstFrame();
            Projectile.netUpdate = true;
            doneFirstFrame = true;
        }

        Projectile.rotation = Projectile.ai[0];
    }

    public virtual void FirstFrame()
    {
        Projectile.timeLeft = (int) Projectile.ai[1];
        MaxTimeLeft = (int) Projectile.ai[1];
    }
}