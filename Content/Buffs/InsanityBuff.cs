using System;
using AcidicBosses.Common.Textures;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Buffs;

public class InsanityBuff : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.debuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.dontStarveShader = true;
        player.GetModPlayer<InsanityModPlayer>().IsInsane = true;
    }
}

public class InsanityModPlayer : ModPlayer
{
    public bool IsInsane = false;

    public override void ResetEffects()
    {
        IsInsane = false;
    }
}

public class InsanityOverlaySystem : ModSystem
{
    private float overlayOpacity = 0f;
    private const int Buffer = 32;

    public override void Load()
    {
        On_ScreenDarkness.DrawFront += DrawOverlay;
    }

    public override void Unload()
    {
        On_ScreenDarkness.DrawFront -= DrawOverlay;
    }

    private void DrawOverlay(On_ScreenDarkness.orig_DrawFront orig, SpriteBatch spritebatch)
    {
        orig.Invoke(spritebatch);

        if (overlayOpacity <= 0f) return;

        // This isn't fully accurate to Don't Starve, but I don't care.
        var tex = TextureRegistry.InsanityOverlay.Value;
        var offset = Main.rand.NextVector2Unit() * 4 * overlayOpacity;
        var scale = (int)MathHelper.Lerp(Buffer + 256, Buffer, EasingHelper.QuadOut(overlayOpacity));
        var dest = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
        dest.Inflate(scale, scale);
        dest.Offset(offset.ToPoint());
        spritebatch.Draw(tex, dest, Color.White * overlayOpacity);
    }

    public override void PostUpdatePlayers()
    {
        if (!AcidUtils.IsClient()) return;

        var insane = Main.LocalPlayer.GetModPlayer<InsanityModPlayer>().IsInsane;
        var elapsedTime = (float)Main.gameTimeCache.ElapsedGameTime.TotalSeconds;

        if (insane)
        {
            overlayOpacity = MathF.Min(1f, overlayOpacity + elapsedTime);
        }
        else
        {
            overlayOpacity = MathF.Max(0f, overlayOpacity - elapsedTime);
        }
    }
}