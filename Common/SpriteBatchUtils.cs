using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AcidicBosses.Common;

public static class SpriteBatchUtils
{
    /// <summary>
    /// Restarts the sprite batch so shader effects can be applied.
    /// Make sure to call <see cref="ExitShader"/> when done.
    /// </summary>
    /// <seealso cref="ExitShader"/>
    public static void EnterShader(this SpriteBatch spriteBatch, BlendState blendState = null, Effect effect = null)
    {
        try
        {
            spriteBatch.End();
        }
        catch (InvalidOperationException e)
        {
            // Don't end then
        }
        
        spriteBatch.Begin(SpriteSortMode.Immediate, blendState ?? BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);
    }

    /// <summary>
    /// Restarts the sprite batch to the default state. Must be called after <see cref="EnterShader"/>
    /// or else things will break.
    /// </summary>
    /// <seealso cref="EnterShader"/>
    public static void ExitShader(this SpriteBatch spriteBatch)
    {
        try
        {
            spriteBatch.End();
        }
        catch (InvalidOperationException e)
        {
            // Don't end then
        }
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
    }
}