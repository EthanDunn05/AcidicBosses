using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AcidicBosses.Common.Textures;
using AcidicBosses.Content.Bosses.Twins.Projectiles;
using AcidicBosses.Content.Particles;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Content.Projectiles;
using AcidicBosses.Core.StateManagement;
using AcidicBosses.Helpers;
using AcidicBosses.Helpers.NpcHelpers;
using Luminance.Common.Utilities;
using Luminance.Common.VerletIntergration;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Animation = AcidicBosses.Core.Animation.Animation;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace AcidicBosses.Content.Bosses.Twins;

public class TwinsController : AcidicNPC
{
    public override string Texture => TextureRegistry.InvisPath;

    private int spazId
    {
        get => (int) NPC.ai[0];
        set => NPC.ai[0] = value;
    }

    private int retId
    {
        get => (int) NPC.ai[1];
        set => NPC.ai[1] = value;
    }

    private Retinazer Retinazer => Main.npc[retId].GetGlobalNPC<Retinazer>();
    private Spazmatism Spazmatism => Main.npc[spazId].GetGlobalNPC<Spazmatism>();

    private float AverageLifePercent => (Spazmatism.Npc.GetLifePercent() + Retinazer.Npc.GetLifePercent()) / 2f;

    // Only exists on client
    private List<VerletSegment> connectionSegments = [];
    private VerletSettings connectionSimSettings = new()
    {
        ConserveEnergy = true
    };
    
    private Texture2D connectionTex = TextureAssets.Chain12.Value; // The sprite is oriented vertically
    private Asset<Texture2D> connectionTexAsset = TextureAssets.Chain12;
    private int connectionLength = 500;

    public override void SetDefaults()
    {
        NPC.damage = 0;
        NPC.defense = 0;
        NPC.lifeMax = 1;
        NPC.life = 1;
        NPC.dontTakeDamage = true;

        NPC.width = 0;
        NPC.height = 0;
    }

    #region AI

    private AttackManager attackManager = new();

    private PhaseTracker phaseTracker;

    public override void OnFirstFrame()
    {
        NPC.TargetClosest();
        NPC.position = Main.player[NPC.target].position;

        phaseTracker = new PhaseTracker([
            PhaseUntransformed,
            PhaseTranformation,
            PhaseTransformed1
        ]);

        // Fill out the connector on the client
        if (Main.netMode != NetmodeID.Server) FillTether();
    }

    public override void AcidAI()
    {
        if (CheckTwinsDead())
        {
            NPC.active = false;
            return;
        }
        
        // If a player joins mid-fight, they need to create the tether on their screen.
        if (Main.netMode != NetmodeID.Server && connectionSegments.Count == 0) FillTether();
        
        attackManager.PreAttackAi();

        NPC.TargetClosest();
        var target = Main.player[NPC.target];
        NPC.Center = target.Center;
        
        phaseTracker.RunPhaseAI();
        
        attackManager.PostAttackAi();
    }

    private bool CheckTwinsDead()
    {
        return !Spazmatism.Npc.active && !Retinazer.Npc.active;
    }

    private void FillTether()
    {
        connectionSegments.Clear(); // Just to be safe
        
        for (var i = 0; i < connectionLength; i += connectionTex.Height)
        {
            connectionSegments.Add(
                new VerletSegment(new Vector2(Spazmatism.Npc.Center.X + i, Spazmatism.Npc.Center.Y), Vector2.Zero));
        }
            
        // Remove one segment
        connectionSegments.RemoveAt(0);
    }

    /// <summary>
    /// Simulates the two eyes being connected together by a rope
    /// </summary>
    private void SimulateTether(float retMass, float spazMass)
    {
        var distance = Retinazer.Npc.Center.Distance(Spazmatism.Npc.Center);
        if (distance >= connectionLength)
        {
            // Each eye contributes half of their velocity to the other because momentum
            var totalMass = retMass + spazMass;
            var origRetVel = Retinazer.Npc.velocity;
            var origSpazVel = Spazmatism.Npc.velocity;
            Retinazer.Npc.velocity = 
                ((origRetVel * retMass) +
                 Retinazer.Npc.Center.DirectionTo(Spazmatism.Npc.Center) * (origSpazVel.Length() * spazMass)) / totalMass;
            Spazmatism.Npc.velocity =
                (origSpazVel * spazMass +
                 Spazmatism.Npc.Center.DirectionTo(Retinazer.Npc.Center) * (origRetVel.Length() * retMass)) / totalMass;
        }
    }

    #endregion
    
    #region Phases

    private PhaseState PhaseUntransformed => new PhaseState(Phase_Untransformed, EnterPhaseUntransformed);
    private PhaseState PhaseTranformation => new(Phase_Transformation, EnterPhaseTransformation);
    private PhaseState PhaseTransformed1 => new PhaseState(Phase_Transformed1, EnterPhaseTransformed1);
    
    private void EnterPhaseUntransformed()
    {
        var hover = new AttackState(() => Attack_Hover(90, 20, 0.25f), 30);
        var doubleDash = new AttackState(() => Attack_DoubleDash(10, 30, 15, 30), 60);
        var longCrossDash = new AttackState(() => Attack_CrossDash(20, 60, 30), 0);
        var plusDash = new AttackState(() => Attack_PlusDash(20, 60, 25), 0);
        var crossDash = new AttackState(() => Attack_CrossDash(20, 60, 25), 0);
        var alternatingDashes = new AttackState(() => Attack_AlternatingDashes(60 * 4, 10, 30, 15, 20), 0);
        var fastAlternatingDashes = new AttackState(() => Attack_AlternatingFastDashes(60 * 3, 10, 60, 5, 10), 0);
        var recenter = new AttackState(Attack_Recenter, 0);
        
        attackManager.SetAttackPattern([
            hover,
            recenter,
            hover, recenter, hover, recenter,
            doubleDash, doubleDash,
            alternatingDashes,
            fastAlternatingDashes,
            recenter,
            hover,
            doubleDash, doubleDash, doubleDash,
            hover,
            longCrossDash, plusDash, crossDash, plusDash, crossDash,
            recenter,
            hover,
            alternatingDashes
        ]);
    }
    
    private void Phase_Untransformed()
    {
        if (attackManager.InWindDown)
        {
            if (AverageLifePercent <= 0.75f)
            {
                phaseTracker.NextPhase();
                attackManager.Reset();
                return;
            }
            
            Attack_Hover(20f, 0.25f);
            return;
        }
        
        attackManager.RunAttackPattern();
    }

    private Animation transformation;
    private void EnterPhaseTransformation()
    {
        transformation = new Animation();
        
        // Stop Moving
        transformation.AddConstantEvent((progress, frame) =>
        {
            Spazmatism.Npc.SimpleFlyMovement(Vector2.Zero, 0.2f);
            Retinazer.Npc.SimpleFlyMovement(Vector2.Zero, 0.2f);
        });
        
        // Start rumble
        transformation.AddInstantEvent(0, () =>
        {
            ScreenShakeSystem.SetUniversalRumble(0.5f, shakeStrengthDissipationIncrement: 0f);
        });

        // Make Smoke
        transformation.AddTimedEvent(0, 120, (progress, frame) =>
        {
            DoBothTwins(twin =>
            {
                var npc = twin.Npc;
                if (frame % 30 == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item13, npc.Center);
                }

                var smokeDusts = MathHelper.Lerp(2f, 5f, progress);
                for (var i = 0; i < smokeDusts; i++)
                {
                    var speed = Main.rand.NextVector2Circular(5, 5);
                    var pos = Main.rand.NextVector2Circular(npc.width / 2f, npc.height / 2f) + npc.Center;
                    Dust.NewDust(pos, 0, 0, DustID.Smoke, speed.X, speed.Y, Scale: 1.5f);
                }
                if (Main.rand.NextBool(smokeDusts % 1f))
                {
                    var speed = Main.rand.NextVector2Circular(5, 5);
                    var pos = Main.rand.NextVector2Circular(npc.width / 2f, npc.height / 2f) + npc.Center;
                    Dust.NewDust(pos, 0, 0, DustID.Smoke, speed.X, speed.Y, Scale: 1.5f);
                }
                
                var smokeParticles = MathHelper.Lerp(1f, 2f, progress);
                for (var i = 0; i < smokeParticles; i++)
                {
                    var speed = Main.rand.NextVector2Circular(2.5f, 2.5f);
                    var pos = Main.rand.NextVector2Circular(npc.width / 2f, npc.height / 2f) + npc.Center;
                    var rot = Main.rand.NextFloatDirection();
                    var angularVel = Main.rand.NextFloat(0f, MathHelper.Pi / 16f);
                    var puff = new SmallPuffParticle(pos, speed, rot, Color.WhiteSmoke, 30);
                    puff.Opacity = 0.5f;
                    puff.AngularVelocity = angularVel;
                    puff.Spawn();
                }
                if (Main.rand.NextBool(smokeParticles % 1f))
                {
                    var speed = Main.rand.NextVector2Circular(2.5f, 2.5f);
                    var pos = Main.rand.NextVector2Circular(npc.width / 2f, npc.height / 2f) + npc.Center;
                    var rot = Main.rand.NextFloatDirection();
                    var angularVel = Main.rand.NextFloat(0f, MathHelper.Pi / 16f);
                    var puff = new SmallPuffParticle(pos, speed, rot, Color.WhiteSmoke, 30);
                    puff.Opacity = 0.5f;
                    puff.AngularVelocity = angularVel;
                    puff.Spawn();
                }
            });
        });
        
        // Transform twins
        transformation.AddInstantEvent(120, () =>
        {
            ScreenShakeSystem.SetUniversalRumble(0f, shakeStrengthDissipationIncrement: 0.2f);
            ScreenShakeSystem.StartShake(2f);
            
            DoBothTwins(twin =>
            {
                var npc = twin.Npc;
            
                SoundEngine.PlaySound(SoundID.NPCHit1, npc.Center);
                SoundEngine.PlaySound(SoundID.Roar, npc.Center);

                var burst = new BigSmokeDisperseParticle(npc.Bottom, Vector2.Zero, 0f, Color.WhiteSmoke, 60);
                burst.IgnoreLighting = true;
                burst.Scale *= 2.25f;
                burst.Spawn();
            
                for (var i = 0; i < 2; i++)
                {
                    Gore.NewGore(NPC.GetSource_FromAI(), npc.Center, Main.rand.NextVector2Square(-30, 31) * 0.2f, 144);
                    Gore.NewGore(NPC.GetSource_FromAI(), npc.Center, Main.rand.NextVector2Square(-30, 31) * 0.2f, 7);
                    Gore.NewGore(NPC.GetSource_FromAI(), npc.Center, Main.rand.NextVector2Square(-30, 31) * 0.2f, 6);
                }
                for (var i = 0; i < 20; i++)
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                }
                
                var dustId = twin is Spazmatism ? DustID.CursedTorch : DustID.CrimsonTorch;
                for (var i = 0; i < 20; i++)
                {
                    var speed = Main.rand.NextVector2CircularEdge(15f, 15f);
                    Dust.NewDust(npc.Center, 0, 0, dustId, speed.X, speed.Y, Scale: 2f);
                }
            
                npc.HitSound = SoundID.NPCHit4;
                twin.MechForm = true;
            });
        });
    }

    private void Phase_Transformation()
    {
        var done = transformation.RunAnimation();
        if (done)
        {
            phaseTracker.NextPhase();
            attackManager.Reset();
        }
    }
    
    private void EnterPhaseTransformed1()
    {
        var hover = new AttackState(() => Attack_Hover(45, 20, 0.25f), 30);
        var doubleDash = new AttackState(() => Attack_DoubleDash(15, 30, 15, 30), 60);
        var longCrossDash = new AttackState(() => Attack_CrossDash(20, 60, 30), 0);
        var plusDash = new AttackState(() => Attack_PlusDash(20, 60, 25), 0);
        var crossDash = new AttackState(() => Attack_CrossDash(20, 60, 25), 0);
        var alternatingDashes = new AttackState(() => Attack_AlternatingDashes(60 * 4, 10, 30, 15, 20), 0);
        var fastAlternatingDashes = new AttackState(() => Attack_AlternatingFastDashes(60 * 3, 10, 60, 5, 10), 0);
        var recenter = new AttackState(Attack_Recenter, 0);
        
        attackManager.SetAttackPattern([
            hover,
            doubleDash, doubleDash,
            alternatingDashes,
            fastAlternatingDashes,
            recenter,
            hover,
            longCrossDash, plusDash, crossDash, plusDash, crossDash,
            recenter
        ]);
    }
    
    private void Phase_Transformed1()
    {
        Spazmatism.MechForm = true;
        Retinazer.MechForm = true;
        
        if (attackManager.InWindDown)
        {
            var target = Main.player[NPC.target];
            Attack_Hover(30f, 0.3f);
            return;
        }
        
        attackManager.RunAttackPattern();
    }
    
    #endregion
    
    #region Attacks
    
    private void Attack_Hover(Twin twin, float speed, float accel)
    {
        var target = Main.player[NPC.target];
        if (twin == Spazmatism)
        {
            var spazOffset = new Vector2(-200, 0);
            if (Spazmatism.Npc.Center.X > target.Center.X) spazOffset = new Vector2(200, 0);
            FlyTo(Spazmatism.Npc, target.Center + spazOffset, speed, accel);
            Spazmatism.LookTowards(target.Center, 0.05f);
        }
        else
        {
            var retOffset = new Vector2(0, -250);
            if (Retinazer.Npc.Center.Y > target.Center.Y) retOffset = new Vector2(0, 250);
            FlyTo(Retinazer.Npc, target.Center + retOffset, speed, accel);
            Retinazer.LookTowards(target.Center, 0.05f);
        }
    }

    private void Attack_Hover(float speed, float accel)
    {
        Attack_Hover(Spazmatism, speed, accel);
        Attack_Hover(Retinazer, speed, accel);
    }
    
    private bool Attack_Hover(int hoverTime, float speed, float acceleration)
    {
        attackManager.CountUp = true;

        Attack_Hover(speed, acceleration);

        if (attackManager.AiTimer <= hoverTime) return false;

        attackManager.CountUp = false;
        return true;
    }
    
    private void Attack_Pull(Twin leadTwin)
    {
        switch (leadTwin.Npc.type)
        {
            case NPCID.Spazmatism:
                SimulateTether(1, 10);
                break;
            case NPCID.Retinazer:
                SimulateTether(10, 1);
                break;
            default:
                throw new Exception($"{leadTwin.Npc.whoAmI} is somehow not a twin?");
        }
    }

    private bool Attack_Recenter()
    {
        var target = Main.player[NPC.target];
        
        var spazOffset = new Vector2(-400, 0);
        if (Spazmatism.Npc.Center.X > target.Center.X) spazOffset = new Vector2(400, 0);
        Teleport(Spazmatism, target.Center + spazOffset, 20);
        Spazmatism.LookTowards(target.Center, 0.05f);
        
        var retOffset = new Vector2(0, -450);
        // if (Retinazer.Npc.Center.Y > target.Center.Y) retOffset = new Vector2(0, 450);
        Teleport(Retinazer, target.Center + retOffset, 20);
        Retinazer.LookTowards(target.Center, 0.05f);

        return true;
    }

    private DashState Attack_Dash(Twin twin, int dashLength, float speed, int trackTime, int dashAtTime, float distance)
    {
        var options = new DashOptions
        {
            MinimumDistance = distance,
            DashSpeed = speed,
            DashLength = dashLength,
            LookOffset = MathHelper.PiOver2,
            TrackTime = trackTime,
            DashAtTime = dashAtTime
        };
        
        var target = Main.player[NPC.target];

        var dashState = Dash(twin, twin.AttackManager, target.Center, options);

        return dashState;
    }
    
    private DashState Attack_TrackingDash(Twin twin, int dashLength, float speed, int trackTime, int dashAtTime, float distance)
    {
        var options = new DashOptions
        {
            MinimumDistance = distance,
            DashSpeed = speed,
            DashLength = dashLength,
            LookOffset = MathHelper.PiOver2,
            TrackTime = trackTime,
            DashAtTime = dashAtTime
        };
        
        var npc = twin.Npc;
        var target = Main.player[NPC.target];
        
        var trackedPos = target.Center;
        trackedPos += target.velocity * trackTime; // Lead player pos
        var travelTime = npc.Distance(trackedPos) / speed;
        trackedPos += target.velocity * travelTime; // Account for travel time

        var dashState = Dash(twin, twin.AttackManager, trackedPos, options);
        
        return dashState;
    }
    
    private bool Attack_DoubleDash(int dashLength, float speed, int trackTime, int dashAtTime)
    {
        attackManager.CountUp = true;

        ref var spazDone = ref NPC.localAI[0];
        ref var retDone = ref NPC.localAI[1];
        if (attackManager.AiTimer == 0)
        {
            Spazmatism.AttackManager.AiTimer = 0;
            Retinazer.AttackManager.AiTimer = 0;
            spazDone = 0;
            retDone = 0;
        }
        
        // Spaz has a normal dash while Ret tracks the player and dashes later
        if (spazDone == 0)
        {
            var spazDashState = Attack_Dash(Spazmatism, dashLength, speed, trackTime, dashAtTime, 300);
            if (spazDashState == DashState.Done) spazDone = 1;
        }

        if (retDone == 0)
        {
            var retDashState = Attack_TrackingDash(Retinazer, dashLength, speed, trackTime * 2, (int) (dashAtTime * 2f), 300);
            if (retDashState == DashState.Done) retDone = 1;
        }
        
        if (spazDone != 0) Attack_Hover(Spazmatism, 20, 0.25f);
        if (retDone != 0) Attack_Hover(Retinazer, 20, 0.25f);

        if (spazDone == 0 || retDone == 0) return false;
        
        spazDone = 0;
        retDone = 0;
        attackManager.CountUp = false;
        return true;

    }

    private bool Attack_PlusDash(int dashLength, float speed, int dashAtTime)
    {
        var dashOptions = new DashOptions
        {
            DashLength = dashLength,
            DashSpeed = speed,
            LookOffset = MathHelper.PiOver2,
            DashAtTime = dashAtTime,
            MinimumDistance = 0,
            DontReposition = true,
            TrackTime = 0
        };

        var target = Main.player[NPC.target];

        if (attackManager.AiTimer == 0)
        {
            var spazPos = target.Center + new Vector2(350, 0);
            if (Spazmatism.Npc.Center.X < target.Center.X) spazPos = target.Center - new Vector2(350, 0);
            var retPos = target.Center + new Vector2(0, 350);
            if (Retinazer.Npc.Center.Y > target.Center.Y) retPos = target.Center - new Vector2(0, 350);
            
            Spazmatism.LookTowards(spazPos, 1f);
            Retinazer.LookTowards(retPos, 1f);
            
            Teleport(Spazmatism, spazPos, 0);
            Teleport(Retinazer, retPos, 0);
            
            Spazmatism.LookTowards(target.Center, 1f);
            Retinazer.LookTowards(target.Center, 1f);
            
            Spazmatism.Npc.velocity = Vector2.Zero;
            Retinazer.Npc.velocity = Vector2.Zero;
        }
        
        // Dashes are perfectly synced together because there's no repositioning
        var dashState = Dash(Spazmatism, attackManager, target.Center, dashOptions);
        Dash(Retinazer, attackManager, target.Center, dashOptions);

        return dashState == DashState.Done;
    }
    
    private bool Attack_CrossDash(int dashLength, float speed, int dashAtTime)
    {
        var dashOptions = new DashOptions
        {
            DashLength = dashLength,
            DashSpeed = speed,
            LookOffset = MathHelper.PiOver2,
            DashAtTime = dashAtTime,
            MinimumDistance = 0,
            DontReposition = true,
            TrackTime = 0
        };

        var target = Main.player[NPC.target];

        if (attackManager.AiTimer == 0)
        {
            var spazPos = target.Center + new Vector2(300, -300);
            if (Spazmatism.Npc.Center.Y < target.Center.Y) spazPos = target.Center - new Vector2(300, -300);
            var retPos = target.Center + new Vector2(-300, -300);
            if (Retinazer.Npc.Center.Y < target.Center.Y) retPos = target.Center - new Vector2(-300, -300);
            
            Spazmatism.LookTowards(spazPos, 1f);
            Retinazer.LookTowards(retPos, 1f);
            
            Teleport(Spazmatism, spazPos, 0);
            Teleport(Retinazer, retPos, 0);
            
            Spazmatism.LookTowards(target.Center, 1f);
            Retinazer.LookTowards(target.Center, 1f);
            
            Spazmatism.Npc.velocity = Vector2.Zero;
            Retinazer.Npc.velocity = Vector2.Zero;
        }
        
        // Dashes are perfectly synced together because there's no repositioning
        var dashState = Dash(Spazmatism, attackManager, target.Center, dashOptions);
        Dash(Retinazer, attackManager, target.Center, dashOptions);
        
        return dashState == DashState.Done;
    }

    private bool Attack_AlternatingDashes(int length, int dashLength, float speed, int trackTime, int dashAtTime)
    {
        attackManager.CountUp = true;
        
        ref var turn = ref NPC.localAI[0];
        Twin dashingTwin = turn == 0 ? Spazmatism : Retinazer;
        Twin notDashingTwin = turn == 0 ? Retinazer : Spazmatism;

        Attack_Hover(notDashingTwin, 20f, 0.2f);
        
        var target = Main.player[NPC.target];
        var dashOptions = new DashOptions
        {
            DashLength = dashLength,
            DashSpeed = speed,
            LookOffset = MathHelper.PiOver2,
            DashAtTime = dashAtTime,
            MinimumDistance = 250,
            TrackTime = trackTime
        };

        var dashState = Dash(dashingTwin, dashingTwin.AttackManager, target.Center, dashOptions);

        if (dashState == DashState.Done)
        {
            turn = ((int) turn + 1) % 2;
            dashingTwin.AttackManager.Reset();

            if (attackManager.AiTimer >= length)
            {
                attackManager.CountUp = false;
                return true;
            }
        }

        return false;
    }

    private bool Attack_AlternatingFastDashes(int length, int dashLength, float speed, int trackTime, int dashAtTime)
    {
        attackManager.CountUp = true;
        
        if (attackManager.AiTimer == 0) SoundEngine.PlaySound(SoundID.ForceRoarPitched);
        
        ref var turn = ref NPC.localAI[0];
        Twin dashingTwin = turn == 0 ? Spazmatism : Retinazer;
        Twin notDashingTwin = turn == 0 ? Retinazer : Spazmatism;
        
        var target = Main.player[NPC.target];
        var dashOptions = new DashOptions
        {
            DashLength = dashLength,
            DashSpeed = speed,
            LookOffset = MathHelper.PiOver2,
            DashAtTime = dashAtTime,
            MinimumDistance = 0,
            DontReposition = true,
            TrackTime = trackTime
        };

        if (dashingTwin.AttackManager.AiTimer == 0)
        {
            var dest = new Vector2();
            if (dashingTwin is Spazmatism) dest = target.Center + new Vector2(0, 350);
            else dest = target.Center + new Vector2(0, -350);
            
            dashingTwin.LookTowards(dest, 1f);
            Teleport(dashingTwin, dest, 0);
            dashingTwin.LookTowards(target.Center, 1f);

            dashingTwin.Npc.velocity = Vector2.Zero;
        }
        
        var dashState = Dash(dashingTwin, dashingTwin.AttackManager, target.Center, dashOptions);

        if (dashState == DashState.Done)
        {
            turn = ((int) turn + 1) % 2;
            dashingTwin.AttackManager.Reset();

            if (attackManager.AiTimer >= length)
            {
                attackManager.CountUp = false;
                return true;
            }
        }

        return false;
    }

    private DashState Dash(Twin twin, AttackManager am, Vector2 dashTarget, DashOptions options)
    {
        var npc = twin.Npc;
        var state = DashHelper.Dash(twin.Npc, am, dashTarget, options);
        
        if (am.AiTimer == 0)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NewDashLine(twin, npc.Center, MathHelper.PiOver2, options.DashAtTime);
            }
        }
        
        // Lots of FX
        if (state == DashState.StartingDash)
        {
            DashFx(twin);
            twin.UseAfterimages = true;
        }
        
        if (state == DashState.Done)
        {
            twin.UseAfterimages = false;
        }

        return state;
    }

    // Dash effects
    private void DashFx(Twin twin)
    {
        var npc = twin.Npc;
        
        // Smoke ring
        var ring = new SmokeRingParticle(npc.Center, -npc.velocity * 0.25f, npc.rotation, Color.White, 30);
        ring.Opacity = 0.5f;
        ring.Scale *= 2f;
        ring.Spawn();

        // Sparks
        for (var i = 0; i < 20; i++)
        {
            var dustId = twin is Spazmatism ? DustID.CursedTorch : DustID.CrimsonTorch;
            var velOffset = Main.rand.NextVector2Circular(10f, 10f);
                
            Dust.NewDustDirect(npc.Center, 0, 0, dustId, 
                -(npc.velocity.X * 0.75f) + velOffset.X,
                -(npc.velocity.Y * 0.75f) + velOffset.Y, Scale: 1.5f);
        }
            
        // Smoke dust
        for (var i = 0; i < 20; i++)
        {
            var velOffset = Main.rand.NextVector2Circular(15f, 15f);
            Dust.NewDustDirect(npc.Center, 0, 0, DustID.Smoke, 
                -(npc.velocity.X * 0.75f) + velOffset.X,
                -(npc.velocity.Y * 0.75f) + velOffset.Y, Scale: 2f);
        }

        // Lighting
        var color = twin is Spazmatism ? Color.Lime : Color.Red;
        Lighting.AddLight(npc.Center - npc.velocity * 5f, color.ToVector3());
            
        SoundEngine.PlaySound(SoundID.Item89, npc.Center);
    }

    private void Teleport(Twin twin, Vector2 position, float recoil)
    {
        var npc = twin.Npc;
        var awayDir = npc.DirectionTo(position);
        var startPos = npc.Center;

        npc.rotation = awayDir.ToRotation() - MathHelper.PiOver2;
        DashFx(twin);
        
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            NewAfterimage(twin, startPos, position);
        }
        
        npc.Center = position;
        npc.velocity = awayDir * recoil;

        var disperse = new BigSmokeDisperseParticle(npc.Center, Vector2.Zero, 0f, Color.WhiteSmoke, 30);
        disperse.Scale *= 2f;
        disperse.Opacity = 0.5f;
        disperse.Spawn();
    }
    
    #endregion
    
    #region Projectiles
    
    private Projectile NewDashLine(Twin twin, Vector2 position, float offset, int lifetime, bool anchorToBoss = true)
    {
        var ai1 = anchorToBoss ? twin.Npc.whoAmI : -1;
        return BaseLineProjectile.Create<TwinsDashLine>(NPC.GetSource_FromAI(), position, offset, lifetime, ai1);
    }
    
    private Projectile NewAfterimage(Twin twin, Vector2 startPos, Vector2 endPos)
    {
        return NpcAfterimageTrail.Create(NPC.GetSource_FromAI(), startPos, endPos, twin.Npc.whoAmI);
    }

    private Projectile NewSpazFlamethrower(Vector2 pos, float rotation)
    {
        return BaseBetsyFlame.Create<SpazFlamethrower>(NPC.GetSource_FromAI(), pos, rotation - MathHelper.PiOver2, Spazmatism.Npc.damage, 4, Spazmatism.Npc.whoAmI);
    }

    private Projectile NewRetDeathray(Vector2 position, float rotation)
    {
        return BaseLineProjectile.Create<RetDeathrayIndicator>(NPC.GetSource_FromAI(), position, rotation, 30,
            Retinazer.Npc.whoAmI);
    }
    
    #endregion

    private static void FlyTo(NPC npc, Vector2 target, float speed, float acceleration)
    {
        npc.SimpleFlyMovement(npc.DirectionTo(target) * speed, acceleration);
    }

    public override void SendAcidAI(BinaryWriter binaryWriter)
    {
        attackManager.Serialize(binaryWriter);
        phaseTracker.Serialize(binaryWriter);
    }

    public override void ReceiveAcidAI(BinaryReader binaryReader)
    {
        attackManager.Deserialize(binaryReader);
        phaseTracker.Deserialize(binaryReader);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        // Yes I'm running this every frame. If I don't the rope is jittery.
        // You can't stop me
        connectionSegments = VerletSimulations.RopeVerletSimulation(connectionSegments, Retinazer.Npc.Center,
            connectionLength * 0.75f, connectionSimSettings, Spazmatism.Npc.Center);

        if (!ShouldDrawTether()) return false;
        
        var renderSettings = new PrimitiveSettings(
            _ => connectionTex.Width / 2f,
            p =>
            {
                var index = (int) (p * connectionSegments.Count);
                if (index == connectionSegments.Count) index--;
                return Lighting.GetColor(connectionSegments[index].Position.ToTileCoordinates());
            },
            Shader: ShaderManager.GetShader("AcidicBosses.Rope")
        );
        renderSettings.Shader.SetTexture(connectionTexAsset, 1, SamplerState.PointClamp);
        renderSettings.Shader.TrySetParameter("segments", connectionSegments.Count);
    
        PrimitiveRenderer.RenderTrail(connectionSegments.Select(s => s.Position), renderSettings);
        
        spriteBatch.DrawString(FontAssets.MouseText.Value, attackManager.AiTimer.ToString(), NPC.position - Main.screenPosition + new Vector2(50, 50), Color.White);

        return false;
    }

    private bool ShouldDrawTether()
    {
        if (!Retinazer.Npc.active || !Spazmatism.Npc.active) return false;

        return true;
    }

    private void DoBothTwins(Action<Twin> action)
    {
        action(Spazmatism);
        action(Retinazer);
    }

    public static int Link(NPC npc)
    {
        var controller = NPC.FindFirstNPC(ModContent.NPCType<TwinsController>());
        if (controller != -1 && Main.npc[controller].active)
        {
            return controller;
        }

        return NPC.NewNPC(npc.GetSource_FromAI(), (int) npc.position.X, (int) npc.position.Y,
            ModContent.NPCType<TwinsController>(), npc.whoAmI);
    }
}