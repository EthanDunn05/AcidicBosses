using System;
using System.IO;
using System.Linq;
using AcidicBosses.Common.Configs;
using AcidicBosses.Content.Bosses.WoF.Projectiles;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Core.StateManagement;
using AcidicBosses.Helpers;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Content.Bosses.WoF.AI;

public class WoF : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.WallofFlesh;
    protected override bool BossEnabled => BossToggleConfig.Get().EnableWallOfFlesh;

    private int damage = 50;

    public float WallDistance
    {
        get => Npc.ai[3];
        set => Npc.ai[3] = value;
    }

    public override void SetStaticDefaults()
    {
        if (!ShouldOverride()) return;
        NPCID.Sets.NeedsExpertScaling[NPCID.WallofFlesh] = true;
    }

    public override void SetDefaults(NPC entity)
    {
        if (!ShouldOverride()) return;
        entity.damage = 0;
        entity.dontTakeDamage = true;
        entity.ShowNameOnHover = false;
    }

    public override void BossHeadSlot(NPC npc, ref int index)
    {
        if (!ShouldOverride()) return;
        index = -1;
    }

    #region AI

    private PhaseTracker phaseTracker;

    private bool isFleeing = false;

    public override void OnFirstFrame(NPC npc)
    {
        phaseTracker = new PhaseTracker([
            PhaseIntro,
            PhaseOne,
            PhaseMoveTransition,
            PhaseTwo,
            PhaseThree
        ]);
        AttackManager.Reset();
        WallDistance = 3000;
        
        damage = Npc.GetAttackDamage_ScaledByStrength(damage);

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
        // Flee when no players are alive or it is day  
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

        // Fill Arena Area
        SetWoFArea();

        if (isFleeing) FleeAI();
        else phaseTracker.RunPhaseAI();

        return false;
    }

    private void FleeAI()
    {
        // Put Flee Behavior here
        if (!HasValidConditions()) Npc.active = false;
        Npc.TargetClosest();

        Npc.velocity.X = Npc.spriteDirection * EasingHelper.QuadIn(AttackManager.AiTimer / 30f);
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

    private PhaseState PhaseIntro => new(Phase_Intro);
    
    private void Phase_Intro()
    {
        AttackManager.CountUp = true;

        switch (AttackManager.AiTimer)
        {
            case < 120:
                var t = AttackManager.AiTimer / 120f;
                t = EasingHelper.QuadInOut(t);
                WallDistance = MathHelper.Lerp(3000, 750, t);
                break;
            case 120:
                AttackManager.Reset();
                phaseTracker.NextPhase();
                WallDistance = 750;
                break;
        }
    }

    private PhaseState PhaseOne => new(Phase_One, EnterPhaseOne);

    private void EnterPhaseOne()
    {
        var laserShotgun = new AttackState(() => Attack_LaserShotgun(10, MathHelper.Pi / 2f), 10);
        var laserSpam = new AttackState(() => Attack_LaserSpam(5, 15), 30);
        var laserWall = new AttackState(() => Attack_LaserWall(), 30);
        var staggeredBursts = new AttackState(() => Attack_FireballStaggeredBursts(2, 6, 5f, 30), 15);
        var deathray = new AttackState(() => Attack_Deathray(60), 15);
        
        AttackManager.SetAttackPattern([
            laserShotgun,
            staggeredBursts,
            deathray,
            laserShotgun,
            laserWall,
        ]);
    }
    
    private void Phase_One()
    {
        if (AttackManager.AiTimer > 0 && !AttackManager.CountUp)
        {
            if (Npc.GetLifePercent() < 0.6f)
            {
                AttackManager.Reset();
                phaseTracker.NextPhase();
                return;
            }
            return;
        }

        AttackManager.RunAttackPattern();
    }

    private PhaseState PhaseMoveTransition => new(Phase_MoveTransition);
    
    private void Phase_MoveTransition()
    {
        AttackManager.CountUp = true;

        if (AttackManager.AiTimer == 0)
        {
            SoundEngine.PlaySound(SoundID.Roar, Npc.Center);
        }

        switch (AttackManager.AiTimer)
        {
            case < 120:
                var shrinkT = AttackManager.AiTimer / 120f;
                shrinkT = EasingHelper.QuadInOut(shrinkT);
                WallDistance = MathHelper.Lerp(750, 1000, shrinkT);
                break;
            case < 240:
                WallDistance = 1000;
                var speedT = (AttackManager.AiTimer - 120f) / 120f;
                speedT = EasingHelper.QuadInOut(speedT);
                Npc.velocity.X = MathHelper.Lerp(0f, Npc.spriteDirection * 2f, speedT);
                break;
            case 240:
                Npc.velocity.X = Npc.spriteDirection * 2f;
                AttackManager.Reset();
                phaseTracker.NextPhase();
                break;
        }
    }

    private PhaseState PhaseTwo => new(Phase_Two, EnterPhaseTwo);

    private void EnterPhaseTwo()
    {
        var deathray = new AttackState(() => Attack_Deathray(60), 60);
        var squeeze = new AttackState(() => Attack_Squeeze(250), 90);
        var doubleFireball = new AttackState(() => Attack_DoubleFireballBurst(10, 5f), 30);
        var summon = new AttackState(() => Attack_SpawnBiomeMobs(2, 10), 60);
        var laserSpam = new AttackState(() => Attack_LaserSpam(10, 10), 30);
        var laserShotgun = new AttackState(() => Attack_LaserShotgun(14, MathHelper.Pi / 2f), 15);
        
        AttackManager.SetAttackPattern([
            doubleFireball,
            squeeze,
            laserSpam,
            laserShotgun,
            summon
        ]);
    }
    
    private void Phase_Two()
    {
        Npc.velocity.X = Npc.spriteDirection * 2f;
        
        if (AttackManager.AiTimer > 0 && !AttackManager.CountUp)
        {
            if (Npc.GetLifePercent() < 0.4f)
            {
                AttackManager.Reset();
                phaseTracker.NextPhase();
                return;
            }
            return;
        }
        
        AttackManager.RunAttackPattern();
    }

    private PhaseState PhaseThree => new(Phase_Three, EnterPhaseThree);

    private void EnterPhaseThree()
    {
        var deathray = new AttackState(() => Attack_Deathray(60), 15);
        var doubleFireball = new AttackState(() => Attack_DoubleFireballBurst(10, 5f), 30);
        var staggeredBursts = new AttackState(() => Attack_FireballStaggeredBursts(4, 8, 5f, 60), 45);
        var summon = new AttackState(() => Attack_SpawnBiomeMobs(3, 7), 45);
        var laserSpam =  new AttackState(() => Attack_LaserSpam(15, 10), 30);
        var laserShotgun = new AttackState(() => Attack_LaserShotgun(16, MathHelper.Pi / 2f), 30);
        var laserWall = new AttackState(Attack_LaserWall, 30);
        
        AttackManager.SetAttackPattern([
            deathray,
            laserShotgun,
            summon,
            doubleFireball,
            laserSpam,
            staggeredBursts
        ]);
    }
    
    private void Phase_Three()
    {
        var speedT = (1f - Npc.GetLifePercent() - 0.2f) / 0.5f;
        speedT = MathF.Min(speedT, 1f);
        speedT = EasingHelper.QuadIn(speedT);
        Npc.velocity.X = MathHelper.Lerp(Npc.spriteDirection * 2, Npc.spriteDirection * 4, speedT);
        
        if (AttackManager.AiTimer > 0 && !AttackManager.CountUp)
        {
            return;
        }

        AttackManager.RunAttackPattern();
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

    private bool Attack_Deathray(int telegraphTime)
    {
        AttackManager.CountUp = true;
        var done = false;

        var partPos = deathrayOrder.Get();
        var raySpawnPos = PartPosToWorldPos(partPos);

        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            ref var targetRot = ref Npc.localAI[0];
            if (AttackManager.AiTimer == 0)
            {
                targetRot = raySpawnPos.DirectionTo(Main.player[Npc.target].Center).ToRotation();
                if (TryFindPartAtPos(out var part, partPos))
                {
                    var pos = raySpawnPos;
                    if ((partPos & WoFPartPosition.Right) != 0) pos.X += part.width;
                    
                    var indicator = NewDeathrayIndicator(pos, targetRot, telegraphTime, part.whoAmI);
                }
            }

            if (AttackManager.AiTimer == telegraphTime)
            {
                if (TryFindPartAtPos(out var part, partPos))
                {
                    var pos = raySpawnPos;
                    if ((partPos & WoFPartPosition.Right) != 0) pos.X += part.width;
                    
                    var ray = NewDeathray(pos, targetRot, 120, part.whoAmI);
                }
                done = true;
            }
        }

        if (done)
        {
            deathrayOrder.Next();
            Npc.localAI[0] = 0;
            AttackManager.CountUp = false;
        }

        return done;
    }

    private bool Attack_Squeeze(float distance)
    {
        AttackManager.CountUp = true;
        var done = false;

        ref var initialDist = ref Npc.localAI[0];

        if (AttackManager.AiTimer == 0)
        {
            initialDist = WallDistance;
            SoundEngine.PlaySound(SoundID.Roar);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                var offsetL = new Vector2(-distance, 1750);
                var offsetR = new Vector2(distance, -1750);
                var l = NewLineIndicator(Npc.Center + offsetL, MathHelper.PiOver2, 120, Npc.whoAmI);
                var r = NewLineIndicator(Npc.Center + offsetR, -MathHelper.PiOver2, 120, Npc.whoAmI);
            }
        }
        
        switch (AttackManager.AiTimer)
        {
            case < 30:
                break;
            case < 30 + 120:
            {
                var t = (AttackManager.AiTimer - 30) / 120f;
                t = EasingHelper.QuadInOut(t);
                WallDistance = MathHelper.Lerp(initialDist, distance, t);
                break;
            }
            case < 30 + 120 + 60:
            {
                var t = (AttackManager.AiTimer - 30 - 120) / 60f;
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
            AttackManager.CountUp = false;
        }

        return done;
    }

    private bool Attack_FireballBurst(int projectiles, float spread, float angle, float speed, WoFPartPosition pos)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return true;

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

        return true;
    }

    private bool Attack_DoubleFireballBurst(int projectiles, float speed)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return true;
        
        Attack_FireballBurst(projectiles, MathHelper.PiOver2, 0f, speed, WoFPartPosition.Left | WoFPartPosition.Center);
        Attack_FireballBurst(projectiles, MathHelper.PiOver2, MathHelper.Pi, speed, WoFPartPosition.Right | WoFPartPosition.Center);

        return true;
    }
    
    private bool Attack_FireballStaggeredBursts(int waves, int ballsPerWave, float speed, int waveInterval)
    {
        AttackManager.CountUp = true;

        var isDone = AttackManager.AiTimer > (waves - 1) * waveInterval;

        if (isDone)
        {
            AttackManager.CountUp = false;
            return true;
        }
        
        if (Main.netMode == NetmodeID.MultiplayerClient) return false;

        if (AttackManager.AiTimer == 0) Npc.localAI[0] = (int) Main.rand.NextFromList(WoFPartPosition.Left, WoFPartPosition.Right);

        if (AttackManager.AiTimer % waveInterval == 0)
        {
            var side = (WoFPartPosition) Npc.localAI[0];
            var direction = 0f;
            if (side == WoFPartPosition.Right) direction = MathHelper.Pi;

            if (AttackManager.AiTimer % (waveInterval * 2) == 0)
            {
                Attack_FireballBurst(ballsPerWave, MathHelper.PiOver2, direction, speed, side | WoFPartPosition.Center);
            } 
            else
            {
                Attack_FireballBurst(ballsPerWave + 1, MathHelper.PiOver2, direction, speed, side | WoFPartPosition.Center);
            }
        }

        return false;
    }

    private bool Attack_LaserSpam(int shots, int delay)
    {
        AttackManager.CountUp = true;
        const int indicateTime = 30;
        var isDone = AttackManager.AiTimer > shots * delay + indicateTime;

        var pos = laserSpamOrder.Get();

        if (AttackManager.AiTimer == 0)
        {
            // Set the face target bit to true
            TryFindPartAtPos(out var eye, pos);
            var state = (WoFPartState) eye.ai[2];
            state |= WoFPartState.FaceTarget;
            eye.ai[2] = (int) state;
        }
        
        if (isDone)
        {
            AttackManager.CountUp = false;
            laserSpamOrder.Next();
            
            // Set the face target bit to false
            TryFindPartAtPos(out var eye, pos);
            var state = (WoFPartState) eye.ai[2];
            state &= ~WoFPartState.FaceTarget;
            eye.ai[2] = (int) state;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient) return isDone;

        if (AttackManager.AiTimer % delay == 0)
        {
            var position = PartPosToWorldPos(pos);
            TryFindPartAtPos(out var anchor, pos);
            if ((pos & WoFPartPosition.Left) != 0) position.X += anchor.width;
            
            var targetPos = position;
            
            var vel = targetPos.DirectionTo(Main.player[Npc.target].Center);
            
            var laser = NewLaser(position, vel * 25f, vel.ToRotation(), indicateTime);
        }

        return isDone;
    }
    
    private bool Attack_LaserShotgun(int lasers, float spread)
    {
        AttackManager.CountUp = true;
        const int indicateTime = 30;
        var isDone = AttackManager.AiTimer > indicateTime;

        var pos = laserShotgunOrder.Get();

        if (isDone)
        {
            AttackManager.CountUp = false;
            laserShotgunOrder.Next();
        }

        if (AttackManager.AiTimer != 0) return isDone;
        if (Main.netMode == NetmodeID.MultiplayerClient) return isDone;

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

            var laser = NewLaser(position, vel * 25f, vel.ToRotation(), indicateTime, anchor.whoAmI);
        }

        return isDone;
    }

    private bool Attack_LaserWall()
    {
        AttackManager.CountUp = true;
        const int indicateTime = 60;
        
        var isDone = AttackManager.AiTimer > indicateTime;
        
        if (isDone)
        {
            AttackManager.CountUp = false;
        }
        
        if (AttackManager.AiTimer != 0) return isDone;
        if (Main.netMode == NetmodeID.MultiplayerClient) return isDone;

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

        var x = PartPosToWorldPos(side).X;
        if (side == WoFPartPosition.Left) x += 100;

        for (int i = 0; i < lasers; i++)
        {
            var y = WoFSystem.WofDrawAreaTopRight + i * laserSpacing;

            var laser = NewLaser(new Vector2(x, y), direction.ToRotationVector2() * 25f, direction, indicateTime);
        }

        return isDone;
    }

    private bool Attack_SpawnBiomeMobs(int count, int delay)
    {
        AttackManager.CountUp = true;
        var isDone = AttackManager.AiTimer > delay * count;

        if (AttackManager.AiTimer % delay == 0) SoundEngine.PlaySound(SoundID.NPCDeath13, Npc.Center);

        if (isDone)
        {
            AttackManager.CountUp = false;
            AttackManager.AiTimer = 0;
            return true;
        }
        
        if (Main.netMode == NetmodeID.MultiplayerClient) return false;
        if (AttackManager.AiTimer % delay == 0)
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

        return false;
    }

    
    #region Spawners
    
    private Projectile NewFireball(Vector2 pos, Vector2 vel)
    {
        var proj = ProjHelper.NewUnscaledProjectile(Npc.GetSource_FromAI(), pos, vel, 
            ProjectileID.CursedFlameHostile, damage / 2, 3);
        proj.scale = 0.9f;
        proj.tileCollide = false;
        return proj;
    }
    
    private Projectile NewLaser(Vector2 pos, Vector2 vel, float rotation, int lifetime, int anchor = -1)
    {
        var proj = BaseLineProjectile.Create<WoFLaser>(Npc.GetSource_FromAI(), pos, vel, damage / 2, 3, rotation, lifetime, anchor);

        return proj;
    }

    private Projectile NewDeathray(Vector2 pos, float rotation, int lifetime, int anchor = -1)
    {
        return DeathrayBase.Create<WoFDeathray>(Npc.GetSource_FromAI(), pos, (int) (damage * 1.5f), 5, rotation, lifetime, anchor);
    }

    private Projectile NewDeathrayIndicator(Vector2 pos, float rotation, int lifetime, int anchor = -1)
    {
        return BaseLineProjectile.Create<WoFDeathrayIndicator>(Npc.GetSource_FromAI(), pos, rotation, lifetime, anchor);
    }
    
    private Projectile NewLineIndicator(Vector2 pos, float rotation, int lifetime, int anchor = -1)
    {
        return BaseLineProjectile.Create<WoFMoveIndicator>(Npc.GetSource_FromAI(), pos, rotation, lifetime, anchor);
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

    public override void SendAcidAI(BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        deathrayOrder.SendData(binaryWriter);
        laserSpamOrder.SendData(binaryWriter);
        laserShotgunOrder.SendData(binaryWriter);
        
        phaseTracker.Serialize(binaryWriter);
        
        binaryWriter.Write(WoFSystem.WofDrawAreaBottomLeft);
        binaryWriter.Write(WoFSystem.WofDrawAreaBottomRight);
        binaryWriter.Write(WoFSystem.WofDrawAreaTopRight);
        binaryWriter.Write(WoFSystem.WofDrawAreaTopLeft);
    }

    public override void ReceiveAcidAI(BitReader bitReader, BinaryReader binaryReader)
    {
        deathrayOrder.RecieveData(binaryReader);
        laserSpamOrder.RecieveData(binaryReader);
        laserShotgunOrder.RecieveData(binaryReader);
        
        phaseTracker.Deserialize(binaryReader);
        
        WoFSystem.WofDrawAreaBottomLeft = binaryReader.ReadSingle();
        WoFSystem.WofDrawAreaBottomRight = binaryReader.ReadSingle();
        WoFSystem.WofDrawAreaTopRight = binaryReader.ReadSingle();
        WoFSystem.WofDrawAreaTopLeft = binaryReader.ReadSingle();
    }
}