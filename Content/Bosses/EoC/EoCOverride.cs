using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AcidicBosses.Common.Effects;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Content.Bosses.EoC;

public class EoCOverride : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.EyeofCthulhu;

    private enum PhaseState
    {
        One,
        TransitionOne,
        Two,
        Three,
        TransitionTwo,
        Four
    }

    private enum Attack
    {
        Hover,
        Dash,
        TelegraphedDash,
        TripleDash,
        PhantomCrossDash,
        PhantomPlusDash,
        SummonMinions,
        Teleport,
    }

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

    private static Attack[] phaseOneAP =
    {
        Attack.Hover,
        Attack.Dash,
        Attack.SummonMinions,
        Attack.Hover,
        Attack.Dash,
        Attack.Dash
    };

    private static Attack[] phaseTwoAP =
    {
        Attack.Hover,
        Attack.TelegraphedDash,
        Attack.Dash,
        Attack.Hover,
        Attack.TelegraphedDash,
        Attack.Teleport,
    };

    private static Attack[] phaseThreeAP =
    {
        // Triple dash should be after some other dash to provide room to dodge between the eyes
        Attack.Hover,
        Attack.TelegraphedDash,
        Attack.TripleDash,
        Attack.Hover,
        Attack.TelegraphedDash,
        Attack.Teleport,
        Attack.TripleDash,
        Attack.Hover,
        Attack.TelegraphedDash,
        Attack.TripleDash,
        Attack.TripleDash,
        Attack.TripleDash
    };

    private static Attack[] phaseFourAP =
    {
        // Triple dash should be after some other dash to provide room to dodge between the eyes
        Attack.Hover,
        Attack.TelegraphedDash,
        Attack.Teleport,
        Attack.PhantomPlusDash,
        Attack.PhantomCrossDash,
        Attack.PhantomPlusDash,
        Attack.Hover,
        Attack.TelegraphedDash,
        Attack.TripleDash,
        Attack.TripleDash,
        Attack.Teleport,
        Attack.SummonMinions,
    };

    private int CurrentAttackIndex
    {
        get => (int) Npc.ai[2];
        set => Npc.ai[2] = value;
    }

    private Attack[] CurrentAttackPattern => CurrentPhase switch
    {
        PhaseState.One => phaseOneAP,
        PhaseState.Two => phaseTwoAP,
        PhaseState.Three => phaseThreeAP,
        PhaseState.Four => phaseFourAP,
        _ => throw new UsageException(
            $"EoC is in the PhaseState {CurrentPhase} and does not have an attack pattern")
    };

    private Attack CurrentAttack => CurrentAttackPattern[CurrentAttackIndex];

    private bool useAfterimages = false;

    private bool countUpTimer = false;

    private bool isFleeing = false;

    private bool mouthMode = false;

    public override void OnFirstFrame(NPC npc)
    {
        CurrentPhase = PhaseState.One;
        AiTimer = 0;
    }

    public override bool AcidAI(NPC npc)
    {
        if (!countUpTimer) AiTimer = Math.Max(AiTimer - 1, 0);

        // Flee when no players are alive or it is day
        if ((IsTargetGone(npc) || Main.dayTime) && !isFleeing)
        {
            npc.TargetClosest();
            if (IsTargetGone(npc) || Main.dayTime)
            {
                countUpTimer = true;
                isFleeing = true;
                AiTimer = 0;
            }
        }

        if (isFleeing)
        {
            FleeAI();
        }
        else
            switch (CurrentPhase)
            {
                case PhaseState.One:
                    PhaseOneAI();
                    break;
                case PhaseState.TransitionOne:
                    TransitionOneAI();
                    break;
                case PhaseState.Two:
                    PhaseTwoAI();
                    break;
                case PhaseState.Three:
                    PhaseThreeAI();
                    break;
                case PhaseState.TransitionTwo:
                    TransitionTwoAI();
                    break;
                case PhaseState.Four:
                    PhaseFourAI();
                    break;
            }

        if (countUpTimer) AiTimer++;
        return false;
    }

    #region AI

    private void PhaseOneAI()
    {
        if (Npc.GetLifePercent() <= 0.80f && AiTimer == 0)
        {
            CurrentPhase = PhaseState.TransitionOne;
            AiTimer = 0;
            return;
        }

        if (AiTimer > 0 && !countUpTimer)
        {
            Npc.SimpleFlyMovement(Vector2.Zero, 0.25f);
            LookTowards(Main.player[Npc.target].Center, 0.05f);
            return;
        }

        var isDone = true;
        switch (CurrentAttack)
        {
            case Attack.Hover:
                Hover(out isDone, 180, 7.5f, 0.05f, 250f);
                if (isDone) AiTimer = 0;
                break;
            case Attack.Dash:
                DashAtPlayer(out isDone, 30, 7.5f, false);
                if (isDone) AiTimer = 45;
                break;
            case Attack.SummonMinions:
                SummonMinions(out isDone, 3);
                if (isDone) AiTimer = 15;
                break;
        }

        if (isDone) NextAttack();
    }

    private void PhaseTwoAI()
    {
        if (Npc.GetLifePercent() <= 0.60f && AiTimer == 0)
        {
            CurrentPhase = PhaseState.Three;
            AiTimer = 0;
            return;
        }

        if (AiTimer > 0 && !countUpTimer)
        {
            Npc.SimpleFlyMovement(Vector2.Zero, 0.35f);
            LookTowards(Main.player[Npc.target].Center, 0.05f);
            return;
        }

        var isDone = true;
        switch (CurrentAttack)
        {
            case Attack.Hover:
                Hover(out isDone, 120, 10f, 0.15f, 250f);
                if (isDone) AiTimer = 0;
                break;
            case Attack.TelegraphedDash:
                TelegraphedDash(out isDone, 45, 15f);
                if (isDone) AiTimer = 15;
                break;
            case Attack.Dash:
                DashAtPlayer(out isDone, 45, 15f, true);
                if (isDone) AiTimer = 15;
                break;
            case Attack.Teleport:
                TeleportBehind();
                AiTimer = 15;
                break;
        }

        if (isDone) NextAttack();
    }

    private void PhaseThreeAI()
    {
        if (Npc.GetLifePercent() <= 0.25f && AiTimer == 0)
        {
            CurrentPhase = PhaseState.TransitionTwo;
            AiTimer = 0;
            return;
        }

        if (AiTimer > 0 && !countUpTimer)
        {
            Npc.SimpleFlyMovement(Vector2.Zero, 0.35f);
            LookTowards(Main.player[Npc.target].Center, 0.05f);
            return;
        }

        bool isDone = true;
        switch (CurrentAttack)
        {
            case Attack.Hover:
                Hover(out isDone, 90, 10f, 0.15f, 350f);
                if (isDone) AiTimer = 0;
                break;
            case Attack.Dash:
                DashAtPlayer(out isDone, 45, 15f, true);
                if (isDone) AiTimer = 15;
                break;
            case Attack.TelegraphedDash:
                TelegraphedDash(out isDone, 45, 15f);
                if (isDone) AiTimer = 15;
                break;
            case Attack.TripleDash:
                TripleDash(out isDone, 45, 15f);
                if (isDone) AiTimer = 15;
                break;
            case Attack.Teleport:
                TeleportBehind();
                AiTimer = 5;
                break;
        }

        if (isDone) NextAttack();
    }

    private void PhaseFourAI()
    {
        if (AiTimer > 0 && !countUpTimer)
        {
            Npc.SimpleFlyMovement(Vector2.Zero, 0.5f);
            LookTowards(Main.player[Npc.target].Center, 0.15f);
            return;
        }

        var isDone = true;
        switch (CurrentAttack)
        {
            case Attack.Hover:
                Hover(out isDone, 60, 15f, 0.15f, 350f);
                if (isDone) AiTimer = 0;
                break;
            case Attack.TelegraphedDash:
                TelegraphedDash(out isDone, 45, 20f);
                if (isDone)
                {
                    AiTimer = 0;
                    Npc.velocity = Vector2.Zero;
                }

                break;
            case Attack.TripleDash:
                TripleDash(out isDone, 45, 20f);
                if (isDone)
                {
                    AiTimer = 0;
                    Npc.velocity = Vector2.Zero;
                }

                break;
            case Attack.PhantomCrossDash:
                PhantomCrossDash(out isDone);
                if (isDone) AiTimer = 5;
                break;
            case Attack.PhantomPlusDash:
                PhantomPlusDash(out isDone);
                if (isDone) AiTimer = 5;
                break;
            case Attack.SummonMinions:
                SummonMinions(out isDone, 5);
                if (isDone) AiTimer = 0;
                break;
            case Attack.Teleport:
                TeleportBehind();
                AiTimer = 5;
                break;
        }

        if (isDone) NextAttack();
    }

    private void TransitionOneAI()
    {
        countUpTimer = true;
        const int spinCount = 3;
        
        Npc.SimpleFlyMovement(Vector2.Zero, 0.5f);

        // Spin
        if (AiTimer < 90)
        {
            // Don't fly off into the distance
            if (AiTimer == 0)
            {
                Npc.velocity = Vector2.Zero;
                Npc.localAI[0] = Npc.rotation;
            }

            // Spawn minions like in vanilla
            if (AiTimer % 15 == 0) SpawnMinion();

            // Spin :)
            var spinT = AiTimer / 90f;
            var spinOffset = spinCount * MathHelper.TwoPi * EasingHelper.QuadInOut(spinT);
            Npc.rotation = MathHelper.WrapAngle(Npc.localAI[0] + spinOffset);
        }
        // Transform to mouth and start shockwave
        else if (AiTimer == 90)
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

            mouthMode = true;
            Npc.damage = 80; // Way more damage now that the mouth is out
            EffectsManager.ShockwaveActive(Npc.Center, 0.15f, 0.25f, Color.Red);
        }
        // Shockwave Update
        else if (AiTimer is > 90 and < 180)
        {
            var shockT = (AiTimer - 90f) / 60f;
            EffectsManager.ShockwaveProgress(shockT);
            LookTowards(Main.player[Npc.target].Center, 0.2f);
        }
        // End and cleanup
        else if (AiTimer == 180)
        {
            countUpTimer = false;
            AiTimer = 0;
            CurrentPhase = PhaseState.Two;
            CurrentAttackIndex = 0;
            EffectsManager.ShockwaveKill();
            return;
        }
    }

    private void TransitionTwoAI()
    {
        countUpTimer = true;
        
        if (AiTimer == 0)
        {
            EffectsManager.BossRageActivate(Color.MistyRose);
            EffectsManager.ShockwaveActive(Npc.Center, 0.15f, 0.25f, Color.Red);
            SoundEngine.PlaySound(SoundID.Roar, Npc.Center);
            
            // Don't fly off into the distance
            Npc.velocity = Vector2.Zero;
            Npc.localAI[0] = Npc.rotation;
        }
        
        // Spin
        if (AiTimer < 30)
        {
            // Spin :)
            var spinT = AiTimer / 30f;
            var spinOffset = MathHelper.TwoPi * EasingHelper.QuadInOut(spinT);
            Npc.rotation = MathHelper.WrapAngle(Npc.localAI[0] + spinOffset);
        }

        if (AiTimer <= 60)
        {
            var shockT = AiTimer / 60;
            EffectsManager.ShockwaveProgress(shockT);
        }

        if (AiTimer > 60)
        {
            EffectsManager.ShockwaveKill();
            CurrentPhase = PhaseState.Four;
            AiTimer = 0;
            countUpTimer = false;
            CurrentAttackIndex = 0;

            // Faster Dashes
            dashAtTime = 20;
            dashTrackTime = 10;
        }
    }

    private void FleeAI()
    {
        countUpTimer = true;
        if (!IsTargetGone(Npc) && !Main.dayTime)
        {
            countUpTimer = false;
            isFleeing = false;
            AiTimer = 30;
            return;
        }

        if (AiTimer < 120)
        {
            var desiredVelocity = -Vector2.UnitY * 7.5f;
            Npc.SimpleFlyMovement(desiredVelocity, 0.05f);
            LookTowards(desiredVelocity, 0.15f);
        }
        else
        {
            Npc.active = false;
            EffectsManager.BossRageKill();
            EffectsManager.ShockwaveKill();
        }
    }

    #endregion

    #region Attacks

    private void Hover(out bool isDone, int hoverTime, float speed, float acceleration, float distance)
    {
        countUpTimer = true;

        var target = Main.player[Npc.target];
        var goal = target.Center;
        goal.Y -= distance;

        Npc.SimpleFlyMovement(Npc.DirectionTo(goal) * speed, acceleration);
        LookTowards(target.Center, 0.2f);

        isDone = AiTimer > hoverTime;

        if (!isDone) return;
        countUpTimer = false;
    }

    // Dash Stuff

    private static int dashTrackTime = 15;
    private static int dashAtTime = 30;

    private void DashAtPlayer(out bool isDone, int dashLength, float speed, bool enraged)
    {
        countUpTimer = true;
        var target = Main.player[Npc.target].Center;

        if (AiTimer < dashTrackTime)
        {
            Npc.SimpleFlyMovement(Vector2.Zero, 0.75f);
            LookTowards(target, 0.25f);
        }
        else if (AiTimer == dashAtTime)
        {
            Npc.velocity = (Npc.rotation + MathHelper.PiOver2).ToRotationVector2() * speed;
            if (enraged)
            {
                SoundEngine.PlaySound(SoundID.ForceRoarPitched, Npc.Center);
                useAfterimages = true;
            }
        }
        else if (AiTimer >= dashAtTime + dashLength)
        {
            isDone = true;
            countUpTimer = false;
            useAfterimages = false;
            return;
        }

        isDone = false;
    }

    private void TelegraphedDash(out bool isDone, int dashLength, float speed)
    {
        countUpTimer = true;

        // Normal dash movement
        DashAtPlayer(out isDone, dashLength, speed, true);

        if (isDone) countUpTimer = false;
        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        // Create Telegraph
        if (AiTimer == 0)
        {
            var line = NewDashLine(Npc.Center, MathHelper.PiOver2);
            line.timeLeft = dashAtTime;
        }
    }

    private void TripleDash(out bool isDone, int dashLength, float speed)
    {
        countUpTimer = true;

        // Dash Normally for self
        DashAtPlayer(out isDone, dashLength, speed, true);
        
        if (isDone) countUpTimer = false;
        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        const float dashOffset = MathHelper.Pi / 6f;

        // Triple Telegraphs
        for (var i = 0; i < 3; i++)
        {
            // -1 to 1 for offsetting the line direction
            var offsetCoef = i - 1;

            // Create Telegraph
            if (AiTimer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Center
                var line = NewDashLine(Npc.Center, dashOffset * offsetCoef + MathHelper.PiOver2);
                line.timeLeft = (int) (dashAtTime * 1.5f);
            }
        }
        
        // The phantom eye stuff is owned by server
        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        // Phantom Dashes
        if (AiTimer == dashAtTime)
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
    }

    private void PhantomCrossDash(out bool isDone)
    {
        countUpTimer = true;

        Hover(out _, int.MaxValue, 10f, 0.05f, 400f);

        const int dashLength = 45;
        const float dashSpeed = 40f;

        isDone = AiTimer >= dashLength + dashAtTime;

        if (isDone) countUpTimer = false;

        var targetPos = Main.player[Npc.target].Center;

        var offsetFromPlayer0 = new Vector2(800, -800);
        var vel0 = (targetPos + offsetFromPlayer0).DirectionTo(targetPos) * dashSpeed;
        var pos0 = targetPos + offsetFromPlayer0;

        var offsetFromPlayer1 = new Vector2(-800, -800);
        var vel1 = (targetPos + offsetFromPlayer1).DirectionTo(targetPos) * dashSpeed;
        var pos1 = targetPos + offsetFromPlayer1;

        if (AiTimer == 0)
        {
            Main.TeleportEffect(new Rectangle((int) pos0.X, (int) pos0.Y, Npc.width, Npc.height),
                TeleportationStyleID.RodOfDiscord);
            Main.TeleportEffect(new Rectangle((int) pos1.X, (int) pos1.Y, Npc.width, Npc.height),
                TeleportationStyleID.RodOfDiscord);
        }

        if (AiTimer == dashAtTime)
        {
            SoundEngine.PlaySound(SoundID.ForceRoarPitched);
        }

        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        // Start the dash stuff
        if (AiTimer == 0)
        {
            // Spawn Right
            var rightEye = NewPhantomEoC(pos0, vel0, dashAtTime);
            rightEye.timeLeft = dashAtTime + dashLength;
            var rightLine = NewDashLine(pos0, vel0.ToRotation(), false);
            rightLine.timeLeft = dashAtTime;

            // Spawn Left
            var leftEye = NewPhantomEoC(pos1, vel1, dashAtTime);
            leftEye.timeLeft = dashAtTime + dashLength;
            var leftLine = NewDashLine(pos1, vel1.ToRotation(), false);
            leftLine.timeLeft = dashAtTime;
        }
    }

    private void PhantomPlusDash(out bool isDone)
    {
        countUpTimer = true;

        const int dashLength = 45;
        const float dashSpeed = 40f;

        Hover(out _, dashLength + dashAtTime, 10f, 0.05f, 400f);
        isDone = AiTimer >= dashLength + dashAtTime;

        if (isDone) countUpTimer = false;

        var targetPos = Main.player[Npc.target].Center;

        var offsetFromPlayer0 = new Vector2(1000, 0);
        var vel0 = (targetPos + offsetFromPlayer0).DirectionTo(targetPos) * dashSpeed;
        var pos0 = targetPos + offsetFromPlayer0;
        var offsetFromPlayer1 = new Vector2(0, -1000);
        var vel1 = (targetPos + offsetFromPlayer1).DirectionTo(targetPos) * dashSpeed;
        var pos1 = targetPos + offsetFromPlayer1;

        if (AiTimer == 0)
        {
            Main.TeleportEffect(new Rectangle((int) pos0.X, (int) pos0.Y, Npc.width, Npc.height),
                TeleportationStyleID.RodOfDiscord);
            Main.TeleportEffect(new Rectangle((int) pos1.X, (int) pos1.Y, Npc.width, Npc.height),
                TeleportationStyleID.RodOfDiscord);
        }

        if (AiTimer == dashAtTime)
        {
            SoundEngine.PlaySound(SoundID.ForceRoarPitched);
        }

        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        // Start the dash stuff
        if (AiTimer == 0)
        {
            // Spawn Right
            var rightEye = NewPhantomEoC(pos0, vel0, dashAtTime);
            rightEye.timeLeft = dashAtTime + dashLength;
            var rightLine = NewDashLine(pos0, vel0.ToRotation(), false);
            rightLine.timeLeft = dashAtTime;

            // Spawn Left
            var leftEye = NewPhantomEoC(pos1, vel1, dashAtTime);
            leftEye.timeLeft = dashAtTime + dashLength;
            var leftLine = NewDashLine(pos1, vel1.ToRotation(), false);
            leftLine.timeLeft = dashAtTime;
        }
    }

    // Other Random Garbage

    private void SummonMinions(out bool isDone, int minionCount)
    {
        countUpTimer = true;
        const int spawnDelay = 15;

        // Speen 
        if (AiTimer == 0) Npc.localAI[0] = Npc.rotation;
        var spinTime = spawnDelay * minionCount;
        var spinT = (float) AiTimer / (spinTime - 1);
        var dAngle = MathHelper.TwoPi * EasingHelper.QuadOut(spinT);
        Npc.rotation = Npc.localAI[0] + dAngle;

        if (AiTimer >= spinTime)
        {
            isDone = true;
            countUpTimer = false;
            return;
        }

        if (AiTimer % spawnDelay == 0) SpawnMinion();
        isDone = false;
    }

    private void SpawnMinion()
    {
        SoundEngine.PlaySound(SoundID.Item95, Npc.Center);

        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        var summon = NPC.NewNPC(Npc.GetSource_FromAI(), (int) Npc.Center.X, (int) Npc.Center.Y,
            NPCID.ServantofCthulhu);
        Main.npc[summon].velocity = Main.rand.NextVector2Unit() * 10;
    }

    private void TeleportBehind()
    {
        var target = Main.player[Npc.target].Center;
        var destination = target - (Npc.Center - target); // Opposite side of player
        Teleport(destination);
    }

    private void Teleport(Vector2 destination)
    {
        Npc.Teleport(destination, TeleportationStyleID.RodOfDiscord);
        Npc.velocity = Vector2.Zero;
        Npc.rotation += MathHelper.Pi;
    }

    private Projectile NewDashLine(Vector2 position, float offset, bool anchorToBoss = true)
    {
        var ai1 = anchorToBoss ? Npc.whoAmI : 0;
        return Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), position, Vector2.Zero,
            ModContent.ProjectileType<EyeDashLine>(), 0, 0, ai0: offset, ai1: ai1);
    }

    private Projectile NewPhantomEoC(Vector2 position, Vector2 dashVelocity, int dashDelay = 0)
    {
        // Have to counteract the projectile scaling because npc damage is already scaled
        var damage = (int) (Npc.damage / Main.GameModeInfo.EnemyDamageMultiplier / 2);
        return Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), position,
            dashVelocity, ModContent.ProjectileType<PhantomEoC>(), damage, 5, ai0: dashDelay);
    }

    #endregion

    #region Drawing

    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
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
            for (var i = 1; i < npc.oldPos.Length; i ++)
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
        npc.frameCounter += 1.0;
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
        index = mouthMode ? 1 : 0;
    }

    #endregion

    public override void SendAcidAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        bitWriter.WriteBit(useAfterimages);
        bitWriter.WriteBit(countUpTimer);
        bitWriter.WriteBit(isFleeing);
        bitWriter.WriteBit(mouthMode);
    }

    public override void ReceiveAcidAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        useAfterimages = bitReader.ReadBit();
        countUpTimer = bitReader.ReadBit();
        isFleeing = bitReader.ReadBit();
        mouthMode = bitReader.ReadBit();
    }

    protected override void LookTowards(Vector2 target, float power)
    {
        Npc.rotation = Npc.rotation.AngleLerp(Npc.AngleTo(target) - MathHelper.PiOver2, power);
    }

    private void NextAttack()
    {
        CurrentAttackIndex = (CurrentAttackIndex + 1) % CurrentAttackPattern.Length;
    }
}