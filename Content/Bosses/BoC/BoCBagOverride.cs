using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.BoC;

public class BoCBagOverride : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation)
    {
        return entity.type == ItemID.BrainOfCthulhuBossBag;
    }

    public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
    {
        itemLoot.Add(ItemDropRule.Common(ItemID.TissueSample, 1, 75, 125));
    }
}