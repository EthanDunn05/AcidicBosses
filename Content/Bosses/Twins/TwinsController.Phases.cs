using System;
using AcidicBosses.Core.StateManagement;
using Luminance.Common.StateMachines;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;
using Terraria;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    private PhaseState PhaseUntransformed => new PhaseState(Phase_Untransformed, EnterPhaseUntransformed);
    private PhaseState PhaseTransformation => new(Phase_Transformation, CreateTransformationAnimation);
    private PhaseState PhaseTransformed1 => new PhaseState(Phase_Transformed1, EnterPhaseTransformed1);
    private PhaseState PhaseTransformed2 => new PhaseState(Phase_Transformed2, EnterPhaseTransformed2);
    private PhaseState PhaseSoloSpaz => new PhaseState(Phase_SoloSpaz, EnterSoloSpaz);
    private PhaseState PhaseSoloRet => new PhaseState(Phase_SoloRet, EnterSoloRet);
    
    
    private void EnterPhaseUntransformed()
    {
        var hover = new AttackState(() => Attack_Hover(90, 20, 0.25f), 30);
        var doubleDash = new AttackState(() => Attack_DoubleDash(10, 30, 15, 30), 60);
        var longCrossDash = new AttackState(() => Attack_CrossDash(20, 60, 45), 0);
        var plusDash = new AttackState(() => Attack_PlusDash(20, 60, 25), 0);
        var crossDash = new AttackState(() => Attack_CrossDash(20, 60, 25), 0);
        var alternatingDashes = new AttackState(() => Attack_AlternatingDashes(60 * 4, 10, 30, 15, 20), 0);
        var fastAlternatingDashes = new AttackState(() => Attack_AlternatingFastDashes(60 * 3, 10, 60, 5, 10), 0);
        var recenter = new AttackState(Recenter, 0);
        var clash = new AttackState(() => Attack_Clash(false, false), 60);
        
        attackManager.SetAttackPattern([
            hover,
            doubleDash, doubleDash,
            alternatingDashes,
            fastAlternatingDashes,
            recenter,
            hover,
            doubleDash, doubleDash, doubleDash,
            clash,
            hover,
            longCrossDash, plusDash, crossDash, plusDash, crossDash,
            recenter,
            hover,
            alternatingDashes, clash
        ]);
    }
    
    private void Phase_Untransformed()
    {
        if (attackManager.InWindDown)
        {
            if (AverageLifePercent <= 0.75f)
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
        var hover = new AttackState(() => Attack_Hover(45, 30, 0.355f), 0);
        var doubleDash = new AttackState(() => Attack_DoubleDash(15, 30, 15, 30), 60);
        var recenter = new AttackState(Recenter, 0);
        var sweep = new AttackState(Attack_SweepingLaser, 30);
        var circle = new AttackState(Attack_SpazCircle, 0);
        var flamethrower = new AttackState(Attack_FlamethrowerChase, 30);
        var burst = new AttackState(Attack_RetLaserBurst, 60);
        var clash = new AttackState(() => Attack_Clash(true, false), 30);
        
        attackManager.SetAttackPattern([
            hover,
            burst,
            doubleDash,
            clash, clash, clash,
            circle,
            sweep, doubleDash
        ]);
    }
    
    private void Phase_Transformed1()
    {
        Spazmatism.MechForm = true;
        Retinazer.MechForm = true;
        
        if (attackManager.InWindDown)
        {
            if (AverageLifePercent <= 0.55f)
            {
                phaseTracker.NextPhase();
                attackManager.Reset();
                return;
            }
            
            var target = Main.player[NPC.target];
            Hover(30f, 0.3f);
            return;
        }
        
        attackManager.RunAttackPattern();
    }
    
    private void EnterPhaseTransformed2()
    {
        var hover = new AttackState(() => Attack_Hover(30, 30, 0.355f), 0);
        var doubleDash = new AttackState(() => Attack_DoubleDash(15, 30, 15, 30), 60);
        var recenter = new AttackState(Recenter, 0);
        var sweep = new AttackState(Attack_SweepingLaser, 15);
        var circle = new AttackState(Attack_SpazCircle, 0);
        var flamethrower = new AttackState(Attack_FlamethrowerChase, 30);
        var burst = new AttackState(Attack_RetLaserBurst, 30);
        var clash = new AttackState(() => Attack_Clash(true, true), 30);
        var fireballs = new AttackState(Attack_TripleFireball, 0);
        var lasers = new AttackState(Attack_LaserSpread, 0);
        
        attackManager.SetAttackPattern([
            hover,
            burst,
            doubleDash,
            clash, clash, clash,
            hover,
            lasers, fireballs,
            circle,
            sweep, doubleDash, fireballs
        ]);
    }
    
    private void Phase_Transformed2()
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

    private void EnterSoloSpaz()
    {
        attackManager.Reset();
        if (Main.netMode != NetmodeID.MultiplayerClient) Utilities.BroadcastText("Spazmatism is enraged at the loss of their sibling!", Color.Lime);

        var spit = new AttackState(Attack_TripleFireball, 20);
        var circle = new AttackState(Attack_SpazCircle, 60);
        var dash = new AttackState(Attack_EnragedDash, 30);
        var flemthrower = new AttackState(Attack_FlamethrowerChase, 30);
        
        attackManager.SetAttackPattern([
            flemthrower,
            spit, spit, dash,
            circle,
            dash, dash, dash, spit
        ]);
    }

    private void Phase_SoloSpaz()
    {
        if (attackManager.InWindDown)
        {
            var target = Main.player[NPC.target];
            Hover(Spazmatism, 50f, 0.4f);
            return;
        }
        
        attackManager.RunAttackPattern();
    }

    private void EnterSoloRet()
    {
        attackManager.Reset();
        if (Main.netMode != NetmodeID.MultiplayerClient) Utilities.BroadcastText("Retinazer is enraged at the loss of their sibling!", Color.Red);

        var hover = new AttackState(() => Attack_Hover(30, 50, 0.5f), 0);
        var lasers = new AttackState(Attack_LaserSpread, 20);
        var dash = new AttackState(Attack_EnragedDash, 30);
        var burst = new AttackState(Attack_RetLaserBurst, 60);
        var sweep = new AttackState(Attack_SweepingLaser, 60);
        
        attackManager.SetAttackPattern([
            hover,
            burst, dash, hover,
            lasers, dash, dash, hover,
            sweep, dash, lasers, lasers
        ]);
    }

    private void Phase_SoloRet()
    {
        if (attackManager.InWindDown)
        {
            var target = Main.player[NPC.target];
            Hover(Retinazer, 50f, 0.4f);
            return;
        }
        
        attackManager.RunAttackPattern();
    }
}