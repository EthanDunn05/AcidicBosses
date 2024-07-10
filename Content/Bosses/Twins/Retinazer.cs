using System;
using System.IO;
using AcidicBosses.Common.Configs;
using AcidicBosses.Content.Bosses.Twins.Syncing;
using AcidicBosses.Core.StateManagement;
using Luminance.Common.StateMachines;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Content.Bosses.Twins;

public class Retinazer : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.Retinazer;
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
        syncManager = new TwinsSyncManager(Npc.whoAmI, FindSpaz());
        CurrentSyncState = SyncState.TryingToSync;

        phaseTracker = new([
            PhaseOne,
            PhaseTwo
        ]);
    }

    public override bool AcidAI(NPC npc)
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
                TwinsSharedAISystem.SyncedRetAI(this);
                break;
        }
        
        return false;
    }

    private int FindSpaz()
    {
        for (int i = Npc.whoAmI; i < Main.maxNPCs; i++)
        {
            var testNpc = Main.npc[i];
            if (testNpc.active && testNpc.type == NPCID.Spazmatism) return i;
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
                TwinsSharedAISystem.SyncedRetAI(this);
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