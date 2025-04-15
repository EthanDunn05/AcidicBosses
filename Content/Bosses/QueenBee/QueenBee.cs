using System.IO;
using AcidicBosses.Common.Configs;
using AcidicBosses.Content.Particles;
using AcidicBosses.Content.Particles.Animated;
using AcidicBosses.Content.ProjectileBases;
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
    private static readonly SoundStyle FlapSound = SoundID.Item32;
    private static readonly SoundStyle SummonBeesSound = SoundID.DD2_WyvernScream with { Pitch = 0.5f };

    protected override int OverriddenNpc => NPCID.QueenBee;
    protected override bool BossEnabled => BossToggleConfig.Get().EnableQueenBee;

    private bool useUprightSprite = true;
    private bool useAfterimages = false;
    private bool doneIntermission = false;

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
            PhaseTwo
        ]);
    }

    public override bool AcidAI(NPC npc)
    {
        if (IsTargetGone(npc))
        {
            Npc.TargetClosest(false);

            if (IsTargetGone(npc))
            {
                Npc.active = false;
            }
        }

        // Enrage
        if (!TargetPlayer.ZoneJungle && !enraged)
        {
            phaseTracker.ChangeState(PhaseEnraged);
            enraged = true;
        }

        if (TargetPlayer.ZoneJungle && enraged)
        {
            phaseTracker.ResumePhase();
            enraged = false;
        }

        phaseTracker.RunPhaseAI();

        return false;
    }

    #endregion

    #region Phases

    private PhaseState PhaseIntro => new PhaseState(Phase_Intro);
    private PhaseState PhaseOne => new PhaseState(Phase_One, Phase_EnterOne);
    private PhaseState PhaseTwo => new PhaseState(Phase_Two, Phase_EnterTwo);
    private PhaseState PhaseIntermission => new PhaseState(Phase_Intermission);
    private PhaseState PhaseEnraged => new PhaseState(Phase_Enraged, Phase_EnterEnraged);

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

    private void Phase_EnterOne()
    {
        var sideL = () => Attack_SideDash(-1, 20, 30, 60);
        var sideR = () => Attack_SideDash(1, 20, 30, 60);
        var dash = () => Attack_Dash(20, 30, 60);
        var singlePillar = () => Attack_BeePillar(0f, 0, 10f, 0.5f);
        var spreadPillar = () => Attack_BeePillar(500f, 3, 10f, 0.5f);

        AttackManager.SetAttackPattern([
            new AttackState(sideL, 0),
            new AttackState(sideR, 60),
            new AttackState(singlePillar, 30),
            new AttackState(dash, 0),
            new AttackState(dash, 0),
            new AttackState(dash, 0),
            new AttackState(Attack_Reposition, 120),
            new AttackState(Attack_BeeWave, 120),
            new AttackState(spreadPillar, 30),
            new AttackState(sideL, 0),
            new AttackState(sideR, 60),
        ]);
    }


    private void Phase_One()
    {
        if (AttackManager.AiTimer > 0 && !AttackManager.CountUp)
        {
            if (Npc.GetLifePercent() <= 0.60f)
            {
                phaseTracker.NextPhase();
                AttackManager.Reset();
            }

            HoverAbove(10f, 0.3f, 250f);
            return;
        }

        AttackManager.RunAttackPattern();
    }

    private void Phase_EnterTwo()
    {
        var sideL = () => Attack_SideDash(-1, 20, 30, 45);
        var sideR = () => Attack_SideDash(1, 20, 30, 45);
        var dash = () => Attack_Dash(20, 30, 45);
        var ring = () => Attack_BeeRing((30 + 45) * 3);
        var spreadPillar = () => Attack_BeePillar(500f, 3, 10f, 0.5f);

        AttackManager.SetAttackPattern([
            new AttackState(sideL, 0),
            new AttackState(sideR, 60),
            new AttackState(Attack_HiveLaunch, 60),
            new AttackState(Attack_Reposition, 120),
            new AttackState(Attack_BeeWave, 120),
            new AttackState(spreadPillar, 30),
            new AttackState(sideL, 0),
            new AttackState(sideR, 60),
            new AttackState(ring, 60),
            new AttackState(dash, 0),
            new AttackState(dash, 0),
            new AttackState(dash, 60),
        ]);
    }


    private void Phase_Two()
    {
        if (AttackManager.AiTimer > 0 && !AttackManager.CountUp)
        {
            HoverAbove(10f, 0.3f, 250f);

            if (Npc.GetLifePercent() <= 0.25f && !doneIntermission)
            {
                AttackManager.Reset();
                phaseTracker.ChangeState(PhaseIntermission);
            }

            return;
        }

        AttackManager.RunAttackPattern();
    }

    private AcidAnimation? intermissionAnimation;

    private AcidAnimation PrepareIntermissionAnimation()
    {
        var anim = new AcidAnimation();

        anim.AddInstantEvent(0, () =>
        {
            Npc.velocity = Vector2.Zero;
            anim.Data.Set<Vector2>("npcPos", Npc.position);

            SoundEngine.PlaySound(ScreechSound, Npc.Center);
            ScreenShakeSystem.StartShakeAtPoint(Npc.Center, 10f);

            new InternalCircleParticle(Npc.Center - new Vector2(0f, Npc.Size.Y * 0.25f), Vector2.Zero, 0f, Color.White,
                30)
            {
                OnUpdate = particle =>
                {
                    var scaleEase = EasingHelper.QuadOut(particle.LifetimeRatio);
                    particle.Scale = Vector2.Lerp(Vector2.Zero, Vector2.One * 4f, scaleEase);

                    var fadeEase = EasingHelper.ExpIn(particle.LifetimeRatio);
                    particle.Opacity = MathHelper.Lerp(0.75f, 0f, fadeEase);
                }
            }.Spawn();
        });

        anim.AddSequencedEvent(30, (progress, frame) => { });

        const int shootSpeed = 10;
        anim.AddSequencedEvent(60, (progress, frame) =>
        {
            var ease = EasingHelper.QuadInOut(progress);
            var angle = MathHelper.Lerp(0f, MathHelper.TwoPi, ease) - MathHelper.PiOver2;
            var vel = angle.ToRotationVector2() * 10f;
            ShootHive((frame % shootSpeed) / (float)shootSpeed, frame % shootSpeed, vel);
        });

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

    private void Phase_Intermission()
    {
        intermissionAnimation ??= PrepareIntermissionAnimation();
        var animationDone = intermissionAnimation.RunAnimation();
        if (!animationDone) return;

        doneIntermission = true;
        AttackManager.Reset();
        phaseTracker.ResumePhase();
    }

    private void Phase_EnterEnraged()
    {
        // She is speed.
        var sideL = () => Attack_SideDash(side: -1, speed: 50, dashLength: 15, dashAtTime: 30);
        var sideR = () => Attack_SideDash(side: 1, speed: 50, dashLength: 15, dashAtTime: 30);
        var dash = () => Attack_Dash(speed: 50, dashLength: 15, dashAtTime: 30);
        var spreadPillar = () => Attack_BeePillar(spacing: 500f, pillarsToEachSide: 3, 50f, 1.2f);
        var hiveLaunch = () => Attack_HiveLaunch();
        var ring = () => Attack_BeeRing(240);
        var halt = () =>
        {
            Npc.velocity = Vector2.Zero;
            return true;
        };

        AttackManager.SetAttackPattern([
            new AttackState(sideL, 0),
            new AttackState(sideR, 0),
            new AttackState(hiveLaunch, 15),
            new AttackState(dash, 0), new AttackState(halt, 0),
            new AttackState(dash, 0), new AttackState(halt, 0),
            new AttackState(dash, 0),
            new AttackState(Attack_Reposition, 15),
            new AttackState(spreadPillar, 0),
            new AttackState(sideL, 0),
            new AttackState(sideR, 0),
            new AttackState(sideL, 0), new AttackState(halt, 0),
            new AttackState(ring, 60),
            new AttackState(dash, 0), new AttackState(halt, 0),
            new AttackState(dash, 0), new AttackState(halt, 0),
            new AttackState(dash, 0), new AttackState(halt, 0),
        ]);
    }


    private void Phase_Enraged()
    {
        if (AttackManager.AiTimer > 0 && !AttackManager.CountUp)
        {
            HoverAbove(50f, 1.2f, 250f);
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

    private void SummonFx()
    {
        SoundEngine.PlaySound(SummonBeesSound, Npc.Center);

        new InternalCircleParticle(Npc.Center - new Vector2(0f, Npc.Size.Y * 0.25f), Vector2.Zero, 0f, Color.White, 30)
        {
            OnUpdate = particle =>
            {
                var scaleEase = EasingHelper.QuadOut(particle.LifetimeRatio);
                particle.Scale = Vector2.Lerp(Vector2.Zero, Vector2.One * 4f, scaleEase);

                var fadeEase = EasingHelper.ExpIn(particle.LifetimeRatio);
                particle.Opacity = MathHelper.Lerp(0.75f, 0f, fadeEase);
            }
        }.Spawn();
    }

    private bool Attack_Reposition()
    {
        var target = TargetPlayer.Center - new Vector2(0, 250);
        if (Npc.Distance(target) > 250) Teleport(target, 5f);
        return true;
    }

    private bool Attack_SideDash(int side, float speed, int dashLength, int dashAtTime)
    {
        ref var dashStartX = ref Npc.localAI[0];

        var dashOptions = new DashOptions
        {
            DashLength = dashLength,
            DashSpeed = speed,
            TrackTime = 0,
            DashAtTime = dashAtTime,
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

    private bool Attack_Dash(float speed, int dashLength, int dashAtTime)
    {
        var dashOptions = new DashOptions
        {
            DashLength = dashLength,
            DashSpeed = speed,
            TrackTime = dashAtTime / 2,
            DashAtTime = dashAtTime,
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

    private bool Attack_BeePillar(float spacing, int pillarsToEachSide, float hoverSpeed, float hoverAccel)
    {
        AttackManager.CountUp = true;

        ref var centerX = ref Npc.localAI[0];
        ref var centerY = ref Npc.localAI[1];

        HoverAbove(hoverSpeed, hoverAccel, 250);

        if (AttackManager.AiTimer == 0)
        {
            SummonFx();

            var center = Main.player[Npc.target].Center;
            centerX = center.X;
            centerY = center.Y;

            var pos = center;
            pos.Y = Utilities.FindGroundVertical(pos.ToTileCoordinates()).ToWorldCoordinates().Y;

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

    private bool Attack_BeeRing(int ringTime)
    {
        SummonFx();

        NewBeeCircle(Npc.Center, 0f, 60 + ringTime);
        return true;
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

    private Projectile? NewBeeCircle(Vector2 position, float rotation, int lifetime)
    {
        if (!AcidUtils.IsServer()) return null;
        return BeeCircle.Create(Npc.GetSource_FromAI(), position, rotation, Npc.damage, 3,
            lifetime, Npc.whoAmI);
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
            {
                npc.frame.Y = 0;
            }
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