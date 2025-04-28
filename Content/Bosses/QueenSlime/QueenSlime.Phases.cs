using AcidicBosses.Core.StateManagement;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public partial class QueenSlime
{
    private PhaseState PhaseIntro => new(Phase_Intro);
    private PhaseState PhaseOne => new(Phase_One, EnterPhase_One);
    private PhaseState PhaseTwo => new(Phase_Two, EnterPhase_Two);
    private PhaseState PhaseTransform => new(Phase_Transform);

    private void Phase_Intro()
    {
        AttackManager.CountUp = true;
        if (AttackManager.AiTimer == 0)
        {
            Teleport(TargetPlayer.Center + new Vector2(0f, -500f));
        }
        if (Attack_Slam(true)) phaseTracker.NextPhase();
    }

    private void EnterPhase_One()
    {
        AttackManager.Reset();
        
        AttackManager.SetAttackPattern([
            new AttackState(Attack_WaitForLand, 30),
            new AttackState(() => Attack_AirShotgun(3, MathHelper.Pi / 3f, true), 15),
            new AttackState(() => Attack_JumpToPlayer(60), 30),
            new AttackState(() => Attack_SummonSlimes(3), 15),
            new AttackState(() => Attack_Slam(true), 30),
            new AttackState(Attack_WaitForLand, 30),
            new AttackState(() => Attack_JumpToPlayer(60), 30),
            new AttackState(() => Attack_AirShotgun(3, MathHelper.Pi / 3f, true), 15),
            new AttackState(() => Attack_JumpToPlayer(60), 30),
            new AttackState(() => Attack_Slam(true), 30),
        ]);
    }

    private void Phase_One()
    {
        if (AttackManager.InWindDown)
        {
            if (Npc.GetLifePercent() <= 0.75f)
            {
                AttackManager.Reset();
                phaseTracker.NextPhase();
            }
            
            if (Npc.Distance(TargetPlayer.Center) > 2000f)
            {
                FlyTo(TargetPlayer.Center + new Vector2(0, -250f), 30f, 1f);
                AttackManager.AiTimer++;
                singleFlap = true;
                grounded = false;
            }
            return;
        }
        
        AttackManager.RunAttackPattern();
    }
    
    private void EnterPhase_Two()
    {
        AttackManager.Reset();
        
        AttackManager.SetAttackPattern([
            new AttackState(() => Attack_JumpToPlayer(45), 15),
            new AttackState(() => Attack_AirShotgun(4, MathHelper.Pi / 3f, true), 15),
            new AttackState(() => Attack_Slam(true), 15),
            new AttackState(() => Attack_JumpToPlayer(30), 0),
            new AttackState(() => Attack_SummonSlimes(5), 30),
            new AttackState(() => Attack_JumpToPlayer(45), 15),
            new AttackState(() => Attack_AirShotgun(4, MathHelper.Pi / 3f, true), 15),
            new AttackState(() => Attack_Slam(true), 15),
            new AttackState(() => Attack_InstantJumpToPlayer(60), 15),
            new AttackState(() => Attack_LaserBurst(), 30),
        ]);
    }

    private void Phase_Two()
    {
        if (AttackManager.InWindDown)
        {
            if (Npc.Distance(TargetPlayer.Center) > 2000f)
            {
                FlyTo(TargetPlayer.Center + new Vector2(0, -250f), 30f, 1f);
                AttackManager.AiTimer++;
                singleFlap = true;
                grounded = false;
            }
            return;
        }
        
        AttackManager.RunAttackPattern();
    }

    private void Phase_Transform()
    {
        transformAnimation ??= PrepareTransformAnimation();
        if (transformAnimation.RunAnimation()) phaseTracker.NextPhase();
    }
}