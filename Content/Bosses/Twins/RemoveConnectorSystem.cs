using System;
using AcidicBosses.Common.Configs;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.Twins;

/// <summary>
/// Patches vanilla code to not draw the Twins' connector when using custom Twins AI
/// </summary>
public class RemoveConnectorSystem : ModSystem
{
    public override void Load()
    {
        IL_Main.DrawNPCs += RemoveTwinsConnector;
    }
    
    public override void Unload()
    {
        IL_Main.DrawNPCs -= RemoveTwinsConnector;
    }
    
    private static void RemoveTwinsConnector(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            
            /*
            Find this line, where the draw code checks for the twins
            if (npc[i].type == 125 || npc[i].type == 126)
                ldsfld       class Terraria.NPC[] Terraria.Main::npc
                ldloc.2      // i
                ldelem.ref
                ldfld        int32 Terraria.NPC::'type'
                ldc.i4.s     125
                beq.s        IL_00d9
                ldsfld       class Terraria.NPC[] Terraria.Main::npc
                ldloc.2      // i
                ldelem.ref
                ldfld        int32 Terraria.NPC::'type'
            --> ldc.i4.s     126
                bne.un       IL_035e
            */
            
            // Go to the comparison for spaz
            c.GotoNext(i => i.MatchLdcI4(NPCID.Spazmatism));
            
            // Move to just after the if statement
            c.Index++;
            c.Index++;

            // Push whether to hide the vanilla connector to the stack
            c.EmitDelegate<Func<bool>>(HideVanillaConnector);
            
            // Set the flag variable to the current value on the stack
            // When the flag variable is true, the vanilla connector is not drawn
            c.Emit(OpCodes.Stloc_0);
        }
        catch(Exception e)
        {
            MonoModHooks.DumpIL(ModContent.GetInstance<AcidicBosses>(), il);
        }
    }

    private static bool HideVanillaConnector()
    {
        return BossToggleConfig.Get().EnableTwins && !AcidicBosses.DisableReworks();
    }
}