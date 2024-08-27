using System;
using System.IO;
using AcidicBosses.Common.Configs;
using AcidicBosses.Common.Effects;
using AcidicBosses.Content.Particles;
using AcidicBosses.Content.Particles.Animated;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Content.Projectiles;
using AcidicBosses.Core.StateManagement;
using AcidicBosses.Helpers;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Content.Bosses.Skeletron;

public class SkeletronHead : AcidicNPCOverride
{
    // Set this to the boss to override
    protected override int OverriddenNpc => NPCID.SkeletronHead;
    
    protected override bool BossEnabled => BossToggleConfig.Get().EnableSkeletron;
    
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        NPCID.Sets.TrailingMode[NPCID.SkeletronHead] = 3;
    }
    
    #region AI
    
    public enum HandState
    {
        HoverSide,
        LockSide,
        LockHead,
        Slap,
        AlternatingSlaps,
        NoInteractLockHead,
    }

    private HandState CurrentHandState
    {
        get => (HandState) Npc.ai[3];
        set => Npc.ai[3] = (float) value;
    }

    private PhaseTracker phaseTracker;

    private bool useAfterimages = false;

    private bool isFleeing = false;

    public override void OnFirstFrame(NPC npc)
    {
        phaseTracker = new PhaseTracker([
            PhaseIntro,
            PhaseOne,
            PhaseTransition,
            PhaseTwo
        ]);
        
        AttackManager.Reset();
        Npc.damage = 0;
    }

    public override bool AcidAI(NPC npc)
    {
        // Flee when no players are alive
        var target = Main.player[npc.target];
        if (IsTargetGone(npc) && !isFleeing)
        {
            npc.TargetClosest();
            target = Main.player[npc.target];
            if (IsTargetGone(npc))
            {
                AttackManager.CountUp = true;
                isFleeing = true;
                AttackManager.AiTimer = 0;
            }
        }

        if (isFleeing) FleeAI();
        else if (Main.dayTime)
        {
            DungeonGuardianAI();
            return false;
        }
        else phaseTracker.RunPhaseAI();

        return false;
    }

    private void FleeAI()
    {
        // Put Flee Behavior here
        Npc.active = false;
    }

    private void DungeonGuardianAI()
    {
        const float dAngle = MathHelper.TwoPi / 30f;
        Npc.damage = 1000;
        Npc.defense = 9999;

        Npc.rotation = MathHelper.WrapAngle(Npc.rotation + dAngle);

        var goal = Main.player[Npc.target].Center;
        var vel = Npc.DirectionTo(goal) * 25;
        Npc.SimpleFlyMovement(vel, 0.25f);
    }
    
    #endregion

    #region Phase AIs

    private PhaseState PhaseIntro => new(Phase_Intro);

    private void Phase_Intro()
    {
        AttackManager.CountUp = true;
        Npc.dontTakeDamage = true;
        Npc.damage = 0;

        switch (AttackManager.AiTimer)
        {
            case < 120:
            {
                const float dAngle = MathHelper.TwoPi * 2f / 120f;
                Npc.velocity.Y = -5f;

                // Spin
                Npc.rotation = MathHelper.WrapAngle(Npc.rotation + dAngle);
                break;
            }
            case 120:
                Npc.velocity = Vector2.Zero;
                Npc.rotation = 0f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NewHand(Npc.Center, -1);
                    NewHand(Npc.Center, 1);
                    CurrentHandState = HandState.LockHead;
                    NetSync(Npc);
                }

                break;
            case 150:
                Npc.dontTakeDamage = false;
                Npc.damage = Npc.defDamage;
                
                AttackManager.Reset();
                phaseTracker.NextPhase();
                
                var punch = new PunchCameraModifier(Npc.Center, (Main.rand.NextFloat() * ((float)Math.PI * 2f)).ToRotationVector2(), 20f, 6f, 20, 1000f, FullName);
                Main.instance.CameraModifiers.Add(punch);
                SoundEngine.PlaySound(SoundID.Roar, Npc.Center);
                break;
        }
    }

    private PhaseState PhaseOne => new(Phase_One, EnterPhaseOne);

    private void EnterPhaseOne()
    {
        var slap = new AttackState(() => Attack_SlapPlayer(false), 90);
        var alternatingSlaps = new AttackState(() => Attack_AlternatingSlaps(false), 90);
        var spin = new AttackState(() => Attack_Spin(7.5f, false), 120);
        var risingSkulls = new AttackState(() => Attack_RisingSkulls(20f, 150f, false), 60);
        
        AttackManager.SetAttackPattern([
            slap,
            slap,
            spin,
            alternatingSlaps,
            risingSkulls,
        ]);
    }

    private void Phase_One()
    {
        if (Npc.GetLifePercent() <= 0.6f && AttackManager.AiTimer == 0)
        {
            phaseTracker.NextPhase();
            AttackManager.Reset();
            return;
        }
        
        if (AttackManager.AiTimer > 0 && !AttackManager.CountUp)
        {
            Attack_LockAbove();
            return;
        }

        AttackManager.RunAttackPattern();
    }

    private PhaseState PhaseTransition => new(Phase_Transition);
    
    private void Phase_Transition()
    {
        AttackManager.CountUp = true;
        
        if (AttackManager.AiTimer == 0)
        {
            ScreenShakeSystem.StartShakeAtPoint(Npc.Center, 10f);
            SoundEngine.PlaySound(SoundID.Roar);
            Npc.velocity = Vector2.Zero;
            
            CurrentHandState = HandState.LockHead;
        }

        if (AttackManager.AiTimer < 30)
        {
            var shockT = AttackManager.AiTimer / 30f;
            EffectsManager.ShockwaveActivate(Npc.Center, 0.15f, 0.25f, Color.Red, shockT);
        }

        // Spin
        if (AttackManager.AiTimer <= 300)
        {
            const float dAngle = MathHelper.TwoPi / 30f;
            Npc.rotation = MathHelper.WrapAngle(Npc.rotation + dAngle);
        }

        if (AttackManager.AiTimer >= 300)
        {
            AttackManager.Reset();
            phaseTracker.NextPhase();
            CurrentHandState = HandState.LockSide;
            ResetExtraAI();
            Npc.rotation = 0f;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        
        if (AttackManager.AiTimer % 30 == 0)
        {
            Attack_Muramasa(20, 400);
        }
        
        if (AttackManager.AiTimer % 90 == 0)
        {
            Attack_WaterboltBurst(4);
        }
    }

    private PhaseState PhaseTwo => new(Phase_Two, EnterPhaseTwo);

    private void EnterPhaseTwo()
    {
        var slap = new AttackState(() => Attack_SlapPlayer(true), 90);
        var alternatingSlaps = new AttackState(() => Attack_AlternatingSlaps(true), 60);
        var spin = new AttackState(() => Attack_Spin(10f, true), 60);
        var risingSkulls = new AttackState(() => Attack_RisingSkulls( 20f, 125f, true), 90);
        var muramasaBarrage = new AttackState(Attack_MuramasaBarrage, 60);
        
        AttackManager.SetAttackPattern([
            spin,
            muramasaBarrage,
            alternatingSlaps,
            risingSkulls
        ]);
    }

    private void Phase_Two()
    {
        if (AttackManager.AiTimer > 0 && !AttackManager.CountUp)
        {
            Attack_LockAbove();
            return;
        }

        AttackManager.RunAttackPattern();
    }

    #endregion

    #region Attack Behaviors

    private void Attack_HoverAbove()
    {
        var target = Main.player[Npc.target];
        var goal = target.Center;
        goal.Y -= 250;

        Npc.SimpleFlyMovement(Npc.DirectionTo(goal) * 10f, 0.5f);

        var offset = Npc.Center - goal;
        var lerp = MathF.Min(MathF.Abs(offset.X) / 250, 10);
        if(offset.X > 0)
            Npc.rotation = MathHelper.Lerp(0, -MathHelper.PiOver4, lerp);
        else Npc.rotation = MathHelper.Lerp(0, MathHelper.PiOver4, lerp);
    }

    private void Attack_LockAbove()
    {
        var target = Main.player[Npc.target];
        var goal = target.Center;
        goal.Y -= 250;

        // Aggressive lerp
        Npc.velocity = Vector2.Lerp(Npc.velocity, Vector2.Zero, 0.075f);
        Npc.Center = Vector2.Lerp(Npc.Center, goal, 0.075f);
        
        var offset = Npc.Center - goal;
        var lerp = MathF.Min(MathF.Abs(offset.X) / 250, 1);
        if(offset.X > 0)
            Npc.rotation = MathHelper.Lerp(0, -MathHelper.PiOver4, lerp);
        else Npc.rotation = MathHelper.Lerp(0, MathHelper.PiOver4, lerp);
    }

    private bool Attack_SlapPlayer(bool spawnHomingSkulls)
    {
        AttackManager.CountUp = true;
        CurrentHandState = HandState.Slap;
        var isDone = AttackManager.AiTimer >= SkeletronHand.SlapLength;

        Attack_LockAbove();

        if (isDone)
        {
            CurrentHandState = HandState.HoverSide;
            if(spawnHomingSkulls) Attack_HomingSkulls();
            AttackManager.CountUp = false;
        }

        return isDone;
    }

    private bool Attack_AlternatingSlaps(bool burst)
    {
        AttackManager.CountUp = true;
        CurrentHandState = HandState.AlternatingSlaps;
        var isDone = AttackManager.AiTimer >= 300;

        Attack_HoverAbove();

        if (AttackManager.AiTimer == 150 && burst) Attack_WaterboltBurst(8);

        if (isDone)
        {
            CurrentHandState = HandState.HoverSide;
            AttackManager.CountUp = false;
        }

        return isDone;
    }

    private bool Attack_Spin(float speed, bool spawnHomingSkulls)
    {
        const int length = 60 * 5;
        const float dAngle = MathHelper.TwoPi / 30f;

        AttackManager.CountUp = true;
        CurrentHandState = HandState.NoInteractLockHead;
        useAfterimages = true;
        
        if (AttackManager.AiTimer >= length)
        {
            var isDone = Attack_SpinRecovery(length, 10);
            Npc.rotation = 0;
            CurrentHandState = HandState.HoverSide;
            Npc.defense = Npc.defDefense;
            return isDone;
        }

        if (AttackManager.AiTimer == 0)
        {
            TargetRandom();
            
            SoundEngine.PlaySound(SoundID.Roar, Npc.Center);
            // Crazy Damage
            Npc.defense = 0;
            Npc.damage *= 2;
        }
        
        if(spawnHomingSkulls && AttackManager.AiTimer % 60 == 0) Attack_HomingSkulls();

        Npc.rotation = MathHelper.WrapAngle(Npc.rotation + dAngle);
        var goal = Main.player[Npc.target].Center;
        var vel = Npc.DirectionTo(goal) * speed;
        Npc.SimpleFlyMovement(vel, 0.1f);
        
        return false;
    }

    // Allow a bit of buffer time where skeletron does no damage in case the player is above it
    private bool Attack_SpinRecovery(int length, int recoverTime)
    {
        if (AttackManager.AiTimer == length)
        {
            var startPos = Npc.Center;
            var target = Main.player[Npc.target];
            var goal = target.Center;
            goal.Y -= 250;
            var awayDir = Npc.DirectionTo(goal);
            var distanceToGoal = Npc.Distance(goal);
            
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NewAfterimage(startPos, goal, 60);
            }
            
            Npc.Center = goal;
            Npc.velocity = awayDir * 20f;

            new BigSmokeDisperseParticle(startPos, -awayDir * 10f, awayDir.ToRotation() - MathHelper.PiOver2, Color.Gray, 30).Spawn();
            SoundEngine.PlaySound(SoundID.Item8, Npc.Center);
        }
        
        Attack_LockAbove();
        
        if (AttackManager.AiTimer < length + recoverTime)
        {
            Npc.damage = 0;
            return false;
        }

        Npc.damage = Npc.defDamage;
        AttackManager.CountUp = false;
        useAfterimages = false;
        return true;
    }

    private bool Attack_RisingSkulls(float speed, float spacing, bool shouldSlap)
    {
        const int length = 120;
        const int indicatorLength = 30;

        const int skullsToEachSide = 12;

        ref var centerX = ref Npc.localAI[0];
        ref var centerY = ref Npc.localAI[1];

        AttackManager.CountUp = true;

        Attack_LockAbove();

        var isDone = false;
        if (AttackManager.AiTimer >= length)
        {
            isDone = true;
            CurrentHandState = HandState.HoverSide;
            
            AttackManager.CountUp = false;
        }

        if (AttackManager.AiTimer == 0)
        {
            var center = Main.player[Npc.target].Center;
            centerX = center.X;
            centerY = center.Y;
            
            SoundEngine.PlaySound(SoundID.ForceRoarPitched, Npc.Center);
            CurrentHandState = HandState.LockHead;
            
            if (Main.netMode == NetmodeID.MultiplayerClient) return isDone;

            var pos = center;
            pos.Y = Utilities.FindGroundVertical(pos.ToTileCoordinates()).ToWorldCoordinates().Y;
            NewCursedSkullLine(pos, -MathHelper.PiOver2, indicatorLength * 2);
            
            for (var i = 1; i <= skullsToEachSide; i++)
            {
                var leftPos = center;
                var rightPos = center;
                
                leftPos.X -= spacing * i;
                rightPos.X += spacing * i;

                leftPos.Y = Utilities.FindGroundVertical(leftPos.ToTileCoordinates()).ToWorldCoordinates().Y;
                rightPos.Y = Utilities.FindGroundVertical(rightPos.ToTileCoordinates()).ToWorldCoordinates().Y;
                
                NewCursedSkullLine(leftPos, -MathHelper.PiOver2, indicatorLength * 2);
                NewCursedSkullLine(rightPos, -MathHelper.PiOver2, indicatorLength * 2);
            }
        }

        if (AttackManager.AiTimer == indicatorLength)
        {
            var center = new Vector2(centerX, centerY);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                var pos = center;
                pos.Y = Utilities.FindGroundVertical(pos.ToTileCoordinates()).ToWorldCoordinates().Y;
            
                NewCursedSkull(pos, Vector2.UnitY * -speed);
            }
            
            for (var i = 1; i <= skullsToEachSide; i++)
            {
                var leftPos = center;
                var rightPos = center;
                
                leftPos.X -= spacing * i;
                rightPos.X += spacing * i;

                leftPos.Y = Utilities.FindGroundVertical(leftPos.ToTileCoordinates()).ToWorldCoordinates().Y;
                rightPos.Y = Utilities.FindGroundVertical(rightPos.ToTileCoordinates()).ToWorldCoordinates().Y;

                new FireSmokeParticle(leftPos, Vector2.Zero, 0f, Color.Gray, 30).Spawn();
                new FireSmokeParticle(rightPos, Vector2.Zero, 0f, Color.Gray, 30).Spawn();

                if (Main.netMode == NetmodeID.MultiplayerClient) continue;
                
                NewCursedSkull(leftPos, Vector2.UnitY * -speed);
                NewCursedSkull(rightPos, Vector2.UnitY * -speed);
            }
            
            if (shouldSlap) CurrentHandState = HandState.Slap;
        }

        return isDone;
    }

    private bool Attack_Muramasa(float speed, float spawnDist)
    {
        var player = RandomTargetablePlayer();

        // Predict by .5 seconds, not perfect prediction as this is half the delay
        var target = player.Center + (player.velocity * 30);
            
        var pos = target + Main.rand.NextVector2Unit() * spawnDist;
        var rotation = pos.DirectionTo(target);

        NewMuramasaLine(pos, rotation.ToRotation());
        NewMuramasa(pos, rotation * speed);

        return true;
    }
    
    private bool Attack_MuramasaBarrage()
    {
        var isDone = false;
        AttackManager.CountUp = true;

        const int shots = 10;
        const int delay = 20;
        const float distance = 400f;
        const float speed = 20f;

        ref var shotsDone = ref ExtraAI[0];

        if (AttackManager.AiTimer == 0)
        {
            ExtraAI[0] = 0;
        }

        if (AttackManager.AiTimer > delay * shots + 30)
        {
            isDone = true;
            AttackManager.CountUp = false;
            ResetExtraAI();
        }
        
        Attack_LockAbove();
        
        if (AttackManager.AiTimer % delay == 0 && shotsDone < shots)
        {
            shotsDone++;

            if (Main.netMode == NetmodeID.MultiplayerClient) return isDone;
            
            Attack_Muramasa(speed, distance);
        }

        return isDone;
    }

    private bool Attack_HomingSkulls()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return true;

        const float speed = 2f;
        const float dAngle = MathHelper.TwoPi / 6f;
        var target = Main.player[Npc.target].Center;

        for (var i = -1; i < 2; i++)
        {
            var dir = Npc.DirectionTo(target).ToRotation();
            dir = MathHelper.WrapAngle(dir + dAngle * i);
            NewHomingSkull(Npc.Center,  dir.ToRotationVector2() * speed);
        }

        return true;
    }

    private bool Attack_WaterboltBurst(int bolts)
    {
        var burst = new RingBurstParticle(Npc.Center, Vector2.Zero, 0f, Color.Blue, 30);
        burst.Scale *= 2f;
        burst.Spawn();
        
        if (Main.netMode == NetmodeID.MultiplayerClient) return true;
        const float speed = 4f;
        
        var step = MathHelper.TwoPi / bolts;
        var startAngle = Main.rand.NextVector2Unit().ToRotation();
        for (var i = 0; i < bolts; i++)
        {
            var angle = i * step + startAngle;
            var vel = angle.ToRotationVector2() * speed;
            NewWaterbolt(Npc.Center, vel);
        }

        return true;
    }
    
    #endregion
    
    #region Projectiles

    private Projectile NewCursedSkull(Vector2 position, Vector2 velocity)
    {
        return ProjHelper.NewUnscaledProjectile(Npc.GetSource_FromAI(), position, velocity,
            ModContent.ProjectileType<CursedSkull>(), Npc.damage / 2, 3);
    }

    private Projectile NewCursedSkullLine(Vector2 position, float rotation, int lifetime)
    {
        return BaseLineProjectile.Create<CursedSkullLine>(Npc.GetSource_FromAI(), position, rotation, lifetime);
    }
    
    private Projectile NewHomingSkull(Vector2 position, Vector2 velocity)
    {
        return ProjHelper.NewUnscaledProjectile(Npc.GetSource_FromAI(), position, velocity,
            ProjectileID.Skull, Npc.damage / 2, 3);
    }

    private Projectile NewMuramasaLine(Vector2 position, float rotation)
    {
        var proj = BaseLineProjectile.Create<MurasamaLine>(Npc.GetSource_FromAI(), position, rotation, 120);
        return proj;
    }

    private Projectile NewMuramasa(Vector2 position, Vector2 velocity)
    {
        return ProjHelper.NewUnscaledProjectile(Npc.GetSource_FromAI(), position, velocity,
            ModContent.ProjectileType<Muramasa>(), Npc.damage, 3);
    }

    private Projectile NewWaterbolt(Vector2 position, Vector2 velocity)
    {
        return ProjHelper.NewUnscaledProjectile(Npc.GetSource_FromAI(), position, velocity,
            ModContent.ProjectileType<EvilWaterbolt>(), Npc.damage, 3);
    }
    
    private Projectile NewAfterimage(Vector2 startPos, Vector2 endPos, int lifetime)
    {
        return NpcAfterimageTrail.Create(Npc.GetSource_FromAI(), startPos, endPos, Npc.whoAmI);
    }
    
    private NPC NewHand(Vector2 position, int side)
    {
        var npc = NPC.NewNPCDirect(Npc.GetSource_FromAI(), position, NPCID.SkeletronHand);

        npc.ai[0] = side;
        npc.ai[1] = Npc.whoAmI;
        return npc;
    }

    #endregion

    #region Drawing

    public override bool AcidicDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        var drawPos = npc.Center - Main.screenPosition;
        var texture = TextureAssets.Npc[npc.type].Value;
        var origin = npc.frame.Size() * 0.5f;
        lightColor *= npc.Opacity;
        
        if (useAfterimages)
            for (var i = 1; i < npc.oldPos.Length; i++)
            {
                // All of this is heavily simplified from decompiled vanilla
                var fade = 0.5f * (10 - i) / 20f;
                var afterImageColor = Color.Multiply(lightColor, fade);

                var pos = npc.oldPos[i] + new Vector2(npc.width, npc.height) / 2f - Main.screenPosition;
                spriteBatch.Draw(texture, pos, npc.frame, afterImageColor, npc.oldRot[i], origin, npc.scale,
                    SpriteEffects.None, 0f);
            }

        spriteBatch.Draw(
            texture, drawPos,
            npc.frame, lightColor,
            npc.rotation, origin, npc.scale,
            SpriteEffects.None, 0f);

        return false;
    }

    #endregion

    public override void SendAcidAI(BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        phaseTracker.Serialize(binaryWriter);
        bitWriter.WriteBit(useAfterimages);
    }

    public override void ReceiveAcidAI(BitReader bitReader, BinaryReader binaryReader)
    {
        phaseTracker.Deserialize(binaryReader);
        useAfterimages = bitReader.ReadBit();
    }
}