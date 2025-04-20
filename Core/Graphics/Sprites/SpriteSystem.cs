using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Threading;
using Terraria;
using Terraria.ModLoader;

namespace AcidicBosses.Core.Graphics.Sprites;

/// <summary>
/// This system manages client side sprites like Luminance particles
/// </summary>
public class SpriteSystem : ModSystem
{
    public static readonly List<EffectLine> EffectLines = [];
    public static readonly List<FakeAfterimage> Afterimages = [];
    
    public override void Load()
    {
        On_Main.DoDraw_DrawNPCsOverTiles += BehindNpcOverTileLayer;
    }

    public override void Unload()
    {
        On_Main.DoDraw_DrawNPCsOverTiles -= BehindNpcOverTileLayer;
    }

    public override void OnWorldUnload()
    {
        EffectLines.Clear();
        Afterimages.Clear();
    }

    public override void PostUpdateProjectiles()
    {
        FastParallel.For(0, EffectLines.Count, (x, y, context) =>
        {
            for (var i = x; i < y; i++) EffectLines[i].Update();
        });

        EffectLines.RemoveAll(p => p.Time > p.Lifetime);
       
        FastParallel.For(0, Afterimages.Count, (x, y, context) =>
        {
            for (var i = x; i < y; i++) Afterimages[i].Update();
        });
        Afterimages.RemoveAll(p => p.Time > p.MaxTime);
    }
    
    private void BehindNpcOverTileLayer(On_Main.orig_DoDraw_DrawNPCsOverTiles orig, Main self)
    {
        // Sb state: Ended
        
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        foreach (var effectLine in EffectLines) effectLine.Draw(Main.spriteBatch);
        foreach (var afterimage in Afterimages) afterimage.Draw(Main.spriteBatch);
        Main.spriteBatch.End();
        
        orig(self);
    }
}