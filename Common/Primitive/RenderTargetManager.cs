using System;
using System.Collections.Generic;
using System.Linq;
using AcidicBosses.Common.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace AcidicBosses.Common.Primitive;

public class RenderTargetManager : ModSystem
{
    private static RenderTarget2D primRenderTarget;
    
    private static RenderTarget2D primRenderTargetBeforeNPCs;

    private static Filter currentScreenShader;

    private static readonly List<IPrimDrawer> PrimDrawersList = new();
    
    private static readonly List<IPrimDrawer> PrimDrawersListBeforeNPCs = new();

    private Vector2 previousScreenSize;
    
    public override void Load()
    {
        On_Main.CheckMonoliths += DrawToCustomRenderTargets;
        On_Main.DoDraw_DrawNPCsOverTiles += DrawRenderTargetToMain;
        ResizeRenderTarget(true);
    }
    

    public override void Unload()
    {
        On_Main.CheckMonoliths -= DrawToCustomRenderTargets;
        On_Main.DoDraw_DrawNPCsOverTiles -= DrawRenderTargetToMain;
    }
    
    private static void DrawTarget(RenderTarget2D target)
    {
        if (!PrimDrawersList.Any() && !PrimDrawersListBeforeNPCs.Any())
            return;

        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

        // Draw the RT
        Main.spriteBatch.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        Main.spriteBatch.End();
    }
    
    private void DrawRenderTargetToMain(On_Main.orig_DoDraw_DrawNPCsOverTiles orig, Main self)
    {
        DrawTarget(primRenderTargetBeforeNPCs);
        orig(self);
        DrawTarget(primRenderTarget);
    }

    private void DrawToCustomRenderTargets(On_Main.orig_CheckMonoliths orig)
    {
        // Clear our render target from the previous frame.
        PrimDrawersList.Clear();
        PrimDrawersListBeforeNPCs.Clear();

        // Check every active projectile.
        for (int i = 0; i < Main.projectile.Length; i++)
        {
            Projectile projectile = Main.projectile[i];
            // If the projectile is active, a mod projectile, and uses our interface,
            if (projectile.active && projectile.ModProjectile != null && projectile.ModProjectile is IPrimDrawer pixelPrimitiveProjectile)
                // Add it to the list of prims to draw this frame.
                if(pixelPrimitiveProjectile.drawBehindNpcs)
                    PrimDrawersListBeforeNPCs.Add(pixelPrimitiveProjectile);
                else 
                    PrimDrawersList.Add(pixelPrimitiveProjectile);
        }

        DrawProjectilesToRenderTarget(primRenderTarget, PrimDrawersList);
        DrawProjectilesToRenderTarget(primRenderTargetBeforeNPCs, PrimDrawersListBeforeNPCs);

        // Clear the current render target.
        Main.graphics.GraphicsDevice.SetRenderTarget(null);

        // Call orig.
        orig();
    }
    
    private static void DrawProjectilesToRenderTarget(RenderTarget2D renderTarget, List<IPrimDrawer> prims)
    {
        // Swap to our custom render target.
        SwapToRenderTarget(renderTarget);
        
        if (!prims.Any()) return;
        
        // Start a spritebatch, as one does not exist in the method we're detouring.
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null);

        // Loop through the list and call each draw function.
        foreach (var primDrawer in prims)
            primDrawer.DrawPrims(Main.spriteBatch);

        // End the spritebatch we started.
        Main.spriteBatch.End();
    }

    private static void SwapToRenderTarget(RenderTarget2D renderTarget)
    {
        GraphicsDevice graphicsDevice = Main.graphics.GraphicsDevice;
        SpriteBatch spriteBatch = Main.spriteBatch;

        // If we are in the menu, a server, or any of these are null, return.
        if (Main.gameMenu || Main.dedServ || renderTarget is null || graphicsDevice is null || spriteBatch is null)
            return;

        // Else, set the render target.
        graphicsDevice.SetRenderTarget(renderTarget);
        // "Flush" the screen, removing any previous things drawn to it.
        graphicsDevice.Clear(Color.Transparent);
    }

    private void ResizeRenderTarget(bool load)
    {
        // If not in the game menu, and we arent a dedicated server,
        if (!Main.gameMenu && !Main.dedServ || load && !Main.dedServ)
        {
            // Get the current screen size.
            Vector2 currentScreenSize = new(Main.screenWidth, Main.screenHeight);
            // If it does not match the previous one, we need to update it.
            if (currentScreenSize != previousScreenSize)
            {
                // Render target stuff should be done on the main thread only.
                Main.QueueMainThreadAction(() =>
                {
                    // If it is not null, or already disposed, dispose it.
                    if (primRenderTarget != null && !primRenderTarget.IsDisposed)
                        primRenderTarget.Dispose();
                    
                    // If it is not null, or already disposed, dispose it.
                    if (primRenderTargetBeforeNPCs != null && !primRenderTargetBeforeNPCs.IsDisposed)
                        primRenderTargetBeforeNPCs.Dispose();

                    // Recreate the render target with the current, accurate screen dimensions.
                    primRenderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
                    primRenderTargetBeforeNPCs = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
                });

            }
            // Set the current one to the previous one for next frame.
            previousScreenSize = currentScreenSize;
        }
    }
}