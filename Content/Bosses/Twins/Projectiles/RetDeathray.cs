using AcidicBosses.Common.RenderManagers;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Helpers;
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

    public override void AI()
    {
        base.AI();

        const int animLen = 5;
        var timeAlive = maxTimeLeft - Projectile.timeLeft;
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
        base.PreDraw(ref lightColor);
        var pos = Projectile.position - Main.screenPosition;
        var rot = Projectile.rotation + MathHelper.PiOver4;
        var tex = TextureAssets.Extra[ExtrasID.SharpTears];
        var frame = tex.Frame();
        var origin = frame.Center();
        var scaleOffset = Main.rand.NextFloat(-0.2f, 0.2f);
        
        Main.spriteBatch.Draw(tex.Value, pos, frame, Color.White, rot, origin, widthScale * 1.5f + scaleOffset, SpriteEffects.None, 0f);
        Main.spriteBatch.Draw(tex.Value, pos, frame, Color.White, rot + MathHelper.PiOver2, origin, widthScale * 1.5f + scaleOffset, SpriteEffects.None, 0f);

        return false;
    }
}