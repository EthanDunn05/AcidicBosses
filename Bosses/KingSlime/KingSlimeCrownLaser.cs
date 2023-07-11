using AcidicBosses.ProjectileBases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Bosses.KingSlime;

// I'll clean up this mess later
public class KingSlimeCrownLaser : DeathrayBase
{
    public override float Distance => 1200;
    protected override int CollisionWidth => 5;

    protected override float LaserRotation
    {
        get => Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    
    private int Timer
    {
        get => (int) Projectile.localAI[0];
        set => Projectile.localAI[0] = value;
    }
    
    private bool drawLaser = false;
    
    public override void SetDefaults()
    {
        Projectile.width = 10;
        Projectile.height = 10;
        Projectile.hostile = true;
        Projectile.hide = false;
    }

    public override void AI()
    {
        Timer++;

        if (Timer == 120)
        {
            SoundEngine.PlaySound(SoundID.Zombie103);
            drawLaser = true;
        }
        
        if(Timer > 240) Projectile.Kill();
    }

    public override bool PreDraw(ref Color lightColor)
    {
        var texture = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;

        var tellColor = Color.Lerp(Color.Red, Color.Transparent, Timer / 300f);
        
        var value2 = TextureAssets.Extra[ExtrasID.FairyQueenLance].Value;
        var origin = value2.Frame().Size() * new Vector2(0f, 0.5f);
        Main.EntitySpriteDraw(value2, Projectile.Center - Main.screenPosition, null, tellColor, LaserRotation, origin, 1f, SpriteEffects.None, 0f);
        
        if (drawLaser) DrawLaser(texture, Projectile.Center);
        return false;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if(drawLaser) return base.Colliding(projHitbox, targetHitbox);
        return false;
    }
}