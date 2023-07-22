using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace AcidicBosses.Common.Textures;

public static class TextureRegistry
{
    public static string InvisPath = "AcidicBosses/Assets/Textures/Invisible";
    
    public static Asset<Texture2D> GlowLine => Tex("Lines/GlowLine");
    public static Asset<Texture2D> InvertedGlowLine => Tex("Lines/InvertedGlowLine");
    public static Asset<Texture2D> Line => Tex("Lines/Line");

    private static Asset<Texture2D> Tex(string name) => ModContent.Request<Texture2D>($"AcidicBosses/Assets/Textures/{name}", AssetRequestMode.ImmediateLoad);
}