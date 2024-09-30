using AcidicBosses.Common.RenderManagers;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Helpers;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.Twins.Projectiles;

public class RetDeathray : DeathrayBase
{
    public override float Distance => 12000;
    protected override int CollisionWidth => 5;
    protected override Color Color => Color.White;
    protected override Asset<Texture2D> DrTexture => ModContent.Request<Texture2D>(Texture);
    public override bool RotateAroundCenter => true;
    
    protected override bool StartAtEnd => true;

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
    }

    public override void AI()
    {
        base.AI();

        const int animLen = 5;
        ref var timeAlive = ref Projectile.localAI[0];
        if (timeAlive <= animLen)
        {
            // Bounce in
            var t = EasingHelper.QuadOut(Utils.GetLerpValue(0, animLen, timeAlive));
            widthScale = MathHelper.Lerp(0f, 1f, t);
        }
        else if (Projectile.timeLeft <= animLen)
        {
            // Bounce in
            var t = EasingHelper.QuadIn((float) Projectile.timeLeft / animLen);
            widthScale = MathHelper.Lerp(0f, 1f, t);
        }
        else
        {
            widthScale = 1f;
        }

        timeAlive++;
    }

    protected override void SpawnDust(Vector2 position)
    {
        if (Projectile.timeLeft == maxTimeLeft)
        {
            Dust.NewDust(position, 0, 0, DustID.GemRuby, Scale: 0.5f);
        }

        if (Main.rand.NextBool(50))
        {
            Dust.NewDust(position, 0, 0, DustID.GemRuby, Scale: 0.5f);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        var color1 = lightColor;
        ProjHelper.DrawAfterimages(Projectile, (oldPos, oldRot, fade) =>
        {
            DrawDr(oldPos, oldRot, Color.Red * fade);
        });
        
        base.PreDraw(ref lightColor);
        var pos = Projectile.position - Main.screenPosition;
        var rot = Projectile.rotation + MathHelper.PiOver4;
        var tex = AtlasManager.GetTexture("AcidicBosses.GlowStar");
        var scaleOffset = Main.rand.NextFloat(-0.2f, 0.2f);
        
        Main.spriteBatch.Draw(tex, pos, tex.Frame, Color.White, rot, tex.Frame.Size() / 2f, scale: Vector2.One * (3f + scaleOffset));
        return false;
    }
}