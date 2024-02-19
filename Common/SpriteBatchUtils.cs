using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AcidicBosses.Common;

public static class SpriteBatchUtils
{
    public static void StartShader(this SpriteBatch spriteBatch, BlendState blendState = null, Effect effect = null)
    {
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, blendState ?? BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);
    }

    public static void EndShader(this SpriteBatch spriteBatch)
    {
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
    }
}