using System;
using System.IO;
using AcidicBosses.Shaders;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Bosses.KingSlime;

public class AcidicKingSlime : AcidicBossOverride
{
    public override int OverriddenNpc => NPCID.KingSlime;
    
    private NPC ThisNpc { get; set; }

    private enum PhaseState
    {
        One,
        Transition1,
        Two,
        Transition2,
        Desperation
    }

    private enum AttackState
    {
        None,
        Jumping,
        Bursting,
        Teleporting,
        Summoning,
        CrownLaser
    }

    // Synced Variables
    private int AITimer
    {
        get => (int) ThisNpc.ai[0];
        set => ThisNpc.ai[0] = value;
    }

    
    private AttackState CurrentAttack
    {
        get => (AttackState) ThisNpc.ai[1];
        set => ThisNpc.ai[1] = (float) value;
    }

    private PhaseState CurrentPhase
    {
        get => (PhaseState) ThisNpc.ai[2];
        set => ThisNpc.ai[2] = (float) value;
    }

    private int ActionCount { get; set; } = 0;
    private int BurstTimer { get; set; } = 300;
    private int TeleportTimer { get; set; } = 500;
    private int SummonTimer { get; set; } = 600;
    private bool BypassActionTimer { get; set; } = false;
    private bool isGrounded = true;
    private bool isFleeing = false;
    private float targetScale = 1.25f;

    // Unsynced Variables
    private Vector2 teleportDestination = Vector2.Zero;

    public override void OnFirstFrame(NPC npc)
    {
        ThisNpc = npc;
        AITimer = 0;
        CurrentPhase = PhaseState.One;
        CurrentAttack = AttackState.None;
    }
    
    public override bool AcidAI(NPC npc)
    {
        // Land
        if (!isGrounded && npc.velocity.Y == 0)
        {
            isGrounded = true;
            npc.velocity.X = 0;
        }

        // Update Timers
        BurstTimer = Math.Max(BurstTimer - 1, 0);
        TeleportTimer = Math.Max(TeleportTimer - 1, 0);
        SummonTimer = Math.Max(SummonTimer - 1, 0);
        if (AITimer > 0 && !BypassActionTimer && isGrounded)
        {
            AITimer = Math.Max(AITimer - 1, 0);
        }
        
        // Flee when no players are alive
        if (!npc.HasValidTarget && isGrounded)
        {
            isFleeing = true;
        }
        
        if (isFleeing)
        {
            FleeAI(npc);
            return false;
        }

        if (!isGrounded) return false;

        // Select current phase method
        switch (CurrentPhase)
        {
            case PhaseState.One:
                PhaseOneAI(npc);
                break;
            case PhaseState.Transition1:
                Transition1AI(npc);
                break;
            case PhaseState.Two:
                PhaseTwoAI(npc);
                break;
            case PhaseState.Transition2:
                Transition2AI(npc);
                break;
            case PhaseState.Desperation:
                DesperationPhaseAI(npc);
                break;
        }

        return false;
    }

    // Phase AIs //

    #region Phases

    private void PhaseOneAI(NPC npc)
    {
        // Test if we should move to the next phase
        if (npc.GetLifePercent() <= 0.75f)
        {
            CurrentPhase = PhaseState.Transition1;
            AITimer = 0;
            return;
        }

        // Decide what to do once grounded
        if (CurrentAttack == AttackState.None)
        {
            if (TeleportTimer <= 0) CurrentAttack = AttackState.Teleporting;
            else if (BurstTimer == 0) CurrentAttack = AttackState.Bursting;
            else CurrentAttack = AttackState.Jumping;
        }
        
        if (AITimer > 0 && !BypassActionTimer) return;

        // Perform Attack
        switch (CurrentAttack)
        {
            case AttackState.Bursting:
                BurstAttack(npc);
                AITimer = 30;
                BurstTimer = Main.rand.Next(300, 600); // 5-10 second delay
                CurrentAttack = AttackState.None;
                NetSync(npc);
                break;
            case AttackState.Teleporting:
                BypassActionTimer = true;
                TeleportAttack(npc, out var doneTp);
                if (doneTp)
                {
                    BypassActionTimer = false;
                    CurrentAttack = AttackState.None;
                    AITimer = 120;
                    TeleportTimer = Main.rand.Next(600, 1200); // 10-20 seconds
                    NetSync(npc);
                }

                break;
            case AttackState.Jumping:
                if (ActionCount <= 3)
                {
                    JumpAttack(npc, 4f, 10f);
                    AITimer = Main.rand.Next(60, 90); // 1-1.5 second delay
                    NetSync(npc);
                }
                else if (ActionCount == 4)
                {
                    JumpAttack(npc, 4f, 15f);
                    AITimer = 120;
                    ActionCount = -1;
                }

                ActionCount++;
                CurrentAttack = AttackState.None;
                break;
        }
    }

    private void PhaseTwoAI(NPC npc)
    {
        // Check for phase transition
        if (npc.GetLifePercent() <= 0.25f)
        {
            CurrentPhase = PhaseState.Transition2;
            AITimer = 0;
            return;
        }

        // Decide Attack State
        if (CurrentAttack == AttackState.None)
        {
            if (TeleportTimer <= 0) CurrentAttack = AttackState.Teleporting;
            else if (SummonTimer <= 0) CurrentAttack = AttackState.Summoning;
            else if (BurstTimer <= 0)
            {
                if (Main.rand.NextBool(3)) CurrentAttack = AttackState.Bursting;
                else CurrentAttack = AttackState.CrownLaser;
            }
            else CurrentAttack = AttackState.Jumping;
        }

        if (AITimer > 0 && !BypassActionTimer) return;

        // Preform the attack
        switch (CurrentAttack)
        {
            case AttackState.Bursting:
                BurstAttack(npc);
                AITimer = 30;
                BurstTimer = Main.rand.Next(300, 600); // 5-10 second delay
                CurrentAttack = AttackState.None;
                NetSync(npc);
                break;
            case AttackState.Summoning:
                SummonAttack(npc);
                SummonTimer = Main.rand.Next(180, 240); // 3-4 second delay
                AITimer = 15;
                CurrentAttack = AttackState.None;
                NetSync(npc);
                break;
            case AttackState.Teleporting:
                BypassActionTimer = true;
                TeleportAttack(npc, out var doneTp);
                if (doneTp)
                {
                    BypassActionTimer = false;
                    CurrentAttack = AttackState.None;
                    AITimer = 90;
                    TeleportTimer = Main.rand.Next(300, 600); // 5-10 seconds
                    NetSync(npc);
                }

                break;
            case AttackState.CrownLaser:
                BypassActionTimer = true;
                CrownLaserAttack(npc, out var doneCl);
                if (doneCl)
                {
                    AITimer = 0;
                    BurstTimer = Main.rand.Next(180, 300); // 3-5 second delay
                    CurrentAttack = AttackState.None; // Jump next frame
                    BypassActionTimer = false;
                    NetSync(npc);
                }

                break;
            case AttackState.Jumping:
                if (ActionCount < 3)
                {
                    JumpAttack(npc, 6f, 7.5f);
                    AITimer = 30;
                }
                else if (ActionCount == 3)
                {
                    JumpAttack(npc, 4f, 12.5f);
                    AITimer = 60;
                    ActionCount = -1;
                }

                ActionCount++;
                CurrentAttack = AttackState.None;
                break;
        }
    }

    private void DesperationPhaseAI(NPC npc)
    {
        // Select next attack
        if (CurrentAttack == AttackState.None)
        {
            if (TeleportTimer <= 0) CurrentAttack = AttackState.Teleporting;
            else if (SummonTimer <= 0) CurrentAttack = AttackState.Summoning;
            else if (BurstTimer <= 0) CurrentAttack = AttackState.CrownLaser;
            else CurrentAttack = AttackState.Jumping;
        }

        if (AITimer > 0 && !BypassActionTimer) return;

        // Random Jump
        switch (CurrentAttack)
        {
            case AttackState.Summoning:
                BypassActionTimer = true;

                // Spawn 3 over 15 frames
                if (AITimer < 15)
                {
                    if (AITimer % 5 == 0) SummonAttack(npc);
                    AITimer++;
                }
                else
                {
                    AITimer = 15;
                    SummonTimer = 300;
                    BypassActionTimer = false;
                    CurrentAttack = AttackState.None;
                }

                break;
            case AttackState.Teleporting:
                BypassActionTimer = true;
                TeleportAttack(npc, out var isDone);
                if (isDone)
                {
                    AITimer = 30;
                    TeleportTimer = 500;
                    BypassActionTimer = false;
                    CurrentAttack = AttackState.None;
                }

                break;
            case AttackState.CrownLaser:
                BypassActionTimer = true;
                CrownLaserPatternAttack(npc, out var done);
                if (done)
                {
                    AITimer = 0;
                    BurstTimer = Main.rand.Next(180, 300); // 3-5 second delay
                    CurrentAttack = AttackState.None; // Jump next frame
                    BypassActionTimer = false;
                    NetSync(npc);
                }

                break;
            case AttackState.Jumping:
                JumpAttack(npc, Main.rand.Next(4, 6), Main.rand.Next(8, 12));
                AITimer = 30;
                CurrentAttack = AttackState.None;
                break;
        }
    }

    private void Transition1AI(NPC npc)
    {
        BypassActionTimer = true;

        switch (AITimer)
        {
            case >= 0 and < 120:
                var t = AITimer / 120f;
                t = MathHelper.Clamp(t, 0, 1);
                ChangeScale(npc, MathHelper.Lerp(1.25f, 0.75f, t));

                if (AITimer % 20 == 0)
                {
                    SummonAttack(npc);
                }

                break;
            case >= 120 and < 150:
                if (AITimer == 120)
                {
                    for (var i = 0; i < 100; i++)
                    {
                        var vel = Main.rand.NextVector2CircularEdge(64, 64);
                        var dust = Dust.NewDust(
                            npc.Center + (vel / 2),
                            64, 64,
                            DustID.Water,
                            vel.X,
                            vel.Y
                        );

                        Main.dust[dust].noGravity = true;
                    }
                }

                break;
            default:
                targetScale = 0.75f;
                CurrentPhase = PhaseState.Two;
                CurrentAttack = AttackState.None;
                AITimer = 0;
                ActionCount = 0;
                BypassActionTimer = false;
                return;
        }

        AITimer++;
    }

    private void Transition2AI(NPC npc)
    {
        BypassActionTimer = true;

        switch (AITimer)
        {
            case >= 0 and < 30:
                var roarT = AITimer / 29f;
                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                npc.color = Color.Red;

                // Effects
                if (AITimer == 0)
                {
                    Effects.ShockwaveActive(npc.Center, 0.15f, 0.25f, Color.Red);
                    Effects.BossRageActivate(Color.MistyRose);
                }

                Effects.ShockwaveProgress(roarT);
                if (AITimer == 29) Effects.ShockwaveKill();

                break;
            case >= 30 and < 90:
                var shrinkT = (AITimer - 30f) / (90f - 30f);
                ChangeScale(npc, MathHelper.Lerp(0.75f, 0.5f, shrinkT));

                if (AITimer % 10 == 0) SummonAttack(npc);
                break;
            case 180:
                CurrentAttack = AttackState.None;
                BypassActionTimer = false;
                CurrentPhase = PhaseState.Desperation;
                targetScale = 0.5f;
                AITimer = 0;
                ActionCount = 0;
                BurstTimer = 200;
                SummonTimer = 0;
                TeleportTimer = 500;
                return;
        }

        AITimer++;
    }

    private void FleeAI(NPC npc)
    {
        if (npc.HasValidTarget)
        {
            isFleeing = false;
            AITimer = 30;
            ChangeScale(npc, targetScale);
            return;
        }

        switch (AITimer)
        {
            case >= 0 and < 120:
                var shrinkT = AITimer / 120f;
                ChangeScale(npc, MathHelper.Lerp(targetScale, 0f, shrinkT));
                break;
            default:
                npc.active = false;
                Effects.BossRageKill();
                break;
        }

        AITimer++;
    }

    #endregion

    // Attacks //

    #region Attacks

    private void JumpAttack(NPC npc, float horizontalVelocity, float jumpVelocity)
    {
        var direction = Math.Sign(Main.player[npc.target].position.X - npc.position.X);

        npc.velocity = new Vector2(horizontalVelocity * direction, -jumpVelocity);
        isGrounded = false;
    }

    private void BurstAttack(NPC npc)
    {
        // Burst Attack
        SoundEngine.PlaySound(SoundID.SplashWeak, npc.Center);
        const int projCount = 8;

        // Create the projectiles
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        for (var i = 0; i < projCount; i++)
        {
            var angle = MathF.PI * i / projCount + MathF.PI;

            const int speed = 10;
            var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            Projectile.NewProjectile(
                npc.GetSource_FromAI(),
                npc.Center,
                velocity,
                ModContent.ProjectileType<SlimeSpikeProjectile>(),
                20,
                2
            );
        }
    }

    private void TeleportAttack(NPC npc, out bool done)
    {
        // TODO Add light to the indication for night visibility
        void IndicationDust()
        {
            for (var i = 0; i < 25; i++)
            {
                var offset = Main.rand.NextVector2Circular(npc.height, npc.height);
                Dust.NewDust(
                    teleportDestination + offset,
                    32, 32,
                    DustID.Water
                );
            }
        }

        // This is gonna suck to program
        switch (AITimer)
        {
            case >= 0 and < 60: // Indicate While Tracking
                teleportDestination = Main.player[npc.target].position;
                teleportDestination.Y -= 256;
                IndicationDust();
                break;
            case >= 60 and < 90: // Keep indicating while shrinking
                var shrinkT = (AITimer - 60f) / (90f - 60f);
                ChangeScale(npc, MathHelper.Lerp(targetScale, 0f, shrinkT));

                IndicationDust();
                break;
            case >= 90 and < 120: // Grow at the indicated position
                npc.position = teleportDestination;

                var growT = (AITimer - 90f) / (120f - 90f);
                ChangeScale(npc, MathHelper.Lerp(0f, targetScale, growT));
                IndicationDust();
                break;
            default: // Finish the teleport
                npc.position = teleportDestination;
                npc.velocity.Y = 10f;
                isGrounded = false;
                ChangeScale(npc, targetScale);
                done = true;
                return;
        }

        done = false;
        AITimer++;
    }

    private void SummonAttack(NPC npc)
    {
        SoundEngine.PlaySound(SoundID.Item95, npc.Center);

        // Only spawn from a server
        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        var type = Main.rand.Next(100) switch
        {
            < 40 => NPCID.BlueSlime, // 40%
            >= 40 and < 65 => NPCID.SlimeSpiked, // 25%
            >= 65 and < 99 => NPCID.WindyBalloon, // 34%
            >= 99 => NPCID.Pinky // 1%
        };

        var summon = NPC.NewNPC(npc.GetSource_FromAI(), (int) npc.Center.X, (int) npc.Center.Y, type);

        Main.npc[summon].velocity =
            Main.rand.NextVector2Unit(MathHelper.Pi + MathHelper.PiOver4, MathHelper.PiOver2) * 10;
    }

    private void CrownLaserAttack(NPC npc, out bool done)
    {
        switch (AITimer)
        {
            case 0:
                SoundEngine.PlaySound(SoundID.Item8, npc.Center);

                if (Main.netMode == NetmodeID.MultiplayerClient) break;

                var gemPos = new Vector2(npc.Center.X, npc.position.Y);
                var rotation = gemPos.DirectionTo(Main.player[npc.target].Center).ToRotation();

                Projectile.NewProjectile(npc.GetSource_FromAI(), gemPos, Vector2.Zero,
                    ModContent.ProjectileType<KingSlimeCrownLaser>(), 25, 10, ai0: rotation);
                break;
            case 180:
                done = true;
                return;
        }

        done = false;
        AITimer++;
    }

    private void CrownLaserPatternAttack(NPC npc, out bool done)
    {
        const int projCount = 6;
        const int length = 30;
        switch (AITimer)
        {
            case >= 0 and < length:
                if (AITimer % (length / projCount) == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item8, npc.Center);

                    if (Main.netMode == NetmodeID.MultiplayerClient) break;

                    var gemPos = new Vector2(npc.Center.X, npc.position.Y);
                    var dAngle = MathF.PI / projCount;
                    var i = (AITimer / ((float) length / projCount));
                    var angle = dAngle * i + dAngle / 2f + MathF.PI;

                    Projectile.NewProjectile(npc.GetSource_FromAI(), gemPos, Vector2.Zero,
                        ModContent.ProjectileType<KingSlimeCrownLaser>(), 25, 10, ai0: angle);
                }

                break;
            case 180:
                done = true;
                return;
        }

        done = false;
        AITimer++;
    }

    #endregion

    // Other Stuff //

    private void ChangeScale(NPC npc, float scale)
    {
        // Don't know why it has to be done this way, but it's how the game does it
        npc.position.X += npc.width / 2;
        npc.position.Y += npc.height;
        npc.scale = scale;
        npc.width = (int) (98f * scale);
        npc.height = (int) (92f * scale);
        npc.position.X -= npc.width / 2;
        npc.position.Y -= npc.height;
    }

    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        binaryWriter.Write(ActionCount);
        binaryWriter.Write(BurstTimer);
        binaryWriter.Write(TeleportTimer);
        binaryWriter.Write(SummonTimer);
        bitWriter.WriteBit(BypassActionTimer);
        bitWriter.WriteBit(isGrounded);
        bitWriter.WriteBit(isFleeing);
    }

    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        ActionCount = binaryReader.ReadInt32();
        BurstTimer = binaryReader.ReadInt32();
        TeleportTimer = binaryReader.ReadInt32();
        SummonTimer = binaryReader.ReadInt32();
        BypassActionTimer = bitReader.ReadBit();
        isGrounded = bitReader.ReadBit();
        isFleeing = bitReader.ReadBit();
    }

    public override void HitEffect(NPC npc, NPC.HitInfo hit)
    {
        if (npc.life <= 0)
        {
            Effects.BossRageKill();
        }
    }

    public override bool? CanFallThroughPlatforms(NPC npc)
    {
        return (CurrentAttack is AttackState.Jumping or AttackState.None)
               && (Main.player[npc.target].Center.Y > npc.position.Y + npc.height);
    }
}