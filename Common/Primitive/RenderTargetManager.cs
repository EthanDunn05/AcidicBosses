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
    private static RenderTarget2D CustomRenderTarget;

    private static Filter CurrentScreenShader;

    private static readonly List<IPrimDrawer> CustomTargetDrawers = new();

    private Vector2 PreviousScreenSize;
    
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
    
    private void DrawRenderTargetToMain(On_Main.orig_DoDraw_DrawNPCsOverTiles orig, Main self)
    {
        orig(self);
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        
        // Draw our RT. The scale is important, it is 2 here as this RT is 0.5x the main screen size.
        Main.spriteBatch.Draw(CustomRenderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        Main.spriteBatch.End();
    }

    private void DrawToCustomRenderTargets(On_Main.orig_CheckMonoliths orig)
    {
        // Clear our render target from the previous frame.
        CustomTargetDrawers.Clear();

        // Check every active projectile.
        for (int i = 0; i < Main.projectile.Length; i++)
        {
            Projectile projectile = Main.projectile[i];
            // If the projectile is active, a mod projectile, and uses our interface,
            if (projectile.active && projectile.ModProjectile != null && projectile.ModProjectile is IPrimDrawer pixelPrimitiveProjectile)
                // Add it to the list of prims to draw this frame.
                CustomTargetDrawers.Add(pixelPrimitiveProjectile);
        }

        DrawProjectilesToRenderTarget(CustomRenderTarget, CustomTargetDrawers);

        // Clear the current render target.
        Main.graphics.GraphicsDevice.SetRenderTarget(null);

        // Call orig.
        orig();
    }
    
    private static void DrawProjectilesToRenderTarget(RenderTarget2D renderTarget, List<IPrimDrawer> customTargetDrawers)
    {
        // Swap to our custom render target.
        SwapToRenderTarget(renderTarget);
        
        if (!customTargetDrawers.Any()) return;
        
        // Start a spritebatch, as one does not exist in the method we're detouring.
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null);

        // Loop through the list and call each draw function.
        foreach (var customTargetDrawer in customTargetDrawers)
            customTargetDrawer.DrawPrims(Main.spriteBatch);

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
            if (currentScreenSize != PreviousScreenSize)
            {
                // Render target stuff should be done on the main thread only.
                Main.QueueMainThreadAction(() =>
                {
                    // If it is not null, or already disposed, dispose it.
                    if (CustomRenderTarget != null && !CustomRenderTarget.IsDisposed)
                        CustomRenderTarget.Dispose();

                    // Recreate the render target with the current, accurate screen dimensions.
                    // In our case, we want to half them to downscale it, pixelating it.
                    CustomRenderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight );
                });

            }
            // Set the current one to the previous one for next frame.
            PreviousScreenSize = currentScreenSize;
        }
    }
}