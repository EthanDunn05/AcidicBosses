using AcidicBosses.Common.Textures;
using Terraria.ModLoader;

namespace AcidicBosses.Content.ProjectileBases;

public abstract class BaseEffectProjectile : ModProjectile
{
    public override string Texture => TextureRegistry.InvisPath;
    
    public override void SetDefaults()
    {
        Projectile.netImportant = true;
        Projectile.width = 0;
        Projectile.height = 0;
        Projectile.tileCollide = false;
    }
}