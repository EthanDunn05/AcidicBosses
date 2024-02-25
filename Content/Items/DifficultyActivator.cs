using AcidicBosses.Core.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Items;

public class DifficultyActivator : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 16;
        Item.height = 16;
        Item.maxStack = 1;
        Item.value = 0;
        Item.rare = ItemRarityID.Expert;

        Item.useTime = 60;
        Item.useAnimation = 60;
        Item.useStyle = ItemUseStyleID.HoldUp;
    }

    // This will be used when I get around to implementing a difficulty toggle
    // public override bool? UseItem(Player player)
    // {
    //     if (Main.netMode != NetmodeID.MultiplayerClient)
    //     {
    //         DifficultySystem.AcidicActive = !DifficultySystem.AcidicActive;
    //         
    //         if (DifficultySystem.AcidicActive) Main.chatMonitor.NewText("Acidic Activated");
    //         else Main.chatMonitor.NewText("Acidic Deactivated");
    //
    //         var packet = Mod.GetPacket();
    //         packet.Write((byte) AcidicBosses.MessageType.SyncDifficulty);
    //         packet.Write(DifficultySystem.AcidicActive);
    //         packet.Send();
    //     }
    //
    //     return true;
    // }
}