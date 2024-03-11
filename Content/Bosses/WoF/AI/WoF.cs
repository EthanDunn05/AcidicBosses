using System;
using System.IO;
using System.Linq;
using AcidicBosses.Content.Bosses.Skeletron;
using AcidicBosses.Content.Bosses.WoF.Projectiles;
using AcidicBosses.Helpers;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace AcidicBosses.Content.Bosses.WoF.AI;

public class WoF : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.WallofFlesh;
    private int damage = 50;

    public float WallDistance
    {
        get => Npc.ai[3];
        set => Npc.ai[3] = value;
    }

    public override void SetStaticDefaults()
    {
        NPCID.Sets.NeedsExpertScaling[NPCID.WallofFlesh] = true;
    }

    public override void SetDefaults(NPC entity)
    {
        entity.damage = 0;
        entity.dontTakeDamage = true;
        entity.ShowNameOnHover = false;
    }

    public override void BossHeadSlot(NPC npc, ref int index)
    {
        index = -1;
    }

    #region Phases

    public enum PhaseState
    {
        Intro,
        One,
        MoveTransition,
        Two,
        Three
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
        PhaseState.Two => Phase_Two,
        PhaseState.MoveTransition => Phase_MoveTransition,
        PhaseState.Three => Phase_Three,
        _ => throw new UsageException(
            $"The PhaseState {CurrentPhase} and does not have an ai")
    };

    #endregion

    #region Attacks

    public enum Attack
    {
        Deathray,
        DoubleFireballBurst,
        FireballStaggeredBursts,
        Squeeze,
        SpawnBiomeMobs,
        LaserSpam,
        LaserShotgun,
        LaserWall,
    }
    
    private Attack[] phase1Ap =
    {
        Attack.LaserShotgun,
        Attack.FireballStaggeredBursts,
        Attack.Deathray,
        Attack.LaserShotgun,
        Attack.LaserWall,
    };

    private Attack[] phase2Ap =
    {
        Attack.DoubleFireballBurst,
        Attack.Squeeze,
        Attack.LaserSpam,
        Attack.LaserShotgun,
        Attack.SpawnBiomeMobs
    };
    
    private Attack[] phase3Ap =
    {
        Attack.Deathray,
        Attack.LaserShotgun,
        Attack.SpawnBiomeMobs,
        Attack.DoubleFireballBurst,
        Attack.LaserSpam,
        Attack.FireballStaggeredBursts
    };

    private Attack[] CurrentAttackPattern => CurrentPhase switch
    {
        PhaseState.One => phase1Ap,
        PhaseState.Two => phase2Ap,
        PhaseState.Three => phase3Ap,
        _ => throw new UsageException(
            $"Boss is in the PhaseState {CurrentPhase} and does not have an attack pattern")
    };

    private int CurrentAttackIndex
    {
        get => (int) Npc.ai[2];
        set => Npc.ai[2] = value;
    }

    private Attack CurrentAttack => CurrentAttackPattern[CurrentAttackIndex];

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
        WallDistance = 3000;

        Main.wofNPCIndex = Npc.whoAmI;
        Main.wofDrawAreaBottom = -1;
        Main.wofDrawAreaTop = -1;
        Npc.TargetClosest_WOF();

        Npc.position.X = Main.player[Npc.target].position.X;

        SetWoFArea();

        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        NewEye(WoFPartPosition.Left | WoFPartPosition.Top);
        NewMouth(WoFPartPosition.Left | WoFPartPosition.Center);
        NewEye(WoFPartPosition.Left | WoFPartPosition.Bottom);
        NewEye(WoFPartPosition.Right | WoFPartPosition.Top);
        NewMouth(WoFPartPosition.Right | WoFPartPosition.Center);
        NewEye(WoFPartPosition.Right | WoFPartPosition.Bottom);
    }

    public override bool AcidAI(NPC npc)
    {
        if (AiTimer > 0 && !countUpTimer)
            AiTimer--;

        // Flee when no players are alive or it is day  
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

        // Fill Arena Area
        SetWoFArea();

        if (isFleeing) FleeAI();
        else CurrentAi.Invoke();

        if (countUpTimer)
            AiTimer++;

        return false;
    }

    private void FleeAI()
    {
        // Put Flee Behavior here
        if (!HasValidConditions()) Npc.active = false;
        Npc.TargetClosest();

        Npc.velocity.X = Npc.spriteDirection * EasingHelper.QuadIn(AiTimer / 30f);
    }

    // Check if WoF is in the right conditions to be alive
    private bool HasValidConditions()
    {
        // Offscreen
        // Shouldn't be possible, but still check
        if (Npc.position.X < 160f || Npc.position.X > (Main.maxTilesX - 10) * 16)
            return false;

        return true;
    }

    #region Phase AIs

    private void Phase_Intro()
    {
        countUpTimer = true;

        switch (AiTimer)
        {
            case < 120:
                var t = AiTimer / 120f;
                t = EasingHelper.QuadInOut(t);
                WallDistance = MathHelper.Lerp(3000, 750, t);
                break;
            case 120:
                countUpTimer = false;
                AiTimer = 0;
                CurrentPhase = PhaseState.One;
                WallDistance = 750;
                break;
        }
    }
    
    private void Phase_One()
    {
        if (AiTimer > 0 && !countUpTimer)
        {
            if (Npc.GetLifePercent() < 0.6f)
            {
                CurrentPhase = PhaseState.MoveTransition;
                CurrentAttackIndex = 0;
                return;
            }
            return;
        }

        var isDone = false;
        switch (CurrentAttack)
        {
            case Attack.LaserShotgun:
                Attack_LaserShotgun(out isDone, 10, MathHelper.Pi / 2f);
                if (isDone) AiTimer = 10;
                break;
            case Attack.LaserSpam:
                Attack_LaserSpam(out isDone, 5, 15);
                if (isDone) AiTimer = 30;
                break;
            case Attack.LaserWall:
                Attack_LaserWall(out isDone);
                if (isDone) AiTimer = 30;
                break;
            case Attack.FireballStaggeredBursts:
                Attack_FireballStaggeredBursts(out isDone, 2, 6, 5f, 30);
                if (isDone) AiTimer = 15;
                break;
            case Attack.Deathray:
                Attack_Deathray(out isDone, 60);
                if (isDone) AiTimer = 15;
                break;
        }

        if (isDone) NextAttack();
    }
    
    private void Phase_MoveTransition()
    {
        countUpTimer = true;

        if (AiTimer == 0)
        {
            SoundEngine.PlaySound(SoundID.Roar, Npc.Center);
        }

        switch (AiTimer)
        {
            case < 120:
                var shrinkT = AiTimer / 120f;
                shrinkT = EasingHelper.QuadInOut(shrinkT);
                WallDistance = MathHelper.Lerp(750, 1000, shrinkT);
                break;
            case < 240:
                WallDistance = 1000;
                var speedT = (AiTimer - 120f) / 120f;
                speedT = EasingHelper.QuadInOut(speedT);
                Npc.velocity.X = MathHelper.Lerp(0f, Npc.spriteDirection * 2f, speedT);
                break;
            case 240:
                Npc.velocity.X = Npc.spriteDirection * 2f;
                countUpTimer = false;
                AiTimer = 0;
                CurrentPhase = PhaseState.Two;
                break;
        }
    }

    private void Phase_Two()
    {
        Npc.velocity.X = Npc.spriteDirection * 2f;
        
        if (AiTimer > 0 && !countUpTimer)
        {
            if (Npc.GetLifePercent() < 0.4f)
            {
                CurrentPhase = PhaseState.Three;
                CurrentAttackIndex = 0;
                return;
            }
            return;
        }
        
        

        var isDone = false;
        switch (CurrentAttack)
        {
            case Attack.Deathray:
                Attack_Deathray(out isDone, 60);
                if (isDone) AiTimer = 60;
                break;
            case Attack.Squeeze:
                Attack_Squeeze(out isDone, 250);
                if (isDone) AiTimer = 90;
                break;
            case Attack.DoubleFireballBurst:
                Attack_DoubleFireballBurst(10, 5f);
                AiTimer = 30;
                isDone = true;
                break;
            case Attack.SpawnBiomeMobs:
                Attack_SpawnBiomeMobs(out isDone, 2, 10);
                if (isDone) AiTimer = 60;
                break;
            case Attack.LaserSpam:
                Attack_LaserSpam(out isDone, 10, 10);
                if (isDone) AiTimer = 30;
                break;
            case Attack.LaserShotgun:
                Attack_LaserShotgun(out isDone, 14, MathHelper.Pi / 2f);
                if (isDone) AiTimer = 15;
                break;
        }

        if (isDone) NextAttack();
    }
    
    private void Phase_Three()
    {
        var speedT = (1f - Npc.GetLifePercent() - 0.2f) / 0.5f;
        speedT = MathF.Min(speedT, 1f);
        speedT = EasingHelper.QuadIn(speedT);
        Npc.velocity.X = MathHelper.Lerp(Npc.spriteDirection * 2, Npc.spriteDirection * 4, speedT);
        
        if (AiTimer > 0 && !countUpTimer)
        {
            return;
        }

        var isDone = false;
        switch (CurrentAttack)
        {
            case Attack.Deathray:
                Attack_Deathray(out isDone, 60);
                if (isDone) AiTimer = 15;
                break;
            case Attack.DoubleFireballBurst:
                Attack_DoubleFireballBurst(10, 5f);
                AiTimer = 30;
                isDone = true;
                break;
            case Attack.FireballStaggeredBursts:
                Attack_FireballStaggeredBursts(out isDone, 4, 8, 5f, 60);
                if (isDone) AiTimer = 45;
                break;
            case Attack.SpawnBiomeMobs:
                Attack_SpawnBiomeMobs(out isDone, 3, 7);
                if (isDone) AiTimer = 45;
                break;
            case Attack.LaserSpam:
                Attack_LaserSpam(out isDone, 15, 10);
                if (isDone) AiTimer = 30;
                break;
            case Attack.LaserShotgun:
                Attack_LaserShotgun(out isDone, 16, MathHelper.Pi / 2f);
                if (isDone) AiTimer = 30;
                break;
            case Attack.LaserWall:
                Attack_LaserWall(out isDone);
                if (isDone) AiTimer = 30;
                break;
        }

        if (isDone) NextAttack();
    }

    #endregion

    #region Attack Behaviors

    private static WoFPartPosition[] CwEyeOrder =
    {
        WoFPartPosition.Top | WoFPartPosition.Right,
        WoFPartPosition.Bottom | WoFPartPosition.Right,
        WoFPartPosition.Bottom | WoFPartPosition.Left,
        WoFPartPosition.Top | WoFPartPosition.Left,
    };
    
    private static WoFPartPosition[] CcwEyeOrder =
    {
        WoFPartPosition.Bottom | WoFPartPosition.Right,
        WoFPartPosition.Bottom | WoFPartPosition.Left,
        WoFPartPosition.Top | WoFPartPosition.Left,
        WoFPartPosition.Top | WoFPartPosition.Right,
    };

    private Counter<WoFPartPosition> deathrayOrder = new(CwEyeOrder);
    private Counter<WoFPartPosition> laserSpamOrder = new(CwEyeOrder);
    private Counter<WoFPartPosition> laserShotgunOrder = new(CcwEyeOrder);

    private void Attack_Deathray(out bool done, int telegraphTime)
    {
        countUpTimer = true;
        done = false;

        var partPos = deathrayOrder.Get();
        var raySpawnPos = PartPosToWorldPos(partPos);

        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            ref var targetRot = ref Npc.localAI[0];
            if (AiTimer == 0)
            {
                targetRot = raySpawnPos.DirectionTo(Main.player[Npc.target].Center).ToRotation();
                if (TryFindPartAtPos(out var part, partPos))
                {
                    var pos = raySpawnPos;
                    if ((partPos & WoFPartPosition.Right) != 0) pos.X += part.width;
                    
                    var indicator = NewDeathrayIndicator(pos, targetRot, part.whoAmI);
                    indicator.timeLeft = telegraphTime;
                }
            }

            if (AiTimer == telegraphTime)
            {
                if (TryFindPartAtPos(out var part, partPos))
                {
                    var pos = raySpawnPos;
                    if ((partPos & WoFPartPosition.Right) != 0) pos.X += part.width;
                    
                    var ray = NewDeathray(pos, targetRot, part.whoAmI);
                    ray.timeLeft = 120;
                }
                done = true;
            }
        }

        if (done)
        {
            deathrayOrder.Next();
            Npc.localAI[0] = 0;
            countUpTimer = false;
        }
    }

    private void Attack_Squeeze(out bool done, float distance)
    {
        countUpTimer = true;
        done = false;

        ref var initialDist = ref Npc.localAI[0];

        if (AiTimer == 0)
        {
            initialDist = WallDistance;
            SoundEngine.PlaySound(SoundID.Roar);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                var offsetL = new Vector2(-distance, -1750);
                var offsetR = new Vector2(distance, 1750);
                var l = NewLineIndicator(Npc.Center + offsetL, -MathHelper.PiOver2, Npc.whoAmI);
                l.timeLeft = 120;
                var r = NewLineIndicator(Npc.Center + offsetR, MathHelper.PiOver2, Npc.whoAmI);
                r.timeLeft = 120;
            }
        }
        
        switch (AiTimer)
        {
            case < 30:
                break;
            case < 30 + 120:
            {
                var t = (AiTimer - 30) / 120f;
                t = EasingHelper.QuadInOut(t);
                WallDistance = MathHelper.Lerp(initialDist, distance, t);
                break;
            }
            case < 30 + 120 + 60:
            {
                var t = (AiTimer - 30 - 120) / 60f;
                t = EasingHelper.QuadInOut(t);
                WallDistance = MathHelper.Lerp(distance, initialDist, t);
                break;
            }
            default:
                done = true;
                break;
        }

        if (done)
        {
            Npc.localAI[0] = 0;
            Npc.localAI[1] = 0;
            countUpTimer = false;
        }
    }

    private void Attack_FireballBurst(int projectiles, float spread, float angle, float speed, WoFPartPosition pos)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        var position = PartPosToWorldPos(pos);
        var targetPos = Main.player[Npc.target].Center;
        
        if ((pos & WoFPartPosition.Left) != 0) position.X += 100;

        for (int i = 0; i < projectiles; i++)
        {
            var offset = ((float) i / projectiles - 0.5f) * 2 * spread;
            var a = angle + offset + (spread / projectiles);
            var vel = (a.ToRotationVector2() * speed) + (Npc.velocity / 2);

            NewFireball(position, vel);
        }
    }

    private void Attack_DoubleFireballBurst(int projectiles, float speed)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        
        Attack_FireballBurst(projectiles, MathHelper.PiOver2, 0f, speed, WoFPartPosition.Left | WoFPartPosition.Center);
        Attack_FireballBurst(projectiles, MathHelper.PiOver2, MathHelper.Pi, speed, WoFPartPosition.Right | WoFPartPosition.Center);
    }
    
    private void Attack_FireballStaggeredBursts(out bool isDone, int waves, int ballsPerWave, float speed, int waveInterval)
    {
        countUpTimer = true;

        isDone = AiTimer > (waves - 1) * waveInterval;

        if (isDone)
        {
            countUpTimer = false;
            return;
        }
        
        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        if (AiTimer == 0) Npc.localAI[0] = (int) Main.rand.NextFromList(WoFPartPosition.Left, WoFPartPosition.Right);

        if (AiTimer % waveInterval == 0)
        {
            var side = (WoFPartPosition) Npc.localAI[0];
            var direction = 0f;
            if (side == WoFPartPosition.Right) direction = MathHelper.Pi;

            if (AiTimer % (waveInterval * 2) == 0)
            {
                Attack_FireballBurst(ballsPerWave, MathHelper.PiOver2, direction, speed, side | WoFPartPosition.Center);
            } 
            else
            {
                Attack_FireballBurst(ballsPerWave + 1, MathHelper.PiOver2, direction, speed, side | WoFPartPosition.Center);
            }
        }
    }

    private void Attack_LaserSpam(out bool isDone, int shots, int delay)
    {
        countUpTimer = true;
        const int indicateTime = 30;
        isDone = AiTimer > shots * delay + indicateTime;

        var pos = laserSpamOrder.Get();

        if (isDone)
        {
            countUpTimer = false;
            laserSpamOrder.Next();
        }

        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        if (AiTimer % delay == 0)
        {
            var position = PartPosToWorldPos(pos);
            TryFindPartAtPos(out var anchor, pos);
            if ((pos & WoFPartPosition.Left) != 0) position.X += anchor.width;
            
            var targetPos = position;
            
            var vel = targetPos.DirectionTo(Main.player[Npc.target].Center);
            
            var laser = NewLaser(position, vel * 25f, vel.ToRotation(), 0);
            laser.timeLeft = indicateTime;
        }
    }
    
    private void Attack_LaserShotgun(out bool isDone, int lasers, float spread)
    {
        countUpTimer = true;
        const int indicateTime = 30;
        isDone = AiTimer > indicateTime;

        var pos = laserShotgunOrder.Get();

        if (isDone)
        {
            countUpTimer = false;
            laserShotgunOrder.Next();
        }

        if (AiTimer != 0) return;
        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        for (int i = 0; i < lasers; i++)
        {
            var position = PartPosToWorldPos(pos);
            TryFindPartAtPos(out var anchor, pos);
            if ((pos & WoFPartPosition.Right) != 0) position.X += anchor.width;

            // Face inwards
            var vel = Vector2.UnitX;
            if ((pos & WoFPartPosition.Right) != 0) vel *= -1;
            
            // Spread
            var offset = ((float) i / lasers - 0.5f) * 2 * spread;
            vel = vel.RotatedBy(offset + (spread / lasers / 4));
            
            // Randomize spread a little
            vel = vel.RotateRandom(spread / lasers / 2);

            var laser = NewLaser(position, vel * 25f, vel.ToRotation(), anchor.whoAmI);
            laser.timeLeft = indicateTime;
        }
    }

    private void Attack_LaserWall(out bool isDone)
    {
        countUpTimer = true;
        const int indicateTime = 60;
        
        isDone = AiTimer > indicateTime;
        
        if (isDone)
        {
            countUpTimer = false;
        }
        
        if (AiTimer != 0) return;
        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        var side = Main.rand.NextFromList(WoFPartPosition.Left, WoFPartPosition.Right);
        
        var direction = 0f;
        if (side == WoFPartPosition.Right) direction = MathHelper.Pi;

        var wallHeight = 0f;
        if (side == WoFPartPosition.Right)
        {
            wallHeight = WoFSystem.WofDrawAreaBottomRight - WoFSystem.WofDrawAreaTopRight;
        }
        else
        {
            wallHeight = WoFSystem.WofDrawAreaBottomLeft - WoFSystem.WofDrawAreaTopLeft;
        }
        
        var laserSpacing = 125;
        var lasers =(int) (wallHeight / laserSpacing);

        var x = PartPosToWorldPos(WoFPartPosition.Right).X;
        if (side == WoFPartPosition.Left) x += 100;

        for (int i = 0; i < lasers; i++)
        {
            var y = WoFSystem.WofDrawAreaTopRight + i * laserSpacing;
            var angle = MathHelper.Pi;

            var laser = NewLaser(new Vector2(x, y), angle.ToRotationVector2() * 25f, angle);
            laser.timeLeft = indicateTime;
        }
    }

    private void Attack_SpawnBiomeMobs(out bool isDone, int count, int delay)
    {
        countUpTimer = true;
        isDone = AiTimer > delay * count;

        if (AiTimer % delay == 0) SoundEngine.PlaySound(SoundID.NPCDeath13, Npc.Center);

        if (isDone)
        {
            countUpTimer = false;
            AiTimer = 0;
            return;
        }
        
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        if (AiTimer % delay == 0)
        {
            if (Main.rand.NextBool())
            {
                // Evil Mob
                var pos = RandomPartX() | WoFPartPosition.Center;
                NewEvilMob(pos);
            }
            else
            {
                // Good Mob
                var pos = RandomPartX() | WoFPartPosition.Center;
                NewHallowMob(pos);
            }
        }
    }

    
    #region Spawners
    
    private Projectile NewFireball(Vector2 pos, Vector2 vel)
    {
        var proj = Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), pos, vel, 
            ProjectileID.CursedFlameHostile, damage / 4, 3);
        proj.scale = 0.9f;
        proj.tileCollide = false;
        return proj;
    }
    
    
    private Projectile NewLaser(Vector2 pos, Vector2 vel, float rotation, int anchor = 0)
    {
        var proj = Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), pos, vel, 
            ModContent.ProjectileType<WoFLaser>(), damage / 4, 3, ai0: rotation, ai1: anchor);

        return proj;
    }

    private Projectile NewDeathray(Vector2 pos, float rotation, int anchor = 0)
    {
        var ai1 = anchor;
        return Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), pos, Vector2.Zero,
            ModContent.ProjectileType<WoFDeathray>(), damage / 2, 3, ai0: rotation, ai1: ai1);
    }

    private Projectile NewDeathrayIndicator(Vector2 pos, float rotation, int anchor = 0)
    {
        var ai1 = anchor;
        return Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), pos, Vector2.Zero,
            ModContent.ProjectileType<WoFDeathrayIndicator>(), 0, 0, ai0: rotation, ai1: ai1);
    }
    
    private Projectile NewLineIndicator(Vector2 pos, float rotation, int anchor = 0)
    {
        var ai1 = anchor;
        return Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), pos, Vector2.Zero,
            ModContent.ProjectileType<WoFMoveIndicator>(), 0, 0, ai0: rotation, ai1: ai1);
    }
    
    private NPC NewEvilMob(WoFPartPosition pos)
    {
        var crimsonMob = NPCID.Crimera;
        var corruptMob = NPCID.EaterofSouls;
        
        var position = PartPosToWorldPos(pos);

        // Spawn a mob based on which of the world evil it is
        int type;
        
        if (Main.drunkWorld)
            type = Main.rand.NextBool() ? crimsonMob : corruptMob;
        else if (WorldGen.crimson)
            type = crimsonMob;
        else 
            type = corruptMob;
        
        var npc = NPC.NewNPCDirect(Npc.GetSource_FromAI(), position, type, Npc.whoAmI);

        if ((pos & WoFPartPosition.Right) != 0)
            npc.velocity.X = -10;
        else 
            npc.velocity.X = 10;

        return npc;
    }
    
    private NPC NewHallowMob(WoFPartPosition pos)
    {
        var position = PartPosToWorldPos(pos);
        var type = ModContent.NPCType<EasyPixieNPC>();
        var npc = NPC.NewNPCDirect(Npc.GetSource_FromAI(), position, type, Npc.whoAmI);
        
        if ((pos & WoFPartPosition.Right) != 0)
            npc.velocity.X = -10;
        else 
            npc.velocity.X = 10;

        return npc;
    }

    private NPC NewEye(WoFPartPosition pos)
    {
        var position = PartPosToWorldPos(pos);

        var npc = NPC.NewNPCDirect(Npc.GetSource_FromAI(), position, NPCID.WallofFleshEye, Npc.whoAmI);
        npc.ai[0] = (float) pos;

        return npc;
    }

    private NPC NewMouth(WoFPartPosition pos)
    {
        var position = PartPosToWorldPos(pos);

        var npc = NPC.NewNPCDirect(Npc.GetSource_FromAI(), position, ModContent.NPCType<WoFMouth>(), Npc.whoAmI);
        npc.ai[0] = (float) pos;

        return npc;
    }
    
    #endregion

    #endregion

    #endregion

    #region Drawing

    public override bool AcidicDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        // Don't draw lmao
        return false;
    }

    public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
    {
        return false;
    }

    private void SetWoFArea()
    {
        SetWoFAreaRight();
        SetWoFAreaLeft();
    }

    private void SetWoFAreaRight()
    {
        // Stolen from vanilla
        var leftSideBlockX = (int) ((Npc.position.X + WallDistance) / 16f);
        var rightSideBlockX = (int) ((Npc.position.X + Npc.width + WallDistance) / 16f);
        var centerBlockY = (int) ((Npc.position.Y + Npc.height / 2f) / 16f);

        // Find bottom of area
        var i = 0;
        var testBlockY = centerBlockY + 7;
        while (i < 15 && testBlockY > Main.UnderworldLayer)
        {
            testBlockY++;
            for (int testBlockX = leftSideBlockX; testBlockX <= rightSideBlockX; testBlockX++)
            {
                try
                {
                    if (WorldGen.SolidTile(testBlockX, testBlockY) ||
                        Main.tile[testBlockX, testBlockY].LiquidAmount > 0)
                    {
                        i++;
                    }
                }
                catch (Exception _)
                {
                    i += 15;
                }
            }
        }

        testBlockY += 4;
        if (WoFSystem.WofDrawAreaBottomRight == -1)
        {
            WoFSystem.WofDrawAreaBottomRight = testBlockY * 16;
        }
        else if (WoFSystem.WofDrawAreaBottomRight > testBlockY * 16)
        {
            WoFSystem.WofDrawAreaBottomRight--;
            if (WoFSystem.WofDrawAreaBottomRight < testBlockY * 16)
            {
                WoFSystem.WofDrawAreaBottomRight = testBlockY * 16;
            }
        }
        else if (WoFSystem.WofDrawAreaBottomRight < testBlockY * 16)
        {
            WoFSystem.WofDrawAreaBottomRight++;
            if (WoFSystem.WofDrawAreaBottomRight > testBlockY * 16)
            {
                WoFSystem.WofDrawAreaBottomRight = testBlockY * 16;
            }
        }

        // Find Top of area
        i = 0;
        testBlockY = centerBlockY - 7;
        while (i < 15 && testBlockY < Main.maxTilesY - 10)
        {
            testBlockY--;
            for (int testBlockX = leftSideBlockX; testBlockX <= rightSideBlockX; testBlockX++)
            {
                try
                {
                    if (WorldGen.SolidTile(testBlockX, testBlockY) ||
                        Main.tile[testBlockX, testBlockY].LiquidAmount > 0)
                    {
                        i++;
                    }
                }
                catch (Exception _)
                {
                    i += 15;
                }
            }
        }

        testBlockY -= 4;
        if (WoFSystem.WofDrawAreaTopRight == -1)
        {
            WoFSystem.WofDrawAreaTopRight = testBlockY * 16;
        }
        else if (WoFSystem.WofDrawAreaTopRight > testBlockY * 16)
        {
            WoFSystem.WofDrawAreaTopRight--;
            if (WoFSystem.WofDrawAreaTopRight < testBlockY * 16)
            {
                WoFSystem.WofDrawAreaTopRight = testBlockY * 16;
            }
        }
        else if (WoFSystem.WofDrawAreaTopRight < testBlockY * 16)
        {
            WoFSystem.WofDrawAreaTopRight++;
            if (WoFSystem.WofDrawAreaTopRight > testBlockY * 16)
            {
                WoFSystem.WofDrawAreaTopRight = testBlockY * 16;
            }
        }
    }

    private void SetWoFAreaLeft()
    {
        // Stolen from vanilla
        var leftSideBlockX = (int) ((Npc.position.X - WallDistance) / 16f);
        var rightSideBlockX = (int) ((Npc.position.X + Npc.width - WallDistance) / 16f);
        var centerBlockY = (int) ((Npc.position.Y + Npc.height / 2f) / 16f);

        // Find bottom of area
        var i = 0;
        var testBlockY = centerBlockY + 7;
        while (i < 15 && testBlockY > Main.UnderworldLayer)
        {
            testBlockY++;
            for (int testBlockX = leftSideBlockX; testBlockX <= rightSideBlockX; testBlockX++)
            {
                try
                {
                    if (WorldGen.SolidTile(testBlockX, testBlockY) ||
                        Main.tile[testBlockX, testBlockY].LiquidAmount > 0)
                    {
                        i++;
                    }
                }
                catch (Exception _)
                {
                    i += 15;
                }
            }
        }

        testBlockY += 4;
        if (WoFSystem.WofDrawAreaBottomLeft == -1)
        {
            WoFSystem.WofDrawAreaBottomLeft = testBlockY * 16;
        }
        else if (WoFSystem.WofDrawAreaBottomLeft > testBlockY * 16)
        {
            WoFSystem.WofDrawAreaBottomLeft--;
            if (WoFSystem.WofDrawAreaBottomLeft < testBlockY * 16)
            {
                WoFSystem.WofDrawAreaBottomLeft = testBlockY * 16;
            }
        }
        else if (WoFSystem.WofDrawAreaBottomLeft < testBlockY * 16)
        {
            WoFSystem.WofDrawAreaBottomLeft++;
            if (WoFSystem.WofDrawAreaBottomLeft > testBlockY * 16)
            {
                WoFSystem.WofDrawAreaBottomLeft = testBlockY * 16;
            }
        }

        // Find Top of area
        i = 0;
        testBlockY = centerBlockY - 7;
        while (i < 15 && testBlockY < Main.maxTilesY - 10)
        {
            testBlockY--;
            for (int testBlockX = leftSideBlockX; testBlockX <= rightSideBlockX; testBlockX++)
            {
                try
                {
                    if (WorldGen.SolidTile(testBlockX, testBlockY) ||
                        Main.tile[testBlockX, testBlockY].LiquidAmount > 0)
                    {
                        i++;
                    }
                }
                catch (Exception _)
                {
                    i += 15;
                }
            }
        }

        testBlockY -= 4;
        if (WoFSystem.WofDrawAreaTopLeft == -1)
        {
            WoFSystem.WofDrawAreaTopLeft = testBlockY * 16;
        }
        else if (WoFSystem.WofDrawAreaTopLeft > testBlockY * 16)
        {
            WoFSystem.WofDrawAreaTopLeft--;
            if (WoFSystem.WofDrawAreaTopLeft < testBlockY * 16)
            {
                WoFSystem.WofDrawAreaTopLeft = testBlockY * 16;
            }
        }
        else if (WoFSystem.WofDrawAreaTopLeft < testBlockY * 16)
        {
            WoFSystem.WofDrawAreaTopLeft++;
            if (WoFSystem.WofDrawAreaTopLeft > testBlockY * 16)
            {
                WoFSystem.WofDrawAreaTopLeft = testBlockY * 16;
            }
        }
    }

    #endregion
    
    #region PartPos
    private bool TryFindPartAtPos(out NPC? found, WoFPartPosition pos)
    {
        Func<NPC, bool> validType = n => n.type == NPCID.WallofFleshEye || n.type == ModContent.NPCType<WoFMouth>();
        var parts = Main.npc.Where(n => n.active && validType(n));

        foreach (var npc in parts)
        {
            if ((WoFPartPosition) npc.ai[0] == pos)
            {
                found = npc;
                return true;
            };
        }

        found = null;
        return false;
    }

    private Vector2 PartPosToWorldPos(WoFPartPosition pos)
    {
        var position = new Vector2();

        var areaBottom = 0f;
        var areaTop = 0f;
        if ((pos & WoFPartPosition.Right) != 0)
        {
            position.X = Npc.position.X + WallDistance;
            areaBottom = WoFSystem.WofDrawAreaBottomRight;
            areaTop = WoFSystem.WofDrawAreaTopRight;
        }
        else if ((pos & WoFPartPosition.Left) != 0)
        {
            position.X = Npc.position.X - WallDistance;
            areaBottom = WoFSystem.WofDrawAreaBottomLeft;
            areaTop = WoFSystem.WofDrawAreaTopLeft;
        }

        if ((pos & WoFPartPosition.Bottom) != 0)
        {
            position.Y = (areaBottom + areaTop) / 2f;
            position.Y = (position.Y + areaTop) / 2f;
        }
        else if ((pos & WoFPartPosition.Top) != 0)
        {
            position.Y = (areaBottom + areaTop) / 2f;
            position.Y = (position.Y + areaBottom) / 2f;
        }
        else if ((pos & WoFPartPosition.Center) != 0)
        {
            position.Y = (areaBottom + areaTop) / 2f;
        }

        return position;
    }

    private WoFPartPosition RandomPartX()
    {
        return Main.rand.NextFromList(WoFPartPosition.Left, WoFPartPosition.Right);
    }
    
    private WoFPartPosition RandomPartY()
    {
        return Main.rand.NextFromList(WoFPartPosition.Top, WoFPartPosition.Center, WoFPartPosition.Bottom);
    }

    private WoFPartPosition RandomEyeY()
    {
        return Main.rand.NextFromList(WoFPartPosition.Top, WoFPartPosition.Bottom);
    }

    private WoFPartPosition RandomPartPos() => RandomPartX() | RandomPartY();

    private WoFPartPosition RandomEyePos() => RandomPartX() | RandomEyeY();

    #endregion

    public override void SendAcidAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        deathrayOrder.SendData(binaryWriter);
        laserSpamOrder.SendData(binaryWriter);
        laserShotgunOrder.SendData(binaryWriter);
    }

    public override void ReceiveAcidAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        deathrayOrder.RecieveData(binaryReader);
        laserSpamOrder.RecieveData(binaryReader);
        laserShotgunOrder.RecieveData(binaryReader);
    }
}