using System.Collections.Generic;
using AcidicBosses.Common.Textures;
using Luminance.Core.MenuInfoUI;
using Terraria.ID;

namespace AcidicBosses.Core.Systems.DifficultySystem;

public class AcidicDiffucultyUIManager : InfoUIManager
{
    public override IEnumerable<WorldInfoIcon> GetWorldInfoIcons()
    {
        // Don't know if this is acidic bosses world or not.
        yield return new WorldInfoIcon(TextureRegistry.TerrariaItem(ItemID.ExplosiveBunny),
            "Mods.AcidicBosses.InfoIcons.AcidicUnknownIcon",
            worldData => !worldData.TryGetHeaderData<AcidicDifficultySystem>(out _),
            100);
        
        yield return new WorldInfoIcon("Terraria/Images/UI/WorldCreation/IconDifficultyNormal",
            "Mods.AcidicBosses.InfoIcons.AcidicInactiveIcon",
            worldData =>
            {
                if (!worldData.TryGetHeaderData<AcidicDifficultySystem>(out var tag))
                    return false;

                if (!tag.ContainsKey("AcidicActive")) return true;
                return !tag.GetBool("AcidicActive");
            },
            100);
        
        // This is an acidic bosses world
        yield return new WorldInfoIcon("Terraria/Images/UI/WorldCreation/IconDifficultyMaster",
            "Mods.AcidicBosses.InfoIcons.AcidicActiveIcon",
            worldData =>
            {
                if (!worldData.TryGetHeaderData<AcidicDifficultySystem>(out var tag))
                    return false;

                return tag.ContainsKey("AcidicActive") && tag.GetBool("AcidicActive");
            },
            100);
    }
}