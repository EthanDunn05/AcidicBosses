﻿using System.IO;
using AcidicBosses.Common.Configs;
using AcidicBosses.Common.Effects;
using AcidicBosses.Content.Particles;
using AcidicBosses.Content.Particles.Animated;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Content.Projectiles;
using AcidicBosses.Core.StateManagement;
using AcidicBosses.Helpers;
using AcidicBosses.Helpers.NpcHelpers;
using Luminance.Common.Easings;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Content.Bosses.EoC;

public class EoC : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.EyeofCthulhu;

    protected override bool BossEnabled => BossToggleConfig.Get().EnableEyeOfCthulhu;

    #region AI
    
    private PhaseTracker phaseTracker;

    private bool useAfterimages = false;

    private bool isFleeing = false;

    private bool mouthMode = false;

    public override void OnFirstFrame(NPC npc)
    {
        phaseTracker = new PhaseTracker([
            PhaseOne,
            PhaseTransitionOne,
            PhaseTwo,
            PhaseTransitionTwo,
            PhaseThree,
            PhaseFour
        ]);
    }

    public override bool AcidAI(NPC npc)
    {
        // Flee when no players are alive or it is day
        if ((IsTargetGone(npc) || Main.dayTime) && !isFleeing)
        {
            npc.TargetClosest();
            if (IsTargetGone(npc) || Main.dayTime)
            {
                AttackManager.CountUp = true;
                isFleeing = true;
                AttackManager.AiTimer = 0;
            }
        }

        if (isFleeing)
        {
            FleeAI();
        }
        else
        {
            phaseTracker.RunPhaseAI();
        }

        return false;
    }

    private void FleeAI()
    {
        AttackManager.CountUp = true;
        if (!IsTargetGone(Npc) && !Main.dayTime)
        {
            AttackManager.CountUp = false;
            isFleeing = false;
            AttackManager.AiTimer = 30;
            return;
        }

        if (AttackManager.AiTimer < 120)
        {
            var desiredVelocity = -Vector2.UnitY * 7.5f;
            Npc.SimpleFlyMovement(desiredVelocity, 0.05f);
            LookTowards(desiredVelocity, 0.15f);
        }
        else
        {
            Npc.active = false;
        }
    }
    
    #endregion

    #region Phase AIs
    
    private PhaseState PhaseOne => new(Phase_One, EnterPhaseOne);

    private void EnterPhaseOne()
    {
        var hover = new AttackState(() => Attack_Hover(180, 7.5f, 0.05f, 250f), 0);
        var dash = new AttackState(() => Attack_DashAtPlayer(30, 7.5f, false, 250) == DashState.Done, 45);
        var summon = new AttackState(() => Attack_SummonMinions(3), 15);

        AttackManager.SetAttackPattern([
            hover,
            dash,
            summon,
            hover,
            dash,
            dash,
        ]);
    }

    private void Phase_One()
    {
        if (Npc.GetLifePercent() <= 0.80f && AttackManager.AiTimer == 0)
        {
            phaseTracker.NextPhase();
            AttackManager.Reset();
            return;
        }

        if (AttackManager.AiTimer > 0 && !AttackManager.CountUp)
        {
            Npc.SimpleFlyMovement(Vector2.Zero, 0.25f);
            LookTowards(Main.player[Npc.target].Center, 0.05f);
            return;
        }

        AttackManager.RunAttackPattern();
    }
    
    private PhaseState PhaseTwo => new(Phase_Two, EnterPhaseTwo);
    
    private void EnterPhaseTwo()
    {
        var hover = new AttackState(() => Attack_Hover(120, 10f, 0.15f, 250f), 0);
        var telegraphedDash = new AttackState(() => Attack_TelegraphedDash(45, 15f), 15);
        var dash = new AttackState(() => Attack_DashAtPlayer(45, 15f, true, 250) == DashState.Done, 15);
        var teleport = new AttackState(Attack_TeleportBehind, 15);
        
        AttackManager.SetAttackPattern([
            hover,
            telegraphedDash,
            dash,
            hover,
            telegraphedDash,
            teleport
        ]);
    }

    private void Phase_Two()
    {
        if (Npc.GetLifePercent() <= 0.60f && AttackManager.AiTimer == 0)
        {
            phaseTracker.NextPhase();
            AttackManager.Reset();
            return;
        }

        if (AttackManager.AiTimer > 0 && !AttackManager.CountUp)
        {
            Npc.SimpleFlyMovement(Vector2.Zero, 0.35f);
            LookTowards(Main.player[Npc.target].Center, 0.05f);
            return;
        }

        AttackManager.RunAttackPattern();
    }
    
    private PhaseState PhaseThree => new(Phase_Three, EnterPhaseThree);
    
    private void EnterPhaseThree()
    {
        var hover = new AttackState(() => Attack_Hover(90, 10f, 0.15f, 350f), 0);
        var telegraphedDash = new AttackState(() => Attack_TelegraphedDash(45, 15f), 15);
        var tripleDash = new AttackState(() => Attack_TripleDash(45, 15f), 15);
        var phantomCrossDash = new AttackState(Attack_PhantomCrossDash, 15);
        var phantomPlusDash = new AttackState(Attack_PhantomCrossDash, 15);
        var teleport = new AttackState(Attack_TeleportBehind, 5);
        var summon = new AttackState(() => Attack_SummonMinions(3), 15);
        
        AttackManager.SetAttackPattern([
            hover,
            telegraphedDash,
            tripleDash,
            phantomCrossDash,
            hover,
            telegraphedDash,
            teleport,
            tripleDash,
            phantomPlusDash,
            hover,
            telegraphedDash,
            tripleDash,
            tripleDash,
            tripleDash,
            summon
        ]);
    }

    private void Phase_Three()
    {
        if (Npc.GetLifePercent() <= 0.40f && AttackManager.AiTimer == 0)
        {
            phaseTracker.NextPhase();
            AttackManager.Reset();
            return;
        }

        if (AttackManager.AiTimer > 0 && !AttackManager.CountUp)
        {
            Npc.SimpleFlyMovement(Vector2.Zero, 0.35f);
            LookTowards(Main.player[Npc.target].Center, 0.05f);
            return;
        }

        AttackManager.RunAttackPattern();
    }
    
    private PhaseState PhaseFour => new(Phase_Four, EnterPhaseFour);

    private void EnterPhaseFour()
    {
        var hover = new AttackState(() => Attack_Hover(30, 20f, 0.15f, 250f), 0);
        var telegraphedDash = new AttackState(() => Attack_TelegraphedDash(45, 20f), 0);
        var tripleDash = new AttackState(() => Attack_TripleDash(45, 20f), 0)
        {
            OnDone = () => Npc.velocity = Vector2.Zero
        };
        var phantomCrossDash = new AttackState(Attack_PhantomCrossDash, 5);
        var phantomPlusDash = new AttackState(Attack_PhantomPlusDash, 5);
        var gasterSpin = new AttackState(() => Attack_GasterSpinDash(8, 1), 30);
        var summon = new AttackState(() => Attack_SummonMinions(5), 0);
        var teleport = new AttackState(Attack_TeleportBehind, 5);
        
        AttackManager.SetAttackPattern([
            hover,
            telegraphedDash,
            teleport,
            phantomPlusDash,
            phantomCrossDash,
            phantomPlusDash,
            hover,
            telegraphedDash,
            tripleDash,
            tripleDash,
            teleport,
            summon,
            hover,
            gasterSpin
        ]);
    }

    private void Phase_Four()
    {
        if (AttackManager.AiTimer > 0 && !AttackManager.CountUp)
        {
            Npc.SimpleFlyMovement(Vector2.Zero, 0.5f);
            LookTowards(Main.player[Npc.target].Center, 0.15f);
            return;
        }

        AttackManager.RunAttackPattern();
    }

    private PhaseState PhaseTransitionOne => new(Phase_TransitionOne);
    
    private void Phase_TransitionOne()
    {
        AttackManager.CountUp = true;
        const int spinCount = 3;

        Npc.SimpleFlyMovement(Vector2.Zero, 0.5f);

        // Spin
        if (AttackManager.AiTimer < 90)
        {
            // Don't fly off into the distance
            if (AttackManager.AiTimer == 0)
            {
                Npc.localAI[0] = Npc.rotation;
            }

            // Spawn minions like in vanilla
            if (AttackManager.AiTimer % 15 == 0) NewMinion();

            // Spin :)
            var spinT = AttackManager.AiTimer / 90f;
            var spinCurve = new PiecewiseCurve()
                .Add(EasingCurves.Quadratic, EasingType.In, MathHelper.PiOver2, 0.25f)
                .Add(MoreEasingCurves.Back, EasingType.Out, MathHelper.TwoPi, 1f);
            
            Npc.rotation = MathHelper.WrapAngle(Npc.localAI[0] + spinCurve.Evaluate(spinT) * 2f);

        }
        // Transform to mouth and start shockwave
        else if (AttackManager.AiTimer == 90)
        {
            // Yoinked from vanilla code
            SoundEngine.PlaySound(SoundID.NPCHit1, Npc.Center);
            SoundEngine.PlaySound(SoundID.Roar, Npc.Center);
            for (var i = 0; i < 2; i++)
            {
                Gore.NewGore(Npc.GetSource_FromAI(), Npc.Center, Main.rand.NextVector2Square(-30, 31) * 0.2f, 8);
                Gore.NewGore(Npc.GetSource_FromAI(), Npc.Center, Main.rand.NextVector2Square(-30, 31) * 0.2f, 7);
                Gore.NewGore(Npc.GetSource_FromAI(), Npc.Center, Main.rand.NextVector2Square(-30, 31) * 0.2f, 6);
            }

            for (var i = 0; i < 20; i++)
            {
                var speed = Main.rand.NextVector2Square(-30, 31) * 0.2f;
                Dust.NewDust(Npc.Center, Npc.width, Npc.height, DustID.Blood, speed.X, speed.Y);
            }

            ScreenShakeSystem.StartShakeAtPoint(Npc.Center, 4f);

            mouthMode = true;
            Npc.damage = 80; // Way more damage now that the mouth is out
            
        }
        // Shockwave Update
        else if (AttackManager.AiTimer is >= 90 and < 180)
        {
            var shockT = (AttackManager.AiTimer - 90f) / 60f;
            EffectsManager.ShockwaveActivate(Npc.Center, 0.075f, 0.15f, Color.Transparent, shockT);
            LookTowards(Main.player[Npc.target].Center, 0.2f);
        }
        // End and cleanup
        else if (AttackManager.AiTimer == 180)
        {
            phaseTracker.NextPhase();
            AttackManager.Reset();
            return;
        }
    }

    private PhaseState PhaseTransitionTwo => new(Phase_TransitionTwo);
    
    private void Phase_TransitionTwo()
    {
        phaseTracker.NextPhase();
        AttackManager.Reset();

        // Faster Dashes
        dashAtTime = 25;
        dashTrackTime = 10;
    }

    #endregion

    #region Attacks

    private void Attack_Hover(float speed, float acceleration, float distance)
    {
        var target = Main.player[Npc.target];
        var goal = target.Center;
        goal.Y -= distance;

        Npc.SimpleFlyMovement(Npc.DirectionTo(goal) * speed, acceleration);
        LookTowards(target.Center, 0.2f);
    }

    private bool Attack_Hover(int hoverTime, float speed, float acceleration, float distance)
    {
        AttackManager.CountUp = true;

        Attack_Hover(speed, acceleration, distance);

        if (AttackManager.AiTimer <= hoverTime) return false;

        AttackManager.CountUp = false;
        return true;
    }

    // Dash Stuff

    private static int dashTrackTime = 15;
    private static int dashAtTime = 30;

    private DashState Attack_DashAtPlayer(int dashLength, float speed, bool enraged, float distance)
    {
        var options = new DashOptions
        {
            MinimumDistance = distance,
            DashSpeed = speed,
            DashLength = dashLength,
            TrackTime = dashTrackTime,
            DashAtTime = dashAtTime,
            LookOffset = MathHelper.PiOver2
        };
        
        var target = Main.player[Npc.target];
        var dashState = DashHelper.Dash(Npc, AttackManager, target.Center, options);

        if (enraged)
        {
            if (dashState == DashState.StartingDash)
            {
                var ring = new SmokeRingParticle(Npc.Center, Vector2.Zero, Npc.rotation, Color.Gray, 30);
                ring.Scale *= 2f;
                ring.Spawn();
                
                SoundEngine.PlaySound(SoundID.ForceRoarPitched, Npc.Center);
                useAfterimages = true;
            }
            if (dashState == DashState.Done)
            {
                useAfterimages = false;
            }
        }

        return dashState;
    }

    private bool Attack_TelegraphedDash(int dashLength, float speed)
    {
        AttackManager.CountUp = true;

        // Normal dash movement
        var dashState = Attack_DashAtPlayer(dashLength, speed, true, 300);

        if (dashState == DashState.Repositioning) return false;

        if (dashState == DashState.Done) AttackManager.CountUp = false;
        if (Main.netMode == NetmodeID.MultiplayerClient) return dashState == DashState.Done;

        // Create Telegraph
        if (AttackManager.AiTimer == 0)
        {
            var line = NewDashLine(Npc.Center, MathHelper.PiOver2, dashAtTime);
        }

        return dashState == DashState.Done;
    }

    private bool Attack_TripleDash(int dashLength, float speed)
    {
        AttackManager.CountUp = true;

        // Dash Normally for self
        var dashState = Attack_DashAtPlayer(dashLength, speed, true, 500);

        if (dashState == DashState.Repositioning) return false;

        if (dashState == DashState.Done) AttackManager.CountUp = false;
        if (Main.netMode == NetmodeID.MultiplayerClient) return dashState == DashState.Done;

        const float dashOffset = MathHelper.Pi / 5f;

        // Triple Telegraphs
        for (var i = 0; i < 3; i++)
        {
            // -1 to 1 for offsetting the line direction
            var offsetCoef = i - 1;

            // Create Telegraph
            if (AttackManager.AiTimer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Center
                var line = NewDashLine(Npc.Center, dashOffset * offsetCoef + MathHelper.PiOver2, dashAtTime);
            }
        }

        // The phantom eye stuff is owned by server
        if (Main.netMode == NetmodeID.MultiplayerClient) return dashState == DashState.Done;

        // Phantom Dashes
        if (AttackManager.AiTimer == dashAtTime)
        {
            // + dashOffset
            var vel1 = (Npc.rotation + MathHelper.PiOver2 + dashOffset).ToRotationVector2() * speed;
            var eye1 = NewPhantomEoC(Npc.Center, vel1);
            eye1.timeLeft = dashLength;

            // - dashOffset
            var vel2 = (Npc.rotation + MathHelper.PiOver2 - dashOffset).ToRotationVector2() * speed;
            var eye2 = NewPhantomEoC(Npc.Center, vel2);
            eye2.timeLeft = dashLength;
        }

        return dashState == DashState.Done;
    }

    private bool Attack_PhantomCrossDash()
    {
        AttackManager.CountUp = true;

        Attack_Hover(10f, 0.05f, 400f);

        const int dashLength = 45;
        const float dashSpeed = 40f;
        var indicateTime = (int) (dashAtTime);

        var isDone = AttackManager.AiTimer >= dashLength + indicateTime;

        if (isDone) AttackManager.CountUp = false;

        var targetPos = Main.player[Npc.target].Center;

        var offsetFromPlayer0 = new Vector2(710, -710);
        var vel0 = (targetPos + offsetFromPlayer0).DirectionTo(targetPos) * dashSpeed;
        var pos0 = targetPos + offsetFromPlayer0;
        var vel2 = (targetPos - offsetFromPlayer0).DirectionTo(targetPos) * dashSpeed;
        var pos2 = targetPos - offsetFromPlayer0;

        var offsetFromPlayer1 = new Vector2(-710, -710);
        var vel1 = (targetPos + offsetFromPlayer1).DirectionTo(targetPos) * dashSpeed;
        var pos1 = targetPos + offsetFromPlayer1;
        var vel3 = (targetPos - offsetFromPlayer1).DirectionTo(targetPos) * dashSpeed;
        var pos3 = targetPos - offsetFromPlayer1;

        if (AttackManager.AiTimer == 0)
        {
            Main.TeleportEffect(new Rectangle((int) pos0.X, (int) pos0.Y, Npc.width, Npc.height),
                TeleportationStyleID.RodOfDiscord);
            Main.TeleportEffect(new Rectangle((int) pos1.X, (int) pos1.Y, Npc.width, Npc.height),
                TeleportationStyleID.RodOfDiscord);
        }

        if (AttackManager.AiTimer == indicateTime)
        {
            SoundEngine.PlaySound(SoundID.ForceRoarPitched);
        }

        if (Main.netMode == NetmodeID.MultiplayerClient) return isDone;

        // Start the dash stuff
        if (AttackManager.AiTimer == 0)
        {
            // Spawn Right
            var rightEye = NewPhantomEoC(pos0, vel0, indicateTime);
            rightEye.timeLeft = indicateTime + dashLength;
            var rightLine = NewPhantomDashLine(pos0, vel0.ToRotation(), indicateTime, false);
            var rightEye1 = NewPhantomEoC(pos2, vel2, indicateTime);
            rightEye1.timeLeft = indicateTime + dashLength;

            // Spawn Left
            var leftEye = NewPhantomEoC(pos1, vel1, indicateTime);
            leftEye.timeLeft = indicateTime + dashLength;
            var leftLine = NewPhantomDashLine(pos1, vel1.ToRotation(), indicateTime, false);
            var leftEye1 = NewPhantomEoC(pos3, vel3, indicateTime);
            leftEye1.timeLeft = indicateTime + dashLength;
        }

        return isDone;
    }

    private bool Attack_PhantomPlusDash()
    {
        AttackManager.CountUp = true;

        const int dashLength = 45;
        const float dashSpeed = 40f;
        var indicateTime = (int) (dashAtTime);

        Attack_Hover(10f, 0.05f, 400f);
        var isDone = AttackManager.AiTimer >= dashLength + indicateTime;

        if (isDone) AttackManager.CountUp = false;

        var targetPos = Main.player[Npc.target].Center;

        var offsetFromPlayer0 = new Vector2(1000, 0);
        var vel0 = (targetPos + offsetFromPlayer0).DirectionTo(targetPos) * dashSpeed;
        var pos0 = targetPos + offsetFromPlayer0;
        var vel2 = (targetPos - offsetFromPlayer0).DirectionTo(targetPos) * dashSpeed;
        var pos2 = targetPos - offsetFromPlayer0;
        var offsetFromPlayer1 = new Vector2(0, -1000);
        var vel1 = (targetPos + offsetFromPlayer1).DirectionTo(targetPos) * dashSpeed;
        var pos1 = targetPos + offsetFromPlayer1;
        var vel3 = (targetPos - offsetFromPlayer1).DirectionTo(targetPos) * dashSpeed;
        var pos3 = targetPos - offsetFromPlayer1;

        if (AttackManager.AiTimer == 0)
        {
            Main.TeleportEffect(new Rectangle((int) pos0.X, (int) pos0.Y, Npc.width, Npc.height),
                TeleportationStyleID.RodOfDiscord);
            Main.TeleportEffect(new Rectangle((int) pos1.X, (int) pos1.Y, Npc.width, Npc.height),
                TeleportationStyleID.RodOfDiscord);
        }

        if (AttackManager.AiTimer == indicateTime)
        {
            SoundEngine.PlaySound(SoundID.ForceRoarPitched);
        }

        if (Main.netMode == NetmodeID.MultiplayerClient) return isDone;

        // Start the dash stuff
        if (AttackManager.AiTimer == 0)
        {
            // Spawn Right
            var rightEye = NewPhantomEoC(pos0, vel0, indicateTime);
            rightEye.timeLeft = indicateTime + dashLength;
            var rightLine = NewPhantomDashLine(pos0, vel0.ToRotation(), indicateTime, false);
            var rightEye1 = NewPhantomEoC(pos2, vel2, indicateTime);
            rightEye1.timeLeft = indicateTime + dashLength;

            // Spawn Left
            var leftEye = NewPhantomEoC(pos1, vel1, indicateTime);
            leftEye.timeLeft = indicateTime + dashLength;
            var leftLine = NewPhantomDashLine(pos1, vel1.ToRotation(), indicateTime, false);
            var leftEye1 = NewPhantomEoC(pos3, vel3, indicateTime);
            leftEye1.timeLeft = indicateTime + dashLength;
        }

        return isDone;
    }

    private bool Attack_GasterSpinDash(int delay, int spins)
    {
        AttackManager.CountUp = true;

        const int dashLength = 45;
        const float dashSpeed = 40f;
        const int dashesPerSpin = 16;
        ref var dashes = ref Npc.localAI[0];
        ref var originX = ref Npc.localAI[1];
        ref var originY = ref Npc.localAI[2];
        ref var startAngle = ref Npc.localAI[3];
        ref var spinDirection = ref ExtraLocalAI[0];

        Attack_Hover(10f, 0.05f, 400f);
        var isDone = dashes > dashesPerSpin * spins;

        if (isDone)
        {
            AttackManager.CountUp = false;
            dashes = 0;
        }

        if (AttackManager.AiTimer == 0)
        {
            var origin = Main.player[Npc.target].Center;
            originX = origin.X;
            originY = origin.Y;

            startAngle = Main.player[Npc.target].velocity.ToRotation() + MathHelper.PiOver2;
            spinDirection = -1 * Main.player[Npc.target].velocity.X.NonZeroSign() * Main.player[Npc.target].velocity.Y.NonZeroSign();
        }

        var targetPos = new Vector2(originX, originY);
        var rot = (dashes * (MathHelper.Pi / dashesPerSpin) * spinDirection) + startAngle;
        var offsetFromPlayer = new Vector2(1000, 0).RotatedBy(rot);
        var pos = targetPos + offsetFromPlayer;
        var pos2 = targetPos - offsetFromPlayer;
        var vel = pos.DirectionTo(targetPos) * dashSpeed;

        if (AttackManager.AiTimer % delay == 0)
        {
            if (dashes != 0)
            {
                SoundEngine.PlaySound(SoundID.ForceRoarPitched);
            }

            dashes++;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient) return isDone;

        // Start the dash stuff
        if (AttackManager.AiTimer % delay == 0)
        {
            // Spawn
            var eye = NewPhantomEoC(pos, vel, dashAtTime);
            var eye2 = NewPhantomEoC(pos2, -vel, dashAtTime);
            eye.timeLeft = dashAtTime + dashLength;
            var line = NewPhantomDashLine(pos, vel.ToRotation(), (int) (dashAtTime + dashLength / 2f), false);
        }

        return isDone;
    }

    // Other Random Garbage

    private bool Attack_SummonMinions(int minionCount)
    {
        AttackManager.CountUp = true;
        const int spawnDelay = 15;
        ref var startAngle = ref Npc.localAI[0];

        if (AttackManager.AiTimer == 0)
        {
            startAngle = Npc.rotation;
        }

        // Speen 
        Npc.velocity = Vector2.Zero;
        var spinTime = spawnDelay * minionCount;
        var spinT = Utils.GetLerpValue(0f, spinTime, AttackManager.AiTimer);
        var angularAccel = MathHelper.Pi * 0.1f;
        var spinCurve = new PiecewiseCurve()
            .Add(EasingCurves.Quadratic, EasingType.In, MathHelper.PiOver2, 0.25f)
            .Add(EasingCurves.Quadratic, EasingType.Out, MathHelper.TwoPi, 1f);
        
        Npc.rotation = MathHelper.WrapAngle(startAngle + spinCurve.Evaluate(spinT));

        if (AttackManager.AiTimer >= spinTime)
        {
            AttackManager.CountUp = false;
            return true;
        }

        if (AttackManager.AiTimer % spawnDelay == 0) NewMinion();
        return false;
    }

    private bool Attack_TeleportBehind()
    {
        var target = Main.player[Npc.target].Center;
        var targetVel = Main.player[Npc.target].velocity;
        var destination = target - (Npc.Center - target); // Opposite side of player

        // Set a limit to how close the eye can be after teleporting
        if (destination.Distance(target + targetVel * 15) <= 250)
        {
            destination = -target.DirectionTo(Npc.Center) * 250;
            destination += target;
        }
        Teleport(destination, 20f);

        return true;
    }

    private void Teleport(Vector2 destination, float recoil)
    {
        var awayDir = Npc.DirectionTo(destination);
        var startPos = Npc.Center;

        Npc.rotation = awayDir.ToRotation() - MathHelper.PiOver2;
        
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            NpcAfterimageTrail.Create(Npc.GetSource_FromAI(), Npc.Center, destination, Npc.whoAmI);
        }
        
        Npc.Center = destination;
        Npc.velocity = awayDir * recoil;
        
        SoundEngine.PlaySound(SoundID.DD2_JavelinThrowersAttack, Npc.Center);
    }

    private void NewMinion()
    {
        SoundEngine.PlaySound(SoundID.Item95, Npc.Center);

        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        var summon = NPC.NewNPC(Npc.GetSource_FromAI(), (int) Npc.Center.X, (int) Npc.Center.Y,
            NPCID.ServantofCthulhu);
        Main.npc[summon].velocity = Main.rand.NextVector2Unit() * 10;
    }

    private Projectile NewDashLine(Vector2 position, float offset, int lifetime, bool anchorToBoss = true)
    {
        var ai1 = anchorToBoss ? Npc.whoAmI : -1;
        return BaseLineProjectile.Create<EyeDashLine>(Npc.GetSource_FromAI(), position, offset, lifetime, ai1);
    }
    
    private Projectile NewPhantomDashLine(Vector2 position, float offset, int lifetime, bool anchorToBoss = true)
    {
        var ai1 = anchorToBoss ? Npc.whoAmI : -1;
        return BaseLineProjectile.Create<EyePhantomDashLine>(Npc.GetSource_FromAI(), position, offset, lifetime, ai1);
    }

    private Projectile NewPhantomEoC(Vector2 position, Vector2 dashVelocity, int dashDelay = 0)
    {
        return ProjHelper.NewUnscaledProjectile(Npc.GetSource_FromAI(), position,
            dashVelocity, ModContent.ProjectileType<PhantomEoC>(), Npc.damage, 5, ai0: dashDelay);
    }

    #endregion

    #region Drawing

    public override bool AcidicDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        var effects = SpriteEffects.None;
        if (npc.spriteDirection == 1)
            effects = SpriteEffects.FlipHorizontally;

        // Draw it :)
        var drawPos = npc.Center - Main.screenPosition;
        var eyeTexture = TextureAssets.Npc[npc.type].Value;
        var eyeOrigin = eyeTexture.Size() / new Vector2(1f, Main.npcFrameCount[npc.type]) * 0.5f;

        // Afterimages
        if (useAfterimages)
            for (var i = 1; i < npc.oldPos.Length; i++)
            {
                // All of this is heavily simplified from decompiled vanilla
                var fade = 0.5f * (10 - i) / 20f;
                var afterImageColor = Color.Multiply(drawColor, fade);

                var pos = npc.oldPos[i] + new Vector2(npc.width, npc.height) / 2f - Main.screenPosition;
                spriteBatch.Draw(eyeTexture, pos, npc.frame, afterImageColor, npc.rotation, eyeOrigin, npc.scale,
                    effects,
                    0f);
            }

        spriteBatch.Draw(
            eyeTexture, drawPos,
            npc.frame, npc.GetAlpha(drawColor),
            npc.rotation, eyeOrigin, npc.scale,
            effects, 0f);

        return false;
    }

    // Modified vanilla animating
    public override void FindFrame(NPC npc, int frameHeight)
    {
        if (!ShouldOverride()) base.FindFrame(npc, frameHeight);
        
        if (npc.frameCounter < 7.0)
        {
            npc.frame.Y = 0;
        }
        else if (npc.frameCounter < 14.0)
        {
            npc.frame.Y = frameHeight;
        }
        else if (npc.frameCounter < 21.0)
        {
            npc.frame.Y = frameHeight * 2;
        }
        else
        {
            npc.frameCounter = 0.0;
            npc.frame.Y = 0;
        }

        // Show Mouth by offsetting the frame
        if (mouthMode)
        {
            npc.frame.Y += frameHeight * 3;
        }
    }

    public override void BossHeadSlot(NPC npc, ref int index)
    {
        if (!ShouldOverride()) return;
        index = mouthMode ? 1 : 0;
    }

    #endregion

    public override void SendAcidAI(BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        phaseTracker.Serialize(binaryWriter);

        bitWriter.WriteBit(useAfterimages);
        bitWriter.WriteBit(isFleeing);
        bitWriter.WriteBit(mouthMode);
    }

    public override void ReceiveAcidAI(BitReader bitReader, BinaryReader binaryReader)
    {
        phaseTracker.Deserialize(binaryReader);

        useAfterimages = bitReader.ReadBit();
        isFleeing = bitReader.ReadBit();
        mouthMode = bitReader.ReadBit();
    }

    public override void LookTowards(Vector2 target, float power)
    {
        Npc.rotation = Npc.rotation.AngleLerp(Npc.AngleTo(target) - MathHelper.PiOver2, power);
    }
}