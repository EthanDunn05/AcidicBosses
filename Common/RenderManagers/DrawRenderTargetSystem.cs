using System;
using System.Collections.Generic;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace AcidicBosses.Common.RenderManagers;

/// <summary>
/// Organizes drawing of which are on their own render target by batching the draw calls
/// </summary>
[Autoload(Side = ModSide.Client)]
public class DrawRenderTargetSystem : ModSystem
{
    public delegate void RtDrawAction(SpriteBatch spritebatch);

    private static readonly Dictionary<ShadedRenderTarget, List<RtDrawAction>> DrawActions = new();
    
    public override void OnModLoad()
    {
        RenderTargetManager.RenderTargetUpdateLoopEvent += DrawToRenderTargets;
        On_Main.DrawNPCs += DrawNpcLayer;
        On_Main.DrawProjectiles += DrawProjectileLayer;
        Main.OnPostDraw += ClearDrawActions;
    }
    
    public override void OnModUnload()
    {
        On_Main.DrawNPCs -= DrawNpcLayer;
        On_Main.DrawProjectiles -= DrawProjectileLayer;
        Main.OnPostDraw -= ClearDrawActions;
    }

    public static void DrawToTarget(ShadedRenderTarget renderTarget, RtDrawAction drawAction)
    {
        if (!DrawActions.ContainsKey(renderTarget)) DrawActions.Add(renderTarget, []);
        
        DrawActions[renderTarget].Add(drawAction);
    }
    
    private void ClearDrawActions(GameTime obj)
    {
        // DrawActions.Clear();
    }
    
    private void DrawProjectileLayer(On_Main.orig_DrawProjectiles orig, Main self)
    {
        foreach (var renderTarget in DrawActions.Keys)
        {
            if (renderTarget.Layer != RenderLayer.Projectile) continue;
            
            Main.spriteBatch.EnterShader();
            renderTarget.ApplyShader();
            Main.spriteBatch.Draw(renderTarget, Main.screenLastPosition - Main.screenPosition, Color.White);
            Main.spriteBatch.End();

            DrawActions.Remove(renderTarget);
        }

        orig(self);
    }

    private void DrawNpcLayer(On_Main.orig_DrawNPCs orig, Main self, bool behindtiles)
    {
        
        foreach (var renderTarget in DrawActions.Keys)
        {
            if (renderTarget.Layer != RenderLayer.Npc) continue;
            
            Main.spriteBatch.EnterShader();
            renderTarget.ApplyShader();
            Main.spriteBatch.Draw(renderTarget, Main.screenLastPosition - Main.screenPosition, Color.White);
            Main.spriteBatch.End();
            
            DrawActions.Remove(renderTarget);
        }

        orig(self, behindtiles);
    }
    
    private void DrawToRenderTargets()
    {
        try
        {
            Main.spriteBatch.End();
        }
        catch (Exception _)
        {
            // ignored
        }

        foreach (var batch in DrawActions)
        {
            batch.Key.SwapToRenderTarget();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            foreach (var drawAction in batch.Value)
            {
                drawAction.Invoke(Main.spriteBatch);
            }
            
            Main.spriteBatch.End();
        }
        
        Main.instance.GraphicsDevice.SetRenderTarget(null);
    }
}