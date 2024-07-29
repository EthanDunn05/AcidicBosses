using AcidicBosses.Common.Textures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.Skeletron;

public class EvilWaterbolt : ModProjectile
{
    public override string Texture => TextureRegistry.TerrariaProjectile(ProjectileID.WaterBolt);
    
    public override void SetDefaults()
    {
        Projectile.CloneDefaults(ProjectileID.WaterBolt);
        AIType = ProjectileID.WaterBolt;
        Projectile.hostile = true;
        Projectile.friendly = false;
    }
}