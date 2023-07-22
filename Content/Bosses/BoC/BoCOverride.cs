using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.BoC;

public class BoCOverride : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.BrainofCthulhu;


    #region Phase And Attack Patterns
    
    private enum PhaseState
    {
        One,
    }

    private enum Attack
    {
        
    }

    private Attack[] CurrentAttackPattern => CurrentPhase switch
    {
        PhaseState.One => phaseOneAP,
        _ => throw new UsageException(
            $"BoC is in the PhaseState {CurrentPhase} and does not have an attack pattern")
    };

    private Action CurrentAi => CurrentPhase switch
    {
        PhaseState.One => PhaseOneAI,
        _ => throw new UsageException(
            $"BoC is in the PhaseState {CurrentPhase} and does not have an ai")
    };

    private Attack[] phaseOneAP =
    {

    };
    
    private int AiTimer
    {
        get => (int) Npc.ai[0];
        set => Npc.ai[0] = value;
    }

    private PhaseState CurrentPhase
    {
        get => (PhaseState) Npc.ai[1];
        set => Npc.ai[1] = (float) value;
    }

    private int CurrentAttackIndex
    {
        get => (int) Npc.ai[2];
        set => Npc.ai[2] = value;
    }

    private Attack CurrentAttack => CurrentAttackPattern[CurrentAttackIndex];
    
    #endregion

    private bool countUpTimer = false;

    private bool isOpen = false;

    public override void OnFirstFrame(NPC npc)
    {
        CurrentPhase = PhaseState.One;
        AiTimer = 0;
    }

    public override bool AcidAI(NPC npc)
    {
        if (AiTimer > 0 && !countUpTimer)
            AiTimer--;

        CurrentAi.Invoke();
        
        if (countUpTimer)
            AiTimer++;

        return false;
    }

    #region AI

    private void PhaseOneAI()
    {
        
    }

    #endregion
}