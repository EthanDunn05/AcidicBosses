using System.IO;
using System.Reflection;
using AcidicBosses.Common.Effects;
using AcidicBosses.Content.Bosses;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses;

/**
 * The Acidic Bosses Mod!
 * 
 * Created by AcidAssassin
 */
partial class AcidicBosses : Mod
{
    public static Mod Instance => ModContent.GetInstance<AcidicBosses>();

    /// <summary>
    /// Returns true when reworks should be disabled. This is best used for mod compatability.
    /// </summary>
    /// <returns>If the reworks should be disabled</returns>
    public static bool DisableReworks()
    {
        // Prioritize Calamity AI changes.
        if (ModLoader.TryGetMod("CalamityMod", out var calamity))
        {
            if ((bool)calamity.Call("GetDifficultyActive", "revengeance")) return true;
            if ((bool)calamity.Call("GetDifficultyActive", "death")) return true;
            if ((bool)calamity.Call("GetDifficultyActive", "bossrush")) return true;
        }
        
        // Prioritize Fargo's Souls AI changes.
        if (ModLoader.TryGetMod("FargowiltasSouls", out var souls))
        {
            if ((bool)souls.Call("EMode")) return true;
        }
        
        return false;
    }
    
    public override void Load()
    {
        // Disable Calamity messing with vanilla boss AI
        if (ModLoader.TryGetMod("CalamityMod", out var calamity))
        {
            var disableNonRevField = calamity.GetType().GetField("ExternalFlag_DisableNonRevBossAI", BindingFlags.Public | BindingFlags.Static);
            disableNonRevField.SetValue(null, true);
        }
    }
}