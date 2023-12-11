using System;
using System.IO;
using System.Linq;
using AcidicBosses.Common.Effects;
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

public class SkeletronHeadOverride : AcidicNPCOverride
{
    // Set this to the boss to override
    protected override int OverriddenNpc => NPCID.SkeletronHead;

    #region Phases

    public enum PhaseState
    {
        Intro,
        One,
        Transition,
        Two
    }

    private PhaseState CurrentPhase
    {
        get => (PhaseState) Npc.ai[1];
        set => Npc.ai[1] = (float) value;
    }

    private Action CurrentAi => CurrentPhase switch
    {
        PhaseState.Intro => Phase_Intro,
        PhaseState.One => Phase_One,
        PhaseState.Transition => Phase_Transition,
        PhaseState.Two => Phase_Two,
        _ => throw new UsageException(
            $"The PhaseState {CurrentPhase} and does not have an ai")
    };

    #endregion

    #region Attacks

    private enum Attack
    {
        SlapPlayer,
        AlternatingSlaps,
        Spin,
        RisingSkulls,
        MuramasaBarrage,
        ShadowflameBurst,
    }

    private Attack[] phaseOneAp =
    {
        Attack.SlapPlayer,
        Attack.SlapPlayer,
        Attack.Spin,
        Attack.AlternatingSlaps,
        Attack.RisingSkulls,
    };

    private Attack[] phaseTwoAp =
    {
        Attack.Spin,
        Attack.MuramasaBarrage,
        Attack.AlternatingSlaps,
        Attack.ShadowflameBurst,
        Attack.RisingSkulls,
    };

    private Attack[] CurrentAttackPattern => CurrentPhase switch
    {
        PhaseState.One => phaseOneAp,
        PhaseState.Two => phaseTwoAp,
        _ => throw new UsageException(
            $"BoC is in the PhaseState {CurrentPhase} and does not have an attack pattern")
    };

    private int CurrentAttackIndex
    {
        get => (int) Npc.ai[2];
        set => Npc.ai[2] = value;
    }

    private Attack CurrentAttack => CurrentAttackPattern[CurrentAttackIndex];

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

    private void NextAttack()
    {
        CurrentAttackIndex = (CurrentAttackIndex + 1) % CurrentAttackPattern.Length;
    }

    #endregion

    #region AI

    private bool countUpTimer = false;

    private bool isFleeing = false;

    private int AiTimer
    {
        get => (int) Npc.ai[0];
        set => Npc.ai[0] = value;
    }

    public override void OnFirstFrame(NPC npc)
    {
        CurrentPhase = PhaseState.Intro;
        AiTimer = 0;
        Npc.damage = 0;
    }

    public override bool AcidAI(NPC npc)
    {
        if (AiTimer > 0 && !countUpTimer)
            AiTimer--;

        // Flee when no players are alive
        var target = Main.player[npc.target];
        if (IsTargetGone(npc) && !isFleeing)
        {
            npc.TargetClosest();
            target = Main.player[npc.target];
            if (IsTargetGone(npc))
            {
                countUpTimer = true;
                isFleeing = true;
                AiTimer = 0;
            }
        }

        if (isFleeing) FleeAI();
        else if (Main.dayTime)
        {
            DungeonGuardianAI();
            return false;
        }
        else CurrentAi.Invoke();

        if (countUpTimer)
            AiTimer++;

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

    #region Phase AIs

    private void Phase_Intro()
    {
        countUpTimer = true;
        Npc.dontTakeDamage = true;
        Npc.damage = 0;

        switch (AiTimer)
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
                countUpTimer = false;
                Npc.dontTakeDamage = false;
                Npc.damage = Npc.defDamage;
                AiTimer = 0;
                CurrentPhase = PhaseState.One;
                
                var punch = new PunchCameraModifier(Npc.Center, (Main.rand.NextFloat() * ((float)Math.PI * 2f)).ToRotationVector2(), 20f, 6f, 20, 1000f, FullName);
                Main.instance.CameraModifiers.Add(punch);
                SoundEngine.PlaySound(SoundID.Roar, Npc.Center);
                break;
        }
    }

    private void Phase_One()
    {
        if (Npc.GetLifePercent() <= 0.6f && AiTimer == 0)
        {
            CurrentPhase = PhaseState.Transition;
            AiTimer = 0;
            CurrentAttackIndex = 0;
            return;
        }
        
        if (AiTimer > 0 && !countUpTimer)
        {
            Attack_LockAbove();
            return;
        }

        var isDone = false;
        switch (CurrentAttack)
        {
            case Attack.SlapPlayer:
                Attack_SlapPlayer(out isDone);
                if (isDone) AiTimer = 90;
                break;
            case Attack.AlternatingSlaps:
                Attack_AlternatingSlaps(out isDone);
                if (isDone) AiTimer = 90;
                break;
            case Attack.Spin:
                Attack_Spin(out isDone, 7.5f);
                if (isDone) AiTimer = 120;
                break;
            case Attack.RisingSkulls:
                Attack_RisingSkulls(out isDone, 20f, 150f);
                if (isDone) AiTimer = 60;
                break;
        }

        if (isDone) NextAttack();
    }

    private void Phase_Transition()
    {
        countUpTimer = true;
        
        if (AiTimer == 0)
        {
            var punch = new PunchCameraModifier(Npc.Center, Main.rand.NextVector2Unit(), 10f, 12f, 20, 1000f, FullName);
            Main.instance.CameraModifiers.Add(punch);
            SoundEngine.PlaySound(SoundID.Roar);
            Npc.velocity = Vector2.Zero;
            
            EffectsManager.ShockwaveActive(Npc.Center, 0.15f, 0.25f, Color.Red);

            CurrentHandState = HandState.LockHead;
        }

        if (AiTimer < 30)
        {
            var shockT = AiTimer / 30f;
            EffectsManager.ShockwaveProgress(shockT);
        }
        
        if (AiTimer == 30)
        {
            EffectsManager.ShockwaveKill();
        }

        // Spin
        if (AiTimer <= 300)
        {
            const float dAngle = MathHelper.TwoPi / 30f;
            Npc.rotation = MathHelper.WrapAngle(Npc.rotation + dAngle);
        }

        if (AiTimer >= 300)
        {
            countUpTimer = false;
            AiTimer = 0;
            CurrentPhase = PhaseState.Two;
            CurrentHandState = HandState.LockSide;
            ResetExtraAI();
            Npc.rotation = 0f;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        
        if (AiTimer % 20 == 0)
        {
            Attack_Muramasa(20, 400);
        }
        
        if (AiTimer % 60 == 0)
        {
            Attack_ShadowflameBurst(8);
        }
    }

    private void Phase_Two()
    {
        if (AiTimer > 0 && !countUpTimer)
        {
            Attack_LockAbove();
            return;
        }

        var isDone = false;
        switch (CurrentAttack)
        {
            case Attack.SlapPlayer:
                Attack_SlapPlayer(out isDone);
                if (isDone) AiTimer = 90;
                break;
            case Attack.AlternatingSlaps:
                Attack_AlternatingSlaps(out isDone);
                if (isDone) AiTimer = 60;
                break;
            case Attack.Spin:
                Attack_Spin(out isDone, 10f);
                if (isDone) AiTimer = 60;
                break;
            case Attack.RisingSkulls:
                Attack_RisingSkulls(out isDone, 20f, 125f);
                if (isDone) AiTimer = 90;
                break;
            case Attack.MuramasaBarrage:
                Attack_MuramasaBarrage(out isDone);
                if (isDone) AiTimer = 60;
                break;
            case Attack.ShadowflameBurst:
                Attack_ShadowflameBurst(10);
                AiTimer = 30;
                isDone = true;
                break;
        }

        if (isDone) NextAttack();
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

    private void Attack_SlapPlayer(out bool isDone)
    {
        countUpTimer = true;
        CurrentHandState = HandState.Slap;
        isDone = AiTimer >= SkeletronHandOverride.SlapLength;

        Attack_LockAbove();

        if (isDone)
        {
            CurrentHandState = HandState.HoverSide;
            if(CurrentPhase == PhaseState.Two) Attack_HomingSkulls();
            countUpTimer = false;
        }
    }

    private void Attack_AlternatingSlaps(out bool isDone)
    {
        countUpTimer = true;
        CurrentHandState = HandState.AlternatingSlaps;
        isDone = AiTimer >= 300;

        Attack_HoverAbove();

        if (CurrentPhase == PhaseState.Two)
        {
            if (AiTimer == 150) Attack_ShadowflameBurst(6);
        }

        if (isDone)
        {
            CurrentHandState = HandState.HoverSide;
            countUpTimer = false;
        }
    }

    private void Attack_Spin(out bool isDone, float speed)
    {
        const int length = 60 * 5;
        const float dAngle = MathHelper.TwoPi / 30f;

        countUpTimer = true;
        CurrentHandState = HandState.NoInteractLockHead;
        
        if (AiTimer >= length)
        {
            isDone = Attack_SpinRecovery(length, 10);
            Npc.rotation = 0;
            CurrentHandState = HandState.HoverSide;
            Npc.defense = Npc.defDefense;
            return;
        }

        if (AiTimer == 0)
        {
            TargetRandom();
            
            SoundEngine.PlaySound(SoundID.Roar, Npc.Center);
            // Crazy Damage
            Npc.defense = 0;
            Npc.damage *= 2;
        }
        
        if(CurrentPhase == PhaseState.Two && AiTimer % 60 == 0) Attack_HomingSkulls();

        Npc.rotation = MathHelper.WrapAngle(Npc.rotation + dAngle);
        var goal = Main.player[Npc.target].Center;
        var vel = Npc.DirectionTo(goal) * speed;
        Npc.SimpleFlyMovement(vel, 0.1f);

        isDone = false;
    }

    // Allow a bit of buffer time where skeletron does no damage in case the player is above it
    private bool Attack_SpinRecovery(int length, int recoverTime)
    {
        Attack_LockAbove();
        
        if (AiTimer < length + recoverTime)
        {
            Npc.damage = 0;
            return false;
        }

        Npc.damage = Npc.defDamage;
        countUpTimer = false;
        return true;
    }

    private void Attack_RisingSkulls(out bool isDone, float speed, float spacing)
    {
        const int length = 120;
        const int indicatorLength = 30;

        const float distanceBelowPlayer = 1000f;
        const int skullsToEachSide = 12;

        ref var centerX = ref Npc.localAI[0];
        ref var centerY = ref Npc.localAI[1];

        countUpTimer = true;

        Attack_LockAbove();

        isDone = false;
        if (AiTimer >= length)
        {
            isDone = true;
            CurrentHandState = HandState.HoverSide;
            
            countUpTimer = false;
        }

        if (AiTimer == 0)
        {
            SoundEngine.PlaySound(SoundID.ForceRoarPitched, Npc.Center);
            CurrentHandState = HandState.LockHead;
        }

        if (AiTimer == indicatorLength && CurrentPhase == PhaseState.Two)
        {
            CurrentHandState = HandState.Slap;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        // Start Indicating
        if (AiTimer == 0)
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
        if (AiTimer == indicatorLength)
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
    }

    private void Attack_Muramasa(float speed, float spawnDist)
    {
        var player = RandomTargetablePlayer();

        // Predict by .5 seconds, not perfect prediction as this is half the delay
        var target = player.Center + (player.velocity * 30);
            
        var pos = target + Main.rand.NextVector2Unit() * spawnDist;
        var rotation = pos.DirectionTo(target);

        NewMuramasaLine(pos, rotation.ToRotation());
        NewMuramasa(pos, rotation * speed);
    }
    
    private void Attack_MuramasaBarrage(out bool isDone)
    {
        isDone = false;
        countUpTimer = true;

        const int shots = 10;
        const int delay = 20;
        const float distance = 400f;
        const float speed = 20f;

        ref var shotsDone = ref ExtraAI[0];

        if (AiTimer == 0)
        {
            ExtraAI[0] = 0;
        }

        if (AiTimer > delay * shots + 30)
        {
            isDone = true;
            countUpTimer = false;
            ResetExtraAI();
        }
        
        Attack_LockAbove();

        

        if (AiTimer % delay == 0 && shotsDone < shots)
        {
            shotsDone++;

            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            
            Attack_Muramasa(speed, distance);
        }
    }

    private void Attack_HomingSkulls()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        const float speed = 2f;
        const float dAngle = MathHelper.TwoPi / 6f;
        var target = Main.player[Npc.target].Center;

        for (var i = -1; i < 2; i++)
        {
            var dir = Npc.DirectionTo(target).ToRotation();
            dir = MathHelper.WrapAngle(dir + dAngle * i);
            NewHomingSkull(Npc.Center,  dir.ToRotationVector2() * speed);
        }
    }

    private void Attack_ShadowflameBurst(int bolts)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        const float speed = 3f;
        
        var step = MathHelper.TwoPi / bolts;
        for (var i = 0; i < bolts; i++)
        {
            var angle = i * step;
            var vel = angle.ToRotationVector2() * speed;

            NewShadowflame(Npc.Center, vel);
        }
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
        return Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), position, Vector2.Zero,
            ModContent.ProjectileType<CursedSkullLine>(), 0, 0, ai0: rotation);
    }
    
    private Projectile NewHomingSkull(Vector2 position, Vector2 velocity)
    {
        return Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), position, velocity,
            ProjectileID.Skull, Npc.damage / 8, 3);
    }

    private Projectile NewMuramasaLine(Vector2 position, float rotation)
    {
        var proj = Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), position, Vector2.Zero, 
            ModContent.ProjectileType<MurasamaLine>(), 0, 0, ai0: rotation);
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

    #endregion

    #region Drawing

    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
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
}