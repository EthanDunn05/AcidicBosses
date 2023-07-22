using System.Collections.Generic;
using AcidicBosses.Common.Effects;
using AcidicBosses.Common.Primitive;
using AcidicBosses.Content.ProjectileBases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.KingSlime;

// I'll clean up this mess later
public class KingSlimeCrownLaser : ModProjectile, IPrimDrawer
{
    public bool drawBehindNpcs { get; } = false;

    private LinePrimDrawer laserDrawer;
    
    private float Distance => 1200;
    private int CollisionWidth => 5;

    private float LaserRotation
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
        if (!drawLaser)
        {
            var tellColor = Color.Lerp(Color.Red, Color.Transparent, Timer / 300f);

            var value2 = TextureAssets.Extra[ExtrasID.FairyQueenLance].Value;
            var origin = value2.Frame().Size() * new Vector2(0f, 0.5f);
            Main.EntitySpriteDraw(value2, Projectile.Center - Main.screenPosition, null, tellColor, LaserRotation,
                origin, 1f, SpriteEffects.None, 0f);
        }

        if (drawLaser)
        {
            var texture = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;
            var step = texture.Width / 3;
            for (var i = 0; i <= Distance / step; i++)
            {
                var position = Projectile.Center + LaserRotation.ToRotationVector2() * i * step;
                Lighting.AddLight(position, 1f, 0.1f, 0.1f);
            }
        }
        
        return false;
    }

    private static float GetWidth(float x) => 10f;

    private static Color GetColor(float x) => Color.White;

    public void DrawPrims(SpriteBatch spriteBatch)
    {
        laserDrawer ??= new LinePrimDrawer(GetWidth, GetColor, specialShader: EffectsRegistry.KsCrownLaser);

        if (!drawLaser) return;

        var texAsset = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad);

        laserDrawer.Shader.UseImage1(texAsset);
        laserDrawer.Shader.Shader.Parameters["uDistance"].SetValue(Distance);
        
        var texture = texAsset.Value;
        var step = texture.Width / 3;

        var points = new List<Vector2>();
        for (var i = 0; i <= Distance / step; i++)
        {
            var position = Projectile.Center + LaserRotation.ToRotationVector2() * i * Distance / step;
            points.Add(position);
        }
        
        laserDrawer.Draw(points, -Main.screenPosition, (int) (Distance / step));
    }
    
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if(drawLaser) return base.Colliding(projHitbox, targetHitbox);
        return false;
    }
}