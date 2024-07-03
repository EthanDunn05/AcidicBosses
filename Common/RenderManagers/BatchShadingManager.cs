using System.Collections.Generic;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace AcidicBosses.Common.RenderManagers;

/// <summary>
/// Organizes drawing of sprites with a shader into batches to improve performance when
/// drawing a lot of sprites with the same shader.
/// </summary>
public class BatchShadingManager : ModSystem
{
    public delegate void ShadedDrawAction(SpriteBatch spritebatch);

    private static readonly Dictionary<MiscShaderData, List<ShadedDrawAction>> NpcsToDraw = new();
    private static readonly Dictionary<MiscShaderData, List<ShadedDrawAction>> ProjsToDraw = new();
    
    public override void OnModLoad()
    {
        On_Main.DrawNPCs += DrawShadedNpcs;
        On_Main.DrawProjectiles += DrawShadedProjectiles;
    }

    public override void OnModUnload()
    {
        On_Main.DrawNPCs -= DrawShadedNpcs;
        On_Main.DrawProjectiles -= DrawShadedProjectiles;
    }

    public static void DrawNpc(MiscShaderData shader, ShadedDrawAction drawAction)
    {
        if (!NpcsToDraw.ContainsKey(shader)) NpcsToDraw.Add(shader, []);
        
        NpcsToDraw[shader].Add(drawAction);
    }
    
    public static void DrawProjectile(MiscShaderData shader, ShadedDrawAction drawAction)
    {
        if (!ProjsToDraw.ContainsKey(shader)) ProjsToDraw.Add(shader, []);
        
        ProjsToDraw[shader].Add(drawAction);
    }
    
    private void DrawShadedNpcs(On_Main.orig_DrawNPCs orig, Main self, bool behindtiles)
    {
        foreach (var batch in NpcsToDraw)
        {
            Main.spriteBatch.EnterShader();
            foreach (var drawAction in batch.Value)
            {
                drawAction.Invoke(Main.spriteBatch);
            }
            Main.spriteBatch.ExitShader();
        }
        
        NpcsToDraw.Clear();
        
        orig(self, behindtiles);
    }
    
    private void DrawShadedProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
    {
        foreach (var batch in ProjsToDraw)
        {
            Main.spriteBatch.EnterShader();
            foreach (var drawAction in batch.Value)
            {
                drawAction.Invoke(Main.spriteBatch);
            }
            Main.spriteBatch.ExitShader();
        }
        
        ProjsToDraw.Clear();
        
        orig(self);
    }
}