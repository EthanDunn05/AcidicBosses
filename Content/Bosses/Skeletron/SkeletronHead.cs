using System;
using System.IO;
using AcidicBosses.Common.Effects;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Core.StateManagement;
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
            var punch = new PunchCameraModifier(Npc.Center, Main.rand.NextVector2Unit(), 10f, 12f, 20, 1000f, FullName);
            Main.instance.CameraModifiers.Add(punch);
            SoundEngine.PlaySound(SoundID.Roar);
            Npc.velocity = Vector2.Zero;
            
            EffectsManager.ShockwaveActive(Npc.Center, 0.15f, 0.25f, Color.Red);

            CurrentHandState = HandState.LockHead;
        }

        if (AttackManager.AiTimer < 30)
        {
            var shockT = AttackManager.AiTimer / 30f;
            EffectsManager.ShockwaveProgress(shockT);
        }
        
        if (AttackManager.AiTimer == 30)
        {
            EffectsManager.ShockwaveKill();
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
        
        if (AttackManager.AiTimer % 20 == 0)
        {
            Attack_Muramasa(20, 400);
        }
        
        if (AttackManager.AiTimer % 60 == 0)
        {
            Attack_ShadowflameBurst(8);
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
        var shadowflameBurst = new AttackState(() => Attack_ShadowflameBurst(10), 30);
        
        AttackManager.SetAttackPattern([
            spin,
            muramasaBarrage,
            alternatingSlaps,
            shadowflameBurst,
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
        goal.Y -= 400;

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
        goal.Y -= 400;

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

    private bool Attack_AlternatingSlaps(bool spawnShadowflameBurst)
    {
        AttackManager.CountUp = true;
        CurrentHandState = HandState.AlternatingSlaps;
        var isDone = AttackManager.AiTimer >= 300;

        Attack_HoverAbove();

        if (spawnShadowflameBurst)
        {
            if (AttackManager.AiTimer == 150) Attack_ShadowflameBurst(6);
        }

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
        Attack_LockAbove();
        
        if (AttackManager.AiTimer < length + recoverTime)
        {
            Npc.damage = 0;
            return false;
        }

        Npc.damage = Npc.defDamage;
        AttackManager.CountUp = false;
        return true;
    }

    private bool Attack_RisingSkulls(float speed, float spacing, bool shouldSlap)
    {
        const int length = 120;
        const int indicatorLength = 30;

        const float distanceBelowPlayer = 1000f;
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
            SoundEngine.PlaySound(SoundID.ForceRoarPitched, Npc.Center);
            CurrentHandState = HandState.LockHead;
        }

        if (AttackManager.AiTimer == indicatorLength && shouldSlap)
        {
            CurrentHandState = HandState.Slap;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient) return isDone;

        // Start Indicating
        if (AttackManager.AiTimer == 0)
        {
            var center = Main.player[Npc.target].Center;
            centerX = center.X;
            centerY = center.Y;
            center.Y += distanceBelowPlayer;

            var centerSkull = NewCursedSkullLine(center, -MathHelper.PiOver2);
            centerSkull.timeLeft = indicatorLength;

            // Left
            for (var i = 1; i <= skullsToEachSide; i++)
            {
                var pos = center;
                pos.X -= spacing * i;
                var skull = NewCursedSkullLine(pos, -MathHelper.PiOver2);
                skull.timeLeft = indicatorLength;
            }

            // Right
            for (var i = 1; i <= skullsToEachSide; i++)
            {
                var pos = center;
                pos.X += spacing * i;
                var skull = NewCursedSkullLine(pos, -MathHelper.PiOver2);
                skull.timeLeft = indicatorLength;
            }
        }

        // Spawn Skulls
        if (AttackManager.AiTimer == indicatorLength)
        {
            var center = new Vector2(centerX, centerY);
            center.Y += distanceBelowPlayer;

            var centerSkull = NewCursedSkull(center, Vector2.UnitY * -speed);

            // Left
            for (var i = 1; i <= skullsToEachSide; i++)
            {
                var pos = center;
                pos.X -= spacing * i;
                var skull = NewCursedSkull(pos, Vector2.UnitY * -speed);
            }

            // Right
            for (var i = 1; i <= skullsToEachSide; i++)
            {
                var pos = center;
                pos.X += spacing * i;
                var skull = NewCursedSkull(pos, Vector2.UnitY * -speed);
            }
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

    private bool Attack_ShadowflameBurst(int bolts)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return true;
        const float speed = 3f;
        
        var step = MathHelper.TwoPi / bolts;
        for (var i = 0; i < bolts; i++)
        {
            var angle = i * step;
            var vel = angle.ToRotationVector2() * speed;

            NewShadowflame(Npc.Center, vel);
        }

        return true;
    }
    
    #endregion
    
    #region Projectiles

    private Projectile NewCursedSkull(Vector2 position, Vector2 velocity)
    {
        return Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), position, velocity,
            ModContent.ProjectileType<CursedSkull>(), Npc.damage / 2, 3);
    }

    private Projectile NewCursedSkullLine(Vector2 position, float rotation)
    {
        return BaseLineProjectile.Create<CursedSkullLine>(Npc.GetSource_FromAI(), position, rotation);
    }
    
    private Projectile NewHomingSkull(Vector2 position, Vector2 velocity)
    {
        return Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), position, velocity,
            ProjectileID.Skull, Npc.damage / 8, 3);
    }

    private Projectile NewMuramasaLine(Vector2 position, float rotation)
    {
        var proj = BaseLineProjectile.Create<MurasamaLine>(Npc.GetSource_FromAI(), position, rotation);
        proj.timeLeft = 60;
        return proj;
    }

    private Projectile NewMuramasa(Vector2 position, Vector2 velocity)
    {
        return Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), position, velocity,
            ModContent.ProjectileType<Muramasa>(), Npc.damage / 2, 3);
    }
    
    private Projectile NewShadowflame(Vector2 position, Vector2 velocity)
    {
        return Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), position, velocity,
            ProjectileID.Shadowflames, Npc.damage / 4, 3);
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
    }

    public override void ReceiveAcidAI(BitReader bitReader, BinaryReader binaryReader)
    {
        phaseTracker.Deserialize(binaryReader);
    }
}