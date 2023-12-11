using System.Collections.Generic;
using AcidicBosses.Common.Effects;
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
public class KingSlimeCrownLaser : DeathrayBase
{
    public override float Distance => 12000;
    protected override int CollisionWidth => 5;
    protected override Color Color => Color.White;
    protected override Asset<Texture2D> DrTexture => ModContent.Request<Texture2D>(Texture);

    protected override void SpawnDust(Vector2 position)
    {
        if (Main.rand.NextBool(4, 5)) return;
        Dust.NewDust(position, 0, 0, DustID.GemRuby, Scale: 0.5f);
    }
}