using AcidicBosses.Core.StateManagement;
using Terraria;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    private PhaseState PhaseUntransformed => new PhaseState(Phase_Untransformed, EnterPhaseUntransformed);
    private PhaseState PhaseTransformation => new(Phase_Transformation, CreateTransformationAnimation);
    private PhaseState PhaseTransformed1 => new PhaseState(Phase_Transformed1, EnterPhaseTransformed1);
    
    private void EnterPhaseUntransformed()
    {
        var hover = new AttackState(() => Attack_Hover(90, 20, 0.25f), 30);
        var doubleDash = new AttackState(() => Attack_DoubleDash(10, 30, 15, 30), 60);
        var longCrossDash = new AttackState(() => Attack_CrossDash(20, 60, 30), 0);
        var plusDash = new AttackState(() => Attack_PlusDash(20, 60, 25), 0);
        var crossDash = new AttackState(() => Attack_CrossDash(20, 60, 25), 0);
        var alternatingDashes = new AttackState(() => Attack_AlternatingDashes(60 * 4, 10, 30, 15, 20), 0);
        var fastAlternatingDashes = new AttackState(() => Attack_AlternatingFastDashes(60 * 3, 10, 60, 5, 10), 0);
        var recenter = new AttackState(Recenter, 0);
        
        attackManager.SetAttackPattern([
            hover,
            doubleDash, doubleDash,
            alternatingDashes,
            fastAlternatingDashes,
            recenter,
            hover,
            doubleDash, doubleDash, doubleDash,
            hover,
            longCrossDash, plusDash, crossDash, plusDash, crossDash,
            recenter,
            hover,
            alternatingDashes
        ]);
    }
    
    private void Phase_Untransformed()
    {
        if (attackManager.InWindDown)
        {
            if (AverageLifePercent <= 1f) // TODO Temp
            {
                phaseTracker.NextPhase();
                attackManager.Reset();
                return;
            }
            
            Hover(20f, 0.25f);
            return;
        }
        
        attackManager.RunAttackPattern();
    }

    private void Phase_Transformation()
    {
        var done = transformAnimation.RunAnimation();
        if (done)
        {
            phaseTracker.NextPhase();
            attackManager.Reset();
        }
    }
    
    private void EnterPhaseTransformed1()
    {
        var hover = new AttackState(() => Attack_Hover(45, 30, 0.355f), 30);
        var doubleDash = new AttackState(() => Attack_DoubleDash(15, 30, 15, 30), 60);
        var recenter = new AttackState(Recenter, 0);
        var sweep = new AttackState(Attack_SweepingLaser, 30);
        var circle = new AttackState(Attack_SpazCircle, 60);
        var flamethrower = new AttackState(Attack_FlamethrowerChase, 30);
        
        attackManager.SetAttackPattern([
            hover,
            sweep,
            circle, flamethrower
        ]);
    }
    
    private void Phase_Transformed1()
    {
        Spazmatism.MechForm = true;
        Retinazer.MechForm = true;
        
        if (attackManager.InWindDown)
        {
            var target = Main.player[NPC.target];
            Hover(30f, 0.3f);
            return;
        }
        
        attackManager.RunAttackPattern();
    }
}