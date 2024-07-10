using System;
using System.IO;
using AcidicBosses.Common.Configs;
using AcidicBosses.Content.Bosses.Twins.Syncing;
using AcidicBosses.Core.StateManagement;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Content.Bosses.Twins;

public class Spazmatism : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.Spazmatism;
    protected override bool BossEnabled => BossToggleConfig.Get().EnableTwins;

    #region AI

    public SyncState CurrentSyncState
    {
        get => (SyncState) Npc.ai[0];
        set => Npc.ai[0] = (float) value;
    }
    
    private TwinsSyncManager syncManager;
    
    private PhaseTracker phaseTracker;
    
    public override void OnFirstFrame(NPC npc)
    {
        syncManager = new TwinsSyncManager(FindRet(), Npc.whoAmI);
        CurrentSyncState = SyncState.Unsynced;

        phaseTracker = new([
            PhaseOne,
            PhaseTwo
        ]);
    }

    public override bool AcidAI(NPC npc)
    {
        
        phaseTracker.RunPhaseAI();
        
        return false;
    }
    
    private int FindRet()
    {
        for (int i = Npc.whoAmI; i >= 0; i--)
        {
            var testNpc = Main.npc[i];
            if (testNpc.active && testNpc.type == NPCID.Retinazer) return i;
        }

        throw new IndexOutOfRangeException();
    }
    
    #endregion
    
    #region Phases

    private PhaseState PhaseOne => new(Phase_One);
    
    private void Phase_One()
    {
        
    }

    private PhaseState PhaseTwo => new(Phase_Two);

    private void Phase_Two()
    {
        switch (CurrentSyncState)
        {
            case SyncState.TryingToSync:
                TwinsSharedAISystem.TryToSync(Npc, syncManager);
                break;
            case SyncState.ReadyToSync:
                TwinsSharedAISystem.SyncAIs(Npc, AttackManager);
                break;
            case SyncState.Synced:
                TwinsSharedAISystem.SyncedSpazAI(this);
                break;
        }
    }
    
    #endregion

    public override void SendAcidAI(BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        syncManager.Send(binaryWriter);
    }

    public override void ReceiveAcidAI(BitReader bitReader, BinaryReader binaryReader)
    {
        syncManager.Recieve(binaryReader);
    }
}