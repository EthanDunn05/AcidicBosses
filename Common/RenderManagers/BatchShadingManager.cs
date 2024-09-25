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
[Autoload(Side = ModSide.Client)]
public class BatchShadingManager : ModSystem
{
    public delegate void ShadedDrawAction(SpriteBatch spritebatch);

    private static readonly Dictionary<ManagedShader, List<ShadedDrawAction>> NpcsToDrawBehind = new();
    private static readonly Dictionary<ManagedShader, List<ShadedDrawAction>> NpcsToDrawAbove = new();
    private static readonly Dictionary<ManagedShader, List<ShadedDrawAction>> ProjsToDraw = new();
    
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

    public static void DrawNpc(NPC npc, ManagedShader shader, ShadedDrawAction drawAction)
    {
        switch (npc.behindTiles)
        {
            case true:
            {
                if (!NpcsToDrawBehind.ContainsKey(shader)) NpcsToDrawBehind.Add(shader, []);

                NpcsToDrawBehind[shader].Add(drawAction);
                break;
            }
            case false:
            {
                if (!NpcsToDrawAbove.ContainsKey(shader)) NpcsToDrawAbove.Add(shader, []);

                NpcsToDrawAbove[shader].Add(drawAction);
                break;
            }
        }
    }
    
    public static void DrawProjectile(Projectile proj, ManagedShader shader, ShadedDrawAction drawAction)
    {
        if (!ProjsToDraw.ContainsKey(shader)) ProjsToDraw.Add(shader, []);
        
        ProjsToDraw[shader].Add(drawAction);
    }
    
    private void DrawShadedNpcs(On_Main.orig_DrawNPCs orig, Main self, bool behindtiles)
    {
        if (behindtiles) DrawBatches(NpcsToDrawBehind);
        else DrawBatches(NpcsToDrawAbove);
        
        orig(self, behindtiles);
    }
    
    private void DrawShadedProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
    {
        DrawBatches(ProjsToDraw);
        
        orig(self);
    }

    private void DrawBatches(Dictionary<ManagedShader, List<ShadedDrawAction>> batches)
    {
        foreach (var batch in batches)
        {
            Main.spriteBatch.EnterShader();
            foreach (var drawAction in batch.Value)
            {
                drawAction.Invoke(Main.spriteBatch);
            }
            Main.spriteBatch.ExitShader();
        }
        
        batches.Clear();
    }
}