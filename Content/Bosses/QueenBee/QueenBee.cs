using System;
using System.IO;
using AcidicBosses.Common.Configs;
using AcidicBosses.Common.Effects;
using AcidicBosses.Content.Bosses.EoC;
using AcidicBosses.Content.Particles.Animated;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Content.Projectiles;
using AcidicBosses.Core.Animation;
using AcidicBosses.Core.StateManagement;
using AcidicBosses.Helpers;
using AcidicBosses.Helpers.NpcHelpers;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
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

    private Vector2 StingerPos => Npc.Center + ((Npc.Bottom - Npc.Center) * 1.75f) +
                                  new Vector2(25f * (Npc.rotation.ToRotationVector2().X > 0 ? 1 : -1), 0f);

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
            PhaseIntro,
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

    private PhaseState PhaseIntro => new PhaseState(Phase_Intro);

    private AcidAnimation? introAnimation;

    private AcidAnimation PrepareIntroAnimation()
    {
        var anim = new AcidAnimation();

        anim.AddInstantEvent(0, () =>
        {
            Attack_Reposition();
            Npc.velocity = Vector2.Zero;

            anim.Data.Set<Vector2>("npcPos", Npc.position);

            ScreenShakeSystem.StartShakeAtPoint(Npc.Center, 10f);
        });

        anim.AddSequencedEvent(30, (progress, frame) =>
        {
            // Wait :)
        });

        anim.AddSequencedEvent(15, (progress, frame) =>
            ShootHive(progress, frame, new Vector2(-5f, -10f))
        );
        anim.AddSequencedEvent(15, (progress, frame) =>
            ShootHive(progress, frame, new Vector2(0f, -12.5f))
        );
        anim.AddSequencedEvent(15, (progress, frame) =>
            ShootHive(progress, frame, new Vector2(5f, -10f))
        );
        return anim;

        void ShootHive(float progress, int frame, Vector2 vel)
        {
            var pos = anim.Data.Get<Vector2>("npcPos");
            if (frame == 0)
            {
                Npc.rotation = vel.X < 0
                    ? new Vector2(-1, 0).ToRotation()
                    : new Vector2(1, 0).ToRotation();

                NewBeeHive(Npc.Center, vel);
            }

            var ease = EasingHelper.QuadOut(progress);
            Npc.position = pos + Vector2.Lerp(-vel * 2f, Vector2.Zero, ease);
        }
    }

    private void Phase_Intro()
    {
        introAnimation ??= PrepareIntroAnimation();
        var animationDone = introAnimation.RunAnimation();
        if (!animationDone) return;

        AttackManager.Reset();
        phaseTracker.NextPhase();
    }

    private PhaseState PhaseOne => new PhaseState(Phase_One, Phase_EnterOne);

    private void Phase_EnterOne()
    {
        AttackManager.SetAttackPattern([
            new AttackState(() => Attack_SideDash(-1, 20), 0),
            new AttackState(() => Attack_SideDash(1, 20), 60),
            new AttackState(() => Attack_BeePillar(0f, 0), 30),
            new AttackState(() => Attack_Dash(20), 0),
            new AttackState(() => Attack_Dash(20), 0),
            new AttackState(() => Attack_Dash(20), 0),
            new AttackState(Attack_Reposition, 120),
            new AttackState(Attack_BeeWave, 120),
            new AttackState(() => Attack_BeePillar(500f, 3), 30),
            new AttackState(() => Attack_SideDash(-1, 20), 0),
            new AttackState(() => Attack_SideDash(1, 20), 60),
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

        if (Npc.DistanceSQ(goal) > 50)
        {
            Npc.rotation = new Vector2(Npc.DirectionTo(goal).X > 0 ? 1 : -1, 0f).ToRotation();
        }
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

    private bool Attack_Reposition()
    {
        Teleport(TargetPlayer.Center - new Vector2(0, 250), 5f);
        return true;
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
                // NewDashLine(Npc.Center, 0f, 60);
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
                // NewDashLine(Npc.Center, 0f, 60);
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
            SoundEngine.PlaySound(SpitSound, StingerPos);
            NewBeeWave(StingerPos, Npc.DirectionTo(TargetPlayer.Center).ToRotation(), 180);
        }

        if (AttackManager.AiTimer > 240)
        {
            AttackManager.CountUp = false;
            return true;
        }

        return false;
    }

    private bool Attack_BeePillar(float spacing, int pillarsToEachSide)
    {
        AttackManager.CountUp = true;

        ref var centerX = ref Npc.localAI[0];
        ref var centerY = ref Npc.localAI[1];

        HoverAbove(10, 0.5f, 250);


        if (AttackManager.AiTimer == 0)
        {
            var center = Main.player[Npc.target].Center;
            centerX = center.X;
            centerY = center.Y;


            var pos = center;
            pos.Y = Utilities.FindGroundVertical(pos.ToTileCoordinates()).ToWorldCoordinates().Y;

            SoundEngine.PlaySound(SummonBeesSound, Npc.Center);
            NewDashLine(pos, new Vector2(0, -1).ToRotation(), 60, false);

            for (var i = 1; i <= pillarsToEachSide; i++)
            {
                var leftPos = center;
                var rightPos = center;

                leftPos.X -= spacing * i;
                rightPos.X += spacing * i;

                leftPos.Y = Utilities.FindGroundVertical(leftPos.ToTileCoordinates()).ToWorldCoordinates().Y;
                rightPos.Y = Utilities.FindGroundVertical(rightPos.ToTileCoordinates()).ToWorldCoordinates().Y;


                NewDashLine(leftPos, new Vector2(0, -1).ToRotation(), 60, false);
                NewDashLine(rightPos, new Vector2(0, -1).ToRotation(), 60, false);
            }
        }

        if (AttackManager.AiTimer == 60)
        {
            var center = new Vector2(centerX, centerY);
            var pos = center;
            pos.Y = Utilities.FindGroundVertical(pos.ToTileCoordinates()).ToWorldCoordinates().Y;

            new FireSmokeParticle(pos, Vector2.Zero, 0f, Color.Yellow, 30)
            {
                Scale = Vector2.One * 2f
            }.Spawn();
            SoundEngine.PlaySound(SpitSound, pos);
            NewBeePillar(pos, new Vector2(0, -1).ToRotation(), 180);

            ScreenShakeSystem.StartShakeAtPoint(pos, 5f);

            for (var i = 1; i <= pillarsToEachSide; i++)
            {
                var leftPos = center;
                var rightPos = center;

                leftPos.X -= spacing * i;
                rightPos.X += spacing * i;

                leftPos.Y = Utilities.FindGroundVertical(leftPos.ToTileCoordinates()).ToWorldCoordinates().Y;
                rightPos.Y = Utilities.FindGroundVertical(rightPos.ToTileCoordinates()).ToWorldCoordinates().Y;

                new FireSmokeParticle(leftPos, Vector2.Zero, 0f, Color.Yellow, 30)
                {
                    Scale = Vector2.One * 2f
                }.Spawn();
                new FireSmokeParticle(rightPos, Vector2.Zero, 0f, Color.Yellow, 30)
                {
                    Scale = Vector2.One * 2f
                }.Spawn();
                
                SoundEngine.PlaySound(SpitSound, leftPos);
                SoundEngine.PlaySound(SpitSound, rightPos);

                if (AcidUtils.IsServer())
                {
                    NewBeePillar(leftPos, new Vector2(0, -1).ToRotation(), 180);
                    NewBeePillar(rightPos, new Vector2(0, -1).ToRotation(), 180);
                }
            }

            AttackManager.CountUp = false;
            return true;
        }

        return false;
    }

    private bool Attack_HiveLaunch()
    {
        AttackManager.CountUp = true;

        Npc.velocity = Vector2.Zero;

        if (AttackManager.AiTimer == 0)
        {
            Npc.rotation = new Vector2(-1, 0).ToRotation();
            NewBeeHive(Npc.Center, new Vector2(-5, -10));
        }

        if (AttackManager.AiTimer == 15)
        {
            Npc.rotation = new Vector2(-1, 0).ToRotation();
            NewBeeHive(Npc.Center, new Vector2(0, -12.5f));
        }

        if (AttackManager.AiTimer == 30)
        {
            Npc.rotation = new Vector2(1, 0).ToRotation();
            NewBeeHive(Npc.Center, new Vector2(5, -10));

            AttackManager.CountUp = false;
            return true;
        }

        return false;
    }

    #endregion

    #region Projectiles

    private Projectile? NewAfterimage(Vector2 startPos, Vector2 endPos)
    {
        if (!AcidUtils.IsServer()) return null;
        return QueenBeeAfterimageTrail.Create(Npc.GetSource_FromAI(), startPos, endPos, Npc.whoAmI);
    }

    private Projectile? NewDashLine(Vector2 position, float offset, int lifetime, bool anchorToBoss = true)
    {
        if (!AcidUtils.IsServer()) return null;
        var ai1 = anchorToBoss ? Npc.whoAmI : -1;
        return BaseLineProjectile.Create<QueenBeePillarLine>(Npc.GetSource_FromAI(), position, offset, lifetime, ai1);
    }

    private Projectile? NewBeeWave(Vector2 position, float rotation, int lifetime)
    {
        if (!AcidUtils.IsServer()) return null;
        return BaseSwarmProjectile.Create<BeeWave>(Npc.GetSource_FromAI(), position, rotation, Npc.damage, 3,
            lifetime);
    }

    private Projectile? NewBeePillar(Vector2 position, float rotation, int lifetime)
    {
        if (!AcidUtils.IsServer()) return null;
        return BaseSwarmProjectile.Create<BeePillar>(Npc.GetSource_FromAI(), position, rotation, Npc.damage, 3,
            lifetime);
    }

    private Projectile? NewBeeHive(Vector2 position, Vector2 velocity)
    {
        if (!AcidUtils.IsServer()) return null;
        var proj = ProjHelper.NewUnscaledProjectile(Npc.GetSource_FromAI(), position, velocity, ProjectileID.BeeHive,
            Npc.damage, 3);
        proj.hostile = true;
        return proj;
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