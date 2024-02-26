using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AcidicBosses.Common;

public static class SpriteBatchUtils
{
    /**
     * Restarts the sprite batch so shader effects can be applied.
     * Make sure to call EndShader when done.
     * <seealso cref="EndShader"/>
     */
    public static void StartShader(this SpriteBatch spriteBatch, BlendState blendState = null, Effect effect = null)
    {
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, blendState ?? BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);
    }

    /**
     * Restarts the sprite batch to the default state. Must be called after StartShader
     * or else things will break.
     * <seealso cref="StartShader"/>
     */
    public static void EndShader(this SpriteBatch spriteBatch)
    {
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
    }
}