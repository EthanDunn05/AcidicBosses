using System.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Core.Systems;

public class DifficultySystem : ModSystem
{
    public static bool AcidicActive { get; set; }

    public override void ClearWorld()
    {
        AcidicActive = false;
    }

    public override void SaveWorldData(TagCompound tag)
    {
        tag["AcidicActive"] = AcidicActive;
    }

    public override void LoadWorldData(TagCompound tag)
    {
        AcidicActive = tag.GetBool("AcidicActive");
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