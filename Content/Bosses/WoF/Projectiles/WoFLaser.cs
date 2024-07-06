using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.WoF.Projectiles;

public class WoFLaser : BaseLineProjectile
{
    protected override float Length { get; set; } = 25000;
    protected override float Width { get; set; } = 10f;
    protected override Color Color => GetColor();
    protected override Asset<Texture2D> LineTexture => TextureRegistry.GlowLine;

    protected override bool AnchorRotation => false;

    private int laserDamage = 0;
    private Vector2 laserVel;
    
    private Color GetColor()
    {
        var fadeT = (float) Projectile.timeLeft / maxTimeLeft;
        var color = Color.Purple;
        color *= EasingHelper.CubicOut(fadeT);
        return color;
    }
    
    public override void AI()
    {
        base.AI();

        if (Projectile.damage > 0)
        {
            laserDamage = Projectile.damage;
            Projectile.damage = 0;
        }
        
        if (Projectile.velocity != Vector2.Zero)
        {
            laserVel = Projectile.velocity;
            Projectile.velocity = Vector2.Zero;
        }
    }

    public override void OnKill(int timeLeft)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        var proj = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.position, laserVel, ProjectileID.EyeLaser,
            laserDamage, 3);
        proj.tileCollide = false;
    }
}