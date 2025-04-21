using AcidicBosses.Core.StateManagement;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public partial class QueenSlime
{
    private PhaseState PhaseIntro => new(Phase_Intro);
    private PhaseState PhaseOne => new(Phase_One, EnterPhase_One);

    private void Phase_Intro()
    {
        if (Attack_Slam()) phaseTracker.NextPhase();
    }

    private void EnterPhase_One()
    {
        AttackManager.Reset();
        
        AttackManager.SetAttackPattern([
            new AttackState(() => Attack_JumpToPlayer(60), 0),
            new AttackState(() => Attack_SummonSlimes(3), 30),
            new AttackState(Attack_Slam, 30),
            new AttackState(() => Attack_InstantJumpToPlayer(60), 30),
            new AttackState(() => Attack_TripleShoot(MathHelper.Pi / 6f, true), 15),
        ]);
    }

    private void Phase_One()
    {
        drawExtraWings = true;
        if (AttackManager.InWindDown)
        {
            return;
        }
        
        AttackManager.RunAttackPattern();
    }
}