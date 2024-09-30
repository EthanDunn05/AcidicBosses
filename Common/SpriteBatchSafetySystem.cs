using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;

namespace AcidicBosses.Common;

/// <summary>
/// I've had a lot of issues with starting and restarting the spritebatch,
/// so this is here to add a safety net to make the spritebatch adapt to the state rather than crash
/// </summary>
public class SpriteBatchSafetySystem : ModSystem
{
    private delegate void orig_Begin(
        SpriteBatch self,
        SpriteSortMode sortMode,
        BlendState blendState,
        SamplerState samplerState,
        DepthStencilState depthStencilState,
        RasterizerState rasterizerState,
        Effect effect,
        Matrix transformMatrix
    );

    private delegate void orig_End(SpriteBatch self);

    private Hook beginHook;
    private Hook endHook;
    
    public override void Load()
    {
        var sbType = typeof(SpriteBatch);
        var beginInfo = sbType.GetMethod(nameof(SpriteBatch.Begin),
        [
            typeof(SpriteSortMode),
            typeof(BlendState),
            typeof(SamplerState),
            typeof(DepthStencilState),
            typeof(RasterizerState),
            typeof(Effect),
            typeof(Matrix)
        ]);
        
        beginHook = new Hook(beginInfo, BeginSafety);
        beginHook.Apply();

        var endInfo = sbType.GetMethod(nameof(SpriteBatch.End));
        endHook = new Hook(endInfo, EndSafety);
        endHook.Apply();
    }

    public override void Unload()
    {
        beginHook.Dispose();
        endHook.Dispose();
    }

    private static void BeginSafety(orig_Begin orig, SpriteBatch self,
        SpriteSortMode sortMode,
        BlendState blendState,
        SamplerState samplerState,
        DepthStencilState depthStencilState,
        RasterizerState rasterizerState,
        Effect effect,
        Matrix transformMatrix)
    {
        // If the spritebatch needs to end, then end it and try again
        try
        {
            orig(self, sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix);
        }
        catch (InvalidOperationException e)
        {
            AcidicBosses.Instance.Logger.Info("Caught spritebatch still running", e);
            self.End();
            orig(self, sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix);
        }
    }

    private static void EndSafety(orig_End orig, SpriteBatch self)
    {
        // If the spritebatch has already ended, do nothing
        try
        {
            orig(self);
        }
        catch (InvalidOperationException e)
        {
            AcidicBosses.Instance.Logger.Info("Caught spritebatch already ended", e);
            // Do nothing
        }
    }
}