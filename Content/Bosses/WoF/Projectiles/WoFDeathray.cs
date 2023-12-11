using AcidicBosses.Content.ProjectileBases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.WoF.Projectiles;

public class WoFDeathray : DeathrayBase
{
    public override float Distance => 25000;
    protected override int CollisionWidth => 15;
    protected override Color Color => Color.White;
    protected override Asset<Texture2D> DrTexture => ModContent.Request<Texture2D>(Texture);

    protected override bool AnchorRotation => false;
}