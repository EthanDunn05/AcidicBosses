using System;
using System.Collections.Generic;
using System.IO;
using AcidicBosses.Common;
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

namespace AcidicBosses.Content.Bosses.KingSlime;

public class KingSlime : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.KingSlime;

    #region Phases
    

    private enum PhaseState
    {
        One,
        Transition1,
        Two,
        Transition2,
        Desperation
    }

    private PhaseState CurrentPhase
    {
        get => (PhaseState) Npc.ai[2];
        set => Npc.ai[2] = (float) value;
    }
    
    #endregion
    
    #region Attacks
    
    private enum Attack
    {
        None, // Only used when transitioning
        JumpShort,
        JumpTall,
        SlimeBurst,
        TeleportAbove,
        SummonSlime,
        CrownLaser
    }
    
    private readonly List<Attack> phase1Ap = new()
    {
        Attack.JumpShort,
        Attack.JumpShort,
        Attack.JumpTall,
        Attack.TeleportAbove,
        Attack.SlimeBurst,
        Attack.JumpTall,
        Attack.JumpShort,
        Attack.TeleportAbove
    };

    private readonly List<Attack> phase2Ap = new()
    {
        Attack.JumpShort,
        Attack.JumpShort,
        Attack.SummonSlime,
        Attack.JumpTall,
        Attack.TeleportAbove,
        Attack.CrownLaser,
        Attack.SlimeBurst,
        Attack.JumpTall,
        Attack.JumpShort,
        Attack.TeleportAbove,
        Attack.SummonSlime,
        Attack.SlimeBurst
    };

    private readonly List<Attack> phase3Ap = new()
    {
        Attack.JumpShort,
        Attack.JumpTall,
        Attack.SlimeBurst,
        Attack.TeleportAbove,
        Attack.SummonSlime,
        Attack.JumpShort,
        Attack.JumpTall,
        Attack.SlimeBurst,
        Attack.TeleportAbove,
        Attack.JumpShort,
        Attack.CrownLaser,
        Attack.SummonSlime,
    };
    
    private readonly List<Attack> canFallThroughPlatform = new()
    {
        Attack.JumpShort,
        Attack.JumpTall,
        Attack.SummonSlime
    };
    
    private int CurrentAttackPatternIndex
    {
        get => (int) Npc.ai[1];
        set => Npc.ai[1] = (int) value;
    }
    
    private void NextAttack()
    {
        CurrentAttackPatternIndex = (CurrentAttackPatternIndex + 1) % CurrentAttackPattern.Count;
    }
    
    #endregion
    
    #region AI
    
    private int AiTimer
    {
        get => (int) Npc.ai[0];
        set => Npc.ai[0] = value;
    }

    private bool BypassActionTimer { get; set; } = false;
    private bool isGrounded = true;
    private bool isFleeing = false;
    private float targetScale = 1.25f;

    // Unsynced Variables
    private Vector2 teleportDestination = Vector2.Zero;

    private List<Attack> CurrentAttackPattern => CurrentPhase switch
    {
        PhaseState.One => phase1Ap,
        PhaseState.Two => phase2Ap,
        PhaseState.Desperation => phase3Ap,
        _ => throw new UsageException(
            $"King Slime is in the PhaseState {CurrentPhase} and does not have an attack pattern")
    };

    private Attack CurrentAttack
    {
        get
        {
            if (CurrentPhase is PhaseState.Transition1 or PhaseState.Transition2) return Attack.None;
            return CurrentAttackPattern[CurrentAttackPatternIndex];
        }
    }

    public override void OnFirstFrame(NPC npc)
    {
        AiTimer = 0;
        CurrentPhase = PhaseState.One;
        ChangeScale(npc, targetScale);
    }

    public override bool AcidAI(NPC npc)
    {
        // Land
        if (!isGrounded && npc.velocity.Y == 0)
        {
            isGrounded = true;
            npc.velocity.X = 0;
        }

        // Update Timer
        if (AiTimer > 0 && !BypassActionTimer && isGrounded)
        {
            AiTimer = Math.Max(AiTimer - 1, 0);
        }

        // Flee when no players are alive
        if (IsTargetGone(npc) && isGrounded && !isFleeing)
        {
            npc.TargetClosest();
            if (IsTargetGone(npc))
            {
                BypassActionTimer = true;
                isFleeing = true;
                AiTimer = 0;
            }
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
                Phase_One(npc);
                break;
            case PhaseState.Transition1:
                Phase_Transition1(npc);
                break;
            case PhaseState.Two:
                Phase_Two(npc);
                break;
            case PhaseState.Transition2:
                Phase_Transition2(npc);
                break;
            case PhaseState.Desperation:
                Phase_Desperation(npc);
                break;
        }

        return false;
    }
    
    private void FleeAI(NPC npc)
    {
        if (!IsTargetGone(npc))
        {
            BypassActionTimer = false;
            isFleeing = false;
            AiTimer = 30;
            ChangeScale(npc, targetScale);
            return;
        }

        switch (AiTimer)
        {
            case >= 0 and < 120:
                var shrinkT = AiTimer / 120f;
                shrinkT = EasingHelper.QuadIn(shrinkT);
                ChangeScale(npc, MathHelper.Lerp(targetScale, 0f, shrinkT));
                break;
            default:
                npc.active = false;
                EffectsManager.BossRageKill();
                EffectsManager.ShockwaveKill();
                break;
        }

        AiTimer++;
    }

    // Phase AIs //

    #region Phases

    private void Phase_One(NPC npc)
    {
        // Test if we should move to the next phase
        if (npc.GetLifePercent() <= 0.75f)
        {
            CurrentPhase = PhaseState.Transition1;
            CurrentAttackPatternIndex = 0;
            AiTimer = 0;
            return;
        }
        
        if (AiTimer > 0 && !BypassActionTimer) return;
        
        switch (CurrentAttack)
        {
            case Attack.SlimeBurst:
                Attack_Burst(npc, 8);
                AiTimer = 30;
                NextAttack();
                break;
            case Attack.TeleportAbove:
                BypassActionTimer = true;
                Attack_Teleport(npc, out var doneTp);
                if (doneTp)
                {
                    BypassActionTimer = false;
                    AiTimer = 120;
                    NextAttack();
                }
                break;
            case Attack.JumpShort:
                Attack_Jump(npc, 4f, 10f);
                AiTimer = 30;
                NextAttack();
                break;
            case Attack.JumpTall:
                Attack_Jump(npc, 4f, 15f);
                AiTimer = 60;
                NextAttack();
                break;
        }
    }

    private void Phase_Two(NPC npc)
    {
        // Check for phase transition
        if (npc.GetLifePercent() <= 0.25f)
        {
            CurrentPhase = PhaseState.Transition2;
            AiTimer = 0;
            return;
        }

        if (AiTimer > 0 && !BypassActionTimer) return;

        // Preform the attack
        switch (CurrentAttack)
        {
            case Attack.SlimeBurst:
                BypassActionTimer = true;
                if (AiTimer < 30)
                {
                    if (AiTimer == 0) Attack_Burst(npc, 5);
                    if (AiTimer == 15) Attack_Burst(npc, 4);
                    AiTimer++;
                }
                else
                {
                    BypassActionTimer = false;
                    AiTimer = 30;
                    NextAttack();
                }
                break;
            case Attack.SummonSlime:
                Attack_Summon(npc);
                AiTimer = 15;
                NextAttack();
                break;
            case Attack.TeleportAbove:
                BypassActionTimer = true;
                Attack_Teleport(npc, out var doneTp);
                if (doneTp)
                {
                    BypassActionTimer = false;
                    AiTimer = 90;
                    NextAttack();
                }
                break;
            case Attack.CrownLaser:
                BypassActionTimer = true;
                Attack_CrownLaser(npc, out var doneCl);
                if (doneCl)
                {
                    AiTimer = 0;
                    BypassActionTimer = false;
                    NextAttack();
                }

                break;
            case Attack.JumpShort:
                Attack_Jump(npc, 6f, 7.5f);
                AiTimer = 30;
                NextAttack();
                break;
            case Attack.JumpTall:
                Attack_Jump(npc, 4f, 15f);
                AiTimer = 30;
                NextAttack(); 
                break;
        }
    }

    private void Phase_Desperation(NPC npc)
    {
        if (AiTimer > 0 && !BypassActionTimer) return;
        
        switch (CurrentAttack)
        {
            case Attack.SlimeBurst:
                BypassActionTimer = true;
                if (AiTimer < 30)
                {
                    if(AiTimer == 0) Attack_Burst(npc, 8);
                    if(AiTimer == 15) Attack_Burst(npc, 7);
                    AiTimer++;
                }
                else
                {
                    BypassActionTimer = false;
                    AiTimer = 15;
                    NextAttack();
                }
                break;
            case Attack.SummonSlime:
                BypassActionTimer = true;

                // Spawn 3 over 15 frames
                if (AiTimer < 15)
                {
                    if (AiTimer % 5 == 0) Attack_Summon(npc);
                    AiTimer++;
                }
                else
                {
                    AiTimer = 15;
                    BypassActionTimer = false;
                    NextAttack();
                }

                break;
            case Attack.TeleportAbove:
                BypassActionTimer = true;
                Attack_Teleport(npc, out var isDone);
                if (isDone)
                {
                    AiTimer = 30;
                    BypassActionTimer = false;
                    NextAttack();
                }

                break;
            case Attack.CrownLaser:
                BypassActionTimer = true;
                Attack_CrownLaserCircle(npc, out var done, 12);
                if (done)
                {
                    AiTimer = 0;
                    BypassActionTimer = false;
                    NextAttack();
                }

                break;
            case Attack.JumpShort:
                Attack_Jump(npc, 6f, 10f);
                AiTimer = 15;
                NextAttack();
                break;
            case Attack.JumpTall:
                Attack_Jump(npc, 6f, 15f);
                AiTimer = 30;
                NextAttack();
                break;
        }
    }

    private void Phase_Transition1(NPC npc)
    {
        BypassActionTimer = true;

        switch (AiTimer)
        {
            case >= 0 and < 120:
                var t = AiTimer / 120f;
                t = MathHelper.Clamp(t, 0, 1);
                ChangeScale(npc, MathHelper.Lerp(1.25f, 0.75f, t));

                if (AiTimer % 20 == 0)
                {
                    Attack_Summon(npc);
                }

                break;
            case >= 120 and < 150:
                if (AiTimer == 120)
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
                CurrentAttackPatternIndex = 0;
                AiTimer = 0;
                BypassActionTimer = false;
                return;
        }

        AiTimer++;
    }

    private void Phase_Transition2(NPC npc)
    {
        BypassActionTimer = true;

        switch (AiTimer)
        {
            case >= 0 and < 30:
                var roarT = AiTimer / 29f;
                SoundEngine.PlaySound(SoundID.Roar, npc.Center);

                // Effects
                if (AiTimer == 0)
                {
                    EffectsManager.ShockwaveActive(npc.Center, 0.15f, 0.25f, Color.Red);
                }

                EffectsManager.ShockwaveProgress(roarT);
                if (AiTimer == 29) EffectsManager.ShockwaveKill();

                break;
            case >= 30 and < 90:
                var shrinkT = (AiTimer - 30f) / (90f - 30f);
                ChangeScale(npc, MathHelper.Lerp(0.75f, 0.5f, shrinkT));

                if (AiTimer % 10 == 0) Attack_Summon(npc);
                break;
            case 180:
                BypassActionTimer = false;
                CurrentPhase = PhaseState.Desperation;
                CurrentAttackPatternIndex = 0;
                targetScale = 0.5f;
                AiTimer = 0;
                return;
        }

        AiTimer++;
    }

    #endregion

    // Attacks //

    #region Attacks

    private void Attack_Jump(NPC npc, float horizontalVelocity, float jumpVelocity)
    {
        var direction = Math.Sign(Main.player[npc.target].position.X - npc.position.X);

        npc.velocity = new Vector2(horizontalVelocity * direction, -jumpVelocity);
        isGrounded = false;
    }

    private void Attack_Burst(NPC npc, int projCount)
    {
        // Burst Attack
        SoundEngine.PlaySound(SoundID.SplashWeak, npc.Center);

        // Create the projectiles
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        for (var i = 0; i < projCount; i++)
        {
            var dAngle = MathF.PI / projCount;
            var angle = dAngle * i + dAngle / 2 + MathF.PI;

            const int speed = 10;
            var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            Projectile.NewProjectile(
                npc.GetSource_FromAI(),
                npc.Center,
                velocity,
                ModContent.ProjectileType<SlimeSpikeProjectile>(),
                (int) (Npc.damage * 0.5f),
                2
            );
        }
    }

    private void Attack_Teleport(NPC npc, out bool done)
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

        switch (AiTimer)
        {
            case >= 0 and < 60: // Indicate While Tracking
                teleportDestination = Main.player[npc.target].position;
                teleportDestination.Y -= 256;
                IndicationDust();
                break;
            case >= 60 and < 90: // Keep indicating while shrinking
                var shrinkT = (AiTimer - 60f) / (90f - 60f);
                shrinkT = EasingHelper.QuadOut(shrinkT);
                ChangeScale(npc, MathHelper.Lerp(targetScale, 0f, shrinkT));
                
                IndicationDust();
                break;
            case >= 90 and < 120: // Grow at the indicated position
                npc.position = teleportDestination;

                if (AiTimer == 90)
                    Gore.NewGore(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, GoreID.KingSlimeCrown);

                var growT = (AiTimer - 90f) / (120f - 90f);
                growT = EasingHelper.QuadIn(growT);
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
        AiTimer++;
    }

    private void Attack_Summon(NPC npc)
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

    private void Attack_CrownLaser(NPC npc, out bool done)
    {
        switch (AiTimer)
        {
            case 0:
                SoundEngine.PlaySound(SoundID.Item8, npc.Center);

                if (Main.netMode == NetmodeID.MultiplayerClient) break;
                
                var gemPos = new Vector2(npc.Center.X, npc.position.Y);
                var rotation = gemPos.DirectionTo(Main.player[npc.target].Center).ToRotation();

                var proj = NewCrownLaser(rotation);
                proj.timeLeft = 60;
                break;
            case 180:
                done = true;
                return;
        }

        done = false;
        AiTimer++;
    }

    private void Attack_CrownLaserCircle(NPC npc, out bool done, int projCount)
    {
        const int length = 30;
        switch (AiTimer)
        {
            case >= 0 and < length:
                if (AiTimer % (length / projCount) == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item8, npc.Center);

                    if (Main.netMode == NetmodeID.MultiplayerClient) break;

                    // Circle lasers
                    var dAngle = MathHelper.TwoPi / projCount;
                    var i = (AiTimer / ((float) length / projCount));
                    var angle = dAngle * i + dAngle / 2f;

                    var proj = NewCrownLaser(angle);
                    proj.timeLeft = 60;
                }

                break;
            case 180:
                done = true;
                return;
        }

        done = false;
        AiTimer++;
    }

    private Projectile NewCrownLaser(float rotation)
    {
        var gemPos = new Vector2(Npc.Center.X, Npc.position.Y);
        return Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), gemPos, Vector2.Zero,
            ModContent.ProjectileType<KingSlimeLaserIndicator>(), 25, 10, ai0: rotation);
    }

    #endregion
    
    #endregion

    // Other Stuff //

    #region Draw

    public override bool AcidicDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        DrawNinja(npc, spriteBatch, screenPos, lightColor);
        
        if (CurrentPhase is PhaseState.Desperation or PhaseState.Transition2)
        {
            spriteBatch.StartShader();
            EffectsManager.SlimeRageApply(TextureAssets.Npc[npc.type]);
        }
        
        DrawSlime(npc, spriteBatch, screenPos, lightColor);
        
        if (CurrentPhase is PhaseState.Desperation or PhaseState.Transition2)
        {
            spriteBatch.EndShader();
        }
        
        DrawCrown(npc, spriteBatch, screenPos, lightColor);
        
        return false;
    }

    private void DrawNinja(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        // Draw Ninja
        Vector2 zero = Vector2.Zero;
        float num243 = 0f;
        zero.Y -= npc.velocity.Y;
        zero.X -= npc.velocity.X * 2f;
        num243 += npc.velocity.X * 0.05f;
        if (npc.frame.Y == 120)
        {
            zero.Y += 2f;
        }
        if (npc.frame.Y == 360)
        {
            zero.Y -= 2f;
        }
        if (npc.frame.Y == 480)
        {
            zero.Y -= 6f;
        }
        spriteBatch.Draw(TextureAssets.Ninja.Value, new Vector2(npc.position.X - screenPos.X + (float)(npc.width / 2) + zero.X, npc.position.Y - screenPos.Y + (float)(npc.height / 2) + zero.Y), new Rectangle(0, 0, TextureAssets.Ninja.Width(), TextureAssets.Ninja.Height()), lightColor, num243, new Vector2(TextureAssets.Ninja.Width() / 2, TextureAssets.Ninja.Height() / 2), 1f, SpriteEffects.None, 0f);
    }

    private void DrawSlime(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        var drawPos = npc.Center - screenPos + Vector2.UnitY * npc.gfxOffY;
        drawPos.Y -= 8f * npc.scale; // KS is in the ground unless I do this for some ungodly reason
        var texAsset = TextureAssets.Npc[npc.type];
        var texture = texAsset.Value;
        var origin = npc.frame.Size() * 0.5f;

        spriteBatch.Draw(
            texture, drawPos,
            npc.frame, npc.GetAlpha(lightColor),
            npc.rotation, origin, npc.scale,
            SpriteEffects.None, 0f);
    }

    private void DrawCrown(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        Texture2D value87 = TextureAssets.Extra[39].Value;
        Vector2 center3 = npc.Center;
        float num154 = 0f;
        switch (npc.frame.Y / (TextureAssets.Npc[npc.type].Value.Height / Main.npcFrameCount[npc.type]))
        {
            case 0:
                num154 = 2f;
                break;
            case 1:
                num154 = -6f;
                break;
            case 2:
                num154 = 2f;
                break;
            case 3:
                num154 = 10f;
                break;
            case 4:
                num154 = 2f;
                break;
            case 5:
                num154 = 0f;
                break;
        }
        center3.Y += npc.gfxOffY - (70f - num154) * npc.scale;
        spriteBatch.Draw(value87, center3 - screenPos, null, lightColor, 0f, value87.Size() / 2f, 1f, SpriteEffects.None, 0f);

    }

    #endregion

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

    public override void SendAcidAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        bitWriter.WriteBit(BypassActionTimer);
        bitWriter.WriteBit(isGrounded);
        bitWriter.WriteBit(isFleeing);
    }

    public override void ReceiveAcidAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        BypassActionTimer = bitReader.ReadBit();
        isGrounded = bitReader.ReadBit();
        isFleeing = bitReader.ReadBit();
    }

    public override void HitEffect(NPC npc, NPC.HitInfo hit)
    {
        if (npc.life <= 0)
        {
            EffectsManager.BossRageKill();
            EffectsManager.ShockwaveKill();
        }
    }

    public override bool? CanFallThroughPlatforms(NPC npc)
    {
        return (canFallThroughPlatform.Contains(CurrentAttack) && AiTimer > 0)
               && (Main.player[npc.target].Center.Y > npc.position.Y + npc.height);
    }
}