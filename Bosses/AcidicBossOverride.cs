using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Bosses;

public abstract class AcidicBossOverride : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public abstract int OverriddenNpc { get; }

    private bool isFirstFrame = true;
    
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
    {
        return entity.type == OverriddenNpc;
    }

    public virtual void OnFirstFrame(NPC npc)
    {
        
    }

    public sealed override bool PreAI(NPC npc)
    {
        if (isFirstFrame)
        {
            OnFirstFrame(npc);
            isFirstFrame = false;
        }
        
        return AcidAI(npc);
    }

    public virtual bool AcidAI(NPC npc)
    {
        return true;
    }
    
    protected static void NetSync(NPC npc, bool onlySendFromServer = true)
    {
        if (onlySendFromServer && Main.netMode != NetmodeID.Server)
            return;
        
        if (Main.netMode != NetmodeID.SinglePlayer)
            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
    }
}