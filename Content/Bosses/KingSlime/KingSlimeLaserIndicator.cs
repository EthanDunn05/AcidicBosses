using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.KingSlime;

public class KingSlimeLaserIndicator : BaseLineProjectile
{
    protected override float Length => 12000;
    protected override float Width => 5;
    protected override Color Color => Color.Red;
    protected override Asset<Texture2D> LineTexture => TextureRegistry.GlowLine;
    
    private int laserDamage = 0;

    public override void AI()
    {
        base.AI();

        if (Projectile.damage > 0)
        {
            laserDamage = Projectile.damage;
            Projectile.damage = 0;
        }
    }

    public override void OnKill(int timeLeft)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        var proj = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.position, Vector2.Zero, 
            ModContent.ProjectileType<KingSlimeCrownLaser>(), laserDamage, 3, ai0: Offset);
        proj.timeLeft = 120;
    }
}