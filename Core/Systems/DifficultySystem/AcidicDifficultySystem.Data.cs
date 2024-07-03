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
    private static bool loadedKey = false;

    public override void SaveWorldHeader(TagCompound tag)
    {
        if (!loadedKey) return;
        
        tag["AcidicActive"] = AcidicActive;
    }

    public override void SaveWorldData(TagCompound tag)
    {
        if (selectionOption != AcidicEnabledID.None) AcidicActive = selectionOption == AcidicEnabledID.Enabled;
        
        tag["AcidicActive"] = AcidicActive;
        loadedKey = true;
    }

    public override void LoadWorldData(TagCompound tag)
    {
        if (tag.ContainsKey("AcidicActive"))
        {
            AcidicActive = tag.GetBool("AcidicActive");
            loadedKey = true;
        }
        else
        {
            AcidicActive = false;
            loadedKey = false;
        }
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(AcidicActive);
    }

    public override void NetReceive(BinaryReader reader)
    {
        AcidicActive = reader.ReadBoolean();
        loadedKey = true;
    }
}