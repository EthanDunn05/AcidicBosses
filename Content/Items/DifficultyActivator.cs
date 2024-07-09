using AcidicBosses.Core.Systems;
using AcidicBosses.Helpers;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AcidicDifficultySystem = AcidicBosses.Core.Systems.DifficultySystem.AcidicDifficultySystem;

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
    public override bool? UseItem(Player player)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            if (Utilities.AnyBosses())
            {
                Main.chatMonitor.NewText(ModLanguage.GetText("Items.DifficultyActivator.RejectChange").Value);
                return true;
            }
            
            AcidicDifficultySystem.AcidicActive = !AcidicDifficultySystem.AcidicActive;
            NetMessage.SendData(MessageID.WorldData);
            Main.chatMonitor.NewText(AcidicDifficultySystem.AcidicActive
                ? ModLanguage.GetText("Items.DifficultyActivator.Activate").Value
                : ModLanguage.GetText("Items.DifficultyActivator.Deactivate").Value
            );
        }

        if (Main.netMode != NetmodeID.Server)
        {
            if (Utilities.AnyBosses())
            {
                SoundEngine.PlaySound(SoundID.NPCHit16);
                return true;
            }
            
            SoundEngine.PlaySound(SoundID.Item4);
        }

        return true;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Amethyst)
            .AddTile(TileID.WorkBenches)
            .Register();
    }
}