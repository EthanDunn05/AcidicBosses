using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.Twins.Projectiles;

public class RetLaserIndicator : BaseLineProjectile
{
    protected override float Length { get; set; } = 1200;
    protected override float Width { get; set; } = 5;
    protected override Color Color => GetColor();
    protected override Asset<Texture2D> LineTexture => TextureRegistry.GlowLine;
    public override bool RotateAroundCenter => true;

    private Vector2 laserVel;
    
    private Color GetColor()
    {
        var fadeT = (float) Projectile.timeLeft / MaxTimeLeft;
        var color = Color.Red;
        color *= EasingHelper.CubicOut(fadeT);
        return color;
    }
    
    public override void AI()
    {
        base.AI();
        
        if (Projectile.velocity != Vector2.Zero)
        {
            laserVel = Projectile.velocity;
            Projectile.velocity = Vector2.Zero;
        }
    }

    public override void OnKill(int timeLeft)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        // Use vanilla to keep scaling
        var proj = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.position, Projectile.rotation.ToRotationVector2() * laserVel.Length(), ProjectileID.DeathLaser,
            Projectile.damage, 3);
        proj.tileCollide = false;
    }
}