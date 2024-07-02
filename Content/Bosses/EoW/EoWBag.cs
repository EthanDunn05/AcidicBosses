using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.EoW;

public class EoWBag : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation)
    {
        return entity.type == ItemID.EaterOfWorldsBossBag;
    }

    public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
    {
        itemLoot.Add(ItemDropRule.Common(ItemID.ShadowScale, 1, 75, 125));
    }   
}