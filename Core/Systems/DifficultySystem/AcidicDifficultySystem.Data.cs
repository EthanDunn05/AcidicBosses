using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;

namespace AcidicBosses.Core.Systems.DifficultySystem;

public partial class AcidicDifficultySystem : ModSystem
{
    public static bool AcidicActive { get; set; }

    public override void SaveWorldHeader(TagCompound tag)
    {
        tag["AcidicActive"] = AcidicActive;
    }

    public override void SaveWorldData(TagCompound tag)
    {
        
        if (!tag.ContainsKey("AcidicActive"))
        {
            if (selectionOption == AcidicEnabledID.None) selectionOption = AcidicEnabledID.Enabled;
            AcidicActive = selectionOption == AcidicEnabledID.Enabled;
        }
        
        tag["AcidicActive"] = AcidicActive;
    }

    public override void LoadWorldData(TagCompound tag)
    {
        if (tag.ContainsKey("AcidicActive"))
        {
            AcidicActive = tag.GetBool("AcidicActive");
        }
        else
        {
            AcidicActive = false;
        }
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(AcidicActive);
    }

    public override void NetReceive(BinaryReader reader)
    {
        AcidicActive = reader.ReadBoolean();
    }
}