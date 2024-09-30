using System.IO;
using AcidicBosses.Common.Effects;
using AcidicBosses.Content.ProjectileBases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.WoF.Projectiles;

public class WoFDeathray : DeathrayBase
{
    public override float Distance => 25000;
    protected override int CollisionWidth => 15;
    protected override Color Color => Color.White;
    protected override Asset<Texture2D> DrTexture => ModContent.Request<Texture2D>(Texture);

    public override bool AnchorRotation => false;

    private const float AberrationStrength = 0.003f;

    public override void FirstFrame()
    {
        base.FirstFrame();
    }

    protected override void AiEffects()
    {
        EffectsManager.AberrationActivate(MathHelper.Lerp(0f, AberrationStrength, Projectile.timeLeft / (float) maxTimeLeft));
    }

    protected override void SpawnDust(Vector2 position)
    {
        if (!Main.rand.NextBool(1, 20)) return;
        Dust.NewDust(position, 0, 0, DustID.Shadowflame, Scale: 1.5f);
    }
}