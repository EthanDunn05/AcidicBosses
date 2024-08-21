using AcidicBosses.Common.Textures;
using AcidicBosses.Content.Bosses.KingSlime;
using AcidicBosses.Content.ProjectileBases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.Twins.Projectiles;

public class RetDeathrayIndicator : BaseLineProjectile
{
    protected override float Length { get; set; } = 12000;
    protected override float Width { get; set; } = 5;
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

        var proj = DeathrayBase.Create<RetDeathray>(Projectile.GetSource_FromAI(), Projectile.position, laserDamage, 4,
            Offset, 120, AnchorTo);
    }
}