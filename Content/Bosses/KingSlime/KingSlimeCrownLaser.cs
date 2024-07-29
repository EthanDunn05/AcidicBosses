using System.Collections.Generic;
using AcidicBosses.Common.Effects;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Helpers;
using Luminance.Common.Easings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.KingSlime;

public class KingSlimeCrownLaser : DeathrayBase
{
    public override float Distance => 12000;
    protected override int CollisionWidth => 5;
    protected override Color Color => Color.White;
    protected override Asset<Texture2D> DrTexture => ModContent.Request<Texture2D>(Texture);
    protected override bool StartAtEnd => true;

    public override void AI()
    {
        base.AI();

        const int animLen = 5;
        var timeAlive = maxTimeLeft - Projectile.timeLeft;
        if (timeAlive <= animLen)
        {
            // Bounce in
            var t = EasingHelper.BackOut((float) timeAlive / animLen);
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
}