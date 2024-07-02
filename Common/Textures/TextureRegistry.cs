using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace AcidicBosses.Common.Textures;

/// <summary>
/// Simple class that holds all the general use textures for the mod.
/// Mainly used to avoid hard-coded strings when accessing textures.
/// </summary>
public static class TextureRegistry
{
    public static string InvisPath = "AcidicBosses/Assets/Textures/Invisible";
    
    // Lines
    public static Asset<Texture2D> GlowLine => Tex("Lines/GlowLine");
    public static Asset<Texture2D> InvertedGlowLine => Tex("Lines/InvertedGlowLine");
    public static Asset<Texture2D> SideGlowLine => Tex("Lines/SideGlowLine");
    public static Asset<Texture2D> Line => Tex("Lines/Line");
    
    // Noise
    public static Asset<Texture2D> RgbPerlin => Tex("Noise/rgbPerlin");

    // A simple function that loads a texture
    private static Asset<Texture2D> Tex(string name) => ModContent.Request<Texture2D>($"AcidicBosses/Assets/Textures/{name}", AssetRequestMode.ImmediateLoad);
    
    // Vanilla textures
    public static string TerrariaProjectile(int projID) => $"Terraria/Images/Projectile_{projID}";
    public static string TerrariaItem(int itemID) => $"Terraria/Images/Item_{itemID}";
    public static string TerrariaNPC(int npcID) => $"Terraria/Images/NPC_{npcID}";
}