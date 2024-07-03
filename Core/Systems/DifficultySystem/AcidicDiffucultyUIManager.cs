using System.Collections.Generic;
using AcidicBosses.Common.Textures;
using Luminance.Core.MenuInfoUI;
using Terraria.ID;

namespace AcidicBosses.Core.Systems.DifficultySystem;

public class AcidicDiffucultyUIManager : InfoUIManager
{
    public override IEnumerable<WorldInfoIcon> GetWorldInfoIcons()
    {
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