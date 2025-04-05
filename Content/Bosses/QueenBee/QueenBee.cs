using System;
using System.IO;
using AcidicBosses.Common.Configs;
using AcidicBosses.Content.Bosses.EoC;
using AcidicBosses.Content.Particles.Animated;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Content.Projectiles;
using AcidicBosses.Core.StateManagement;
using AcidicBosses.Helpers;
using AcidicBosses.Helpers.NpcHelpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Content.Bosses.QueenBee;

public class QueenBee : AcidicNPCOverride
{
    private static readonly SoundStyle SpitSound = SoundID.Item17;
    private static readonly SoundStyle ScreechSound = SoundID.Zombie125;
    public static readonly SoundStyle FlapSound = SoundID.Item32;
    private static readonly SoundStyle SummonBeesSound = SoundID.DD2_WyvernScream;

    protected override int OverriddenNpc => NPCID.QueenBee;
    protected override bool BossEnabled => BossToggleConfig.Get().EnableQueenBee;

    private bool useUprightSprite = true;
    private bool useAfterimages = false;
    
    private Vector2 StingerPos => Npc.Center + ((Npc.Bottom - Npc.Center) * 1.75f) + new Vector2(25f * (Npc.rotation.ToRotationVector2().X > 0 ? 1 : -1), 0f);

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        NPCID.Sets.TrailingMode[NPCID.QueenBee] = 3;
    }

    #region AI

    private PhaseTracker phaseTracker;

    private bool enraged = false;

    public override void OnFirstFrame(NPC npc)
    {
        phaseTracker = new PhaseTracker([
            PhaseOne,
        ]);
    }

    public override bool AcidAI(NPC npc)
    {
        phaseTracker.RunPhaseAI();

        return false;
    }

    #endregion

    #region Phases

    private PhaseState PhaseOne => new PhaseState(Phase_One, Phase_EnterOne);

    private void Phase_EnterOne()
    {
        var teleport = new AttackState(() =>
        {
            Teleport(TargetPlayer.Center - new Vector2(0, 250), 5f);
            return true;
        }, 120);

        var dashR = new AttackState(() => Attack_SideDash(1, 20), 30);
        var dashL = new AttackState(() => Attack_SideDash(-1, 20), 0);
        var dash = new AttackState(() => Attack_Dash(20), 0);

        var beeWave = new AttackState(Attack_BeeWave, 120);

        AttackManager.SetAttackPattern([
            teleport,
            dashR,
            dashL,
            dash,
            dash,
            dash,
            teleport,
            beeWave,
            dash,
        ]);
    }


    private void Phase_One()
    {
        if (AttackManager.AiTimer > 0 && !AttackManager.CountUp)
        {
            HoverAbove(10f, 0.3f, 250f);
            return;
        }

        AttackManager.RunAttackPattern();
    }

    #endregion

    #region Attacks

    private void HoverAbove(float speed, float acceleration, float distance)
    {
        var target = Main.player[Npc.target];
        var goal = target.Center;
        goal.Y -= distance;

        Npc.SimpleFlyMovement(Npc.DirectionTo(goal) * speed, acceleration);

        Npc.rotation = new Vector2(Npc.DirectionTo(goal).X > 0 ? 1 : -1, 0f).ToRotation();
    }

    private void Teleport(Vector2 position, float recoil)
    {
        var awayDir = Npc.DirectionTo(position);
        var startPos = Npc.Center;

        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            NewAfterimage(startPos, position);
        }

        Npc.Center = position;
        Npc.velocity = awayDir * recoil;

        SoundEngine.PlaySound(FlapSound, Npc.Center);

        var disperse = new BigSmokeDisperseParticle(Npc.Center, Vector2.Zero, 0f, Color.WhiteSmoke, 30);
        disperse.Scale *= 2f;
        disperse.Opacity = 0.5f;
        disperse.Spawn();
    }

    private bool Attack_SideDash(int side, float speed)
    {
        ref var dashStartX = ref Npc.localAI[0];

        var dashOptions = new DashOptions
        {
            DashLength = 30,
            DashSpeed = speed,
            TrackTime = 0,
            DashAtTime = 60,
            MinimumDistance = 0,
            LookOffset = 0,
            DontReposition = true
        };

        var dashState = DashHelper.Dash(Npc, AttackManager, TargetPlayer.Center, dashOptions);

        // Prepare dash
        if (AttackManager.AiTimer == 0)
        {
            var distance = 300;
            var pos = TargetPlayer.Center + new Vector2(distance * side, 0f);
            var rotation = new Vector2(-side, 0f).ToRotation();

            Teleport(pos, 0f);
            Npc.rotation = rotation;
            dashStartX = pos.X;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NewDashLine(Npc.Center, 0f, 60);
            }

            SoundEngine.PlaySound(ScreechSound, Npc.Center);
        }

        if (dashState == DashState.Waiting)
        {
            var progress = Utils.GetLerpValue(0f, 60f, AttackManager.AiTimer);
            var ease = EasingHelper.BackOut(progress);
            var offset = MathHelper.Lerp(0f, 50f * side, ease);

            Npc.Center = new Vector2(dashStartX + offset, Npc.Center.Y);
        }

        if (dashState == DashState.Dashing)
        {
            useUprightSprite = false;
            useAfterimages = true;
        }

        if (dashState == DashState.Done)
        {
            useUprightSprite = true;
            useAfterimages = false;

            dashStartX = 0f;
        }

        return dashState == DashState.Done;
    }

    private bool Attack_Dash(float speed)
    {
        var dashOptions = new DashOptions
        {
            DashLength = 30,
            DashSpeed = speed,
            TrackTime = 30,
            DashAtTime = 60,
            MinimumDistance = 200,
            LookOffset = 0,
            DontReposition = false
        };

        var dashState = DashHelper.Dash(Npc, AttackManager, TargetPlayer.Center, dashOptions);

        // Prepare dash
        if (AttackManager.AiTimer == 0 && dashState != DashState.Repositioning)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NewDashLine(Npc.Center, 0f, 60);
            }

            SoundEngine.PlaySound(ScreechSound, Npc.Center);
        }

        if (dashState == DashState.Dashing)
        {
            useUprightSprite = false;
            useAfterimages = true;
        }

        if (dashState == DashState.Done)
        {
            useUprightSprite = true;
            useAfterimages = false;
        }

        return dashState == DashState.Done;
    }

    private bool Attack_BeeWave()
    {
        AttackManager.CountUp = true;

        Npc.velocity = Vector2.Zero;

        if (AttackManager.AiTimer % 60 == 0 && AttackManager.AiTimer != 240)
        {
            new SmallPuffParticle(StingerPos, Vector2.Zero, 0f, Color.Yellow, 30).Spawn();
            NewBeeWave(StingerPos, Npc.DirectionTo(TargetPlayer.Center).ToRotation(), 180);
            SoundEngine.PlaySound(SpitSound, StingerPos);
        }

        if (AttackManager.AiTimer > 240)
        {
            AttackManager.CountUp = false;
            return true;
        }

        return false;
    }

    #endregion

    #region Projectiles

    private Projectile NewAfterimage(Vector2 startPos, Vector2 endPos)
    {
        return QueenBeeAfterimageTrail.Create(Npc.GetSource_FromAI(), startPos, endPos, Npc.whoAmI);
    }

    private Projectile NewDashLine(Vector2 position, float offset, int lifetime, bool anchorToBoss = true)
    {
        var ai1 = anchorToBoss ? Npc.whoAmI : -1;
        return BaseLineProjectile.Create<QueenBeeDashLine>(Npc.GetSource_FromAI(), position, offset, lifetime, ai1);
    }

    private Projectile NewBeeWave(Vector2 position, float rotation, int lifetime)
    {
        return BaseSwarmProjectile.Create<BeeWave>(Npc.GetSource_FromAI(), position, rotation, Npc.damage, 3,
            lifetime);
    }

    #endregion

    #region Drawing

    public override bool AcidicDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        var drawPos = npc.Center - Main.screenPosition;
        var texture = TextureAssets.Npc[npc.type].Value;
        var origin = npc.frame.Size() * 0.5f;
        lightColor *= npc.Opacity;

        var effects = SpriteEffects.FlipHorizontally;
        if (npc.rotation.ToRotationVector2().X < 0) effects |= SpriteEffects.FlipVertically;

        if (useAfterimages)
            for (var i = 1; i < npc.oldPos.Length; i++)
            {
                // All of this is heavily simplified from decompiled vanilla
                var fade = 0.5f * (10 - i) / 20f;
                var afterImageColor = Color.Multiply(lightColor, fade);

                var pos = npc.oldPos[i] + new Vector2(npc.width, npc.height) / 2f - Main.screenPosition;
                var oldRot = npc.oldRot[i];
                var oldEffect = SpriteEffects.FlipHorizontally;
                if (oldRot.ToRotationVector2().X < 0) oldEffect |= SpriteEffects.FlipVertically;

                spriteBatch.Draw(texture, pos, npc.frame, afterImageColor, oldRot, origin, npc.scale,
                    oldEffect, 0f);
            }

        spriteBatch.Draw(
            texture, drawPos,
            npc.frame, lightColor,
            npc.rotation, origin, npc.scale,
            effects, 0f);

        return false;
    }

    public override void FindFrame(NPC npc, int frameHeight)
    {
        if (!ShouldOverride()) base.FindFrame(npc, frameHeight);

        if (useUprightSprite)
        {
            if (npc.frameCounter > 4.0)
            {
                npc.frame.Y += frameHeight;
                npc.frameCounter = 0.0;
            }

            if (npc.frame.Y < frameHeight * 4)
                npc.frame.Y = frameHeight * 4;
            if (npc.frame.Y >= frameHeight * 12)
                npc.frame.Y = frameHeight * 4;
        }
        else
        {
            if (npc.frameCounter > 4.0)
            {
                npc.frame.Y += frameHeight;
                npc.frameCounter = 0.0;
            }

            if (npc.frame.Y >= frameHeight * 4)
                npc.frame.Y = 0;
        }
    }

    #endregion

    public override void SendAcidAI(BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        binaryWriter.WriteFlags(
            enraged
        );
    }

    public override void ReceiveAcidAI(BitReader bitReader, BinaryReader binaryReader)
    {
        binaryReader.ReadFlags(
            out enraged
        );
    }
}