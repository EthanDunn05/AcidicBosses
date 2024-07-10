using System.IO;
using AcidicBosses.Core.StateManagement;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.Twins.Syncing;

/// <summary>
/// Parts of the Twins AI that is shared between the eyes
/// </summary>
public class TwinsSharedAISystem : ModSystem
{
    private static readonly AttackManager AttackManager = new();

    public override void PreUpdateNPCs()
    {
        AttackManager.PreAttackAi();
    }

    public override void PostUpdateNPCs()
    {
        AttackManager.PostAttackAi();
    }

    #region Synced
    
    public static void SyncedSpazAI(Spazmatism npc)
    {
        var target = npc.TargetPlayer.Center;
        npc.LookTowards(target, 0.05f);

        npc.Npc.Center = target + new Vector2(200, 0);
    }
    
    public static void SyncedRetAI(Retinazer npc)
    {
        var target = npc.TargetPlayer.Center;
        npc.LookTowards(target, 0.05f);
        
        npc.Npc.Center = target - new Vector2(200, 0);
    }
    
    #endregion
    
    #region Syncing
    
    public static void TryToSync(NPC npc, TwinsSyncManager syncManager)
    {
        if (syncManager.RetSyncState is SyncState.TryingToSync or SyncState.ReadyToSync 
            && syncManager.SpazSyncState is SyncState.TryingToSync or SyncState.ReadyToSync )
        {
            npc.ai[0] = (float) SyncState.ReadyToSync;
            return;
        }
    }

    public static void SyncAIs(NPC npc, AttackManager attackManager)
    {
        AttackManager.Reset();
        attackManager.Reset();
        npc.ai[0] = (float) SyncState.Synced;
    }
    
    #endregion

    public static void Send(BinaryWriter writer)
    {
        AttackManager.Serialize(writer);
    }

    public static void Recieve(BinaryReader reader)
    {
        AttackManager.Deserialize(reader);
    }
}