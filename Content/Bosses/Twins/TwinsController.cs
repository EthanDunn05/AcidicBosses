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
using AcidicBosses.Helpers.NpcHelpers;
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
    private PhaseState PhaseTransformed1 => new PhaseState(Phase_Transformed1, EnterPhaseTransformed1);
    
    private void EnterPhaseUntransformed()
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
    
    private void Phase_Untransformed()
    {
        if (attackManager.InWindDown)
        {
            if (Spazmatism.Npc.GetLifePercent() + Retinazer.Npc.GetLifePercent() / 2f <= 0.75f)
            {
                phaseTracker.NextPhase();
                attackManager.Reset();
                return;
            }
            
            var target = Main.player[NPC.target];
            Attack_Hover(20f, 0.25f);
            return;
        }
        
        attackManager.RunAttackPattern();
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

    private DashState Attack_Dash(Twin twin, int dashLength, float speed, int trackTime, int dashAtTime, bool enraged, float distance)
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

        var dashState = DashHelper.Dash(npc, twin.AttackManager, target.Center, options);

        if (twin.AttackManager.AiTimer == 0)
        {
            NewDashLine(twin, npc.Center, MathHelper.PiOver2, dashAtTime);
        }
        
        if (enraged)
        {
            if (dashState == DashState.StartingDash)
            {
                SoundEngine.PlaySound(SoundID.Item89, npc.Center);
                twin.UseAfterimages = true;
            }
            if (dashState == DashState.Done)
            {
                twin.UseAfterimages = false;
            }
        }

        return dashState;
    }
    
    private DashState Attack_TrackingDash(Twin twin, int dashLength, float speed, int trackTime, int dashAtTime, bool enraged, float distance)
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

        var dashState = DashHelper.Dash(npc, twin.AttackManager, trackedPos, options);
        
        if (twin.AttackManager.AiTimer == 0)
        {
            NewDashLine(twin, npc.Center, MathHelper.PiOver2, dashAtTime);
        }
        
        if (enraged)
        {
            if (dashState == DashState.StartingDash)
            {
                SoundEngine.PlaySound(SoundID.Item89, npc.Center);
                twin.UseAfterimages = true;
            }
            if (dashState == DashState.Done)
            {
                twin.UseAfterimages = false;
            }
        }
        
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
            var spazDashState = Attack_Dash(Spazmatism, dashLength, speed, trackTime, dashAtTime, true, 300);
            if (spazDashState == DashState.Done) spazDone = 1;
        }

        if (retDone == 0)
        {
            var retDashState = Attack_TrackingDash(Retinazer, dashLength, speed, trackTime * 2, (int) (dashAtTime * 2f), true, 300);
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
            var spazPos = target.Center + new Vector2(500, 0);
            if (Spazmatism.Npc.Center.X < target.Center.X) spazPos = target.Center - new Vector2(500, 0);
            var retPos = target.Center + new Vector2(0, 500);
            if (Retinazer.Npc.Center.Y > target.Center.Y) retPos = target.Center - new Vector2(0, 500);
            
            Spazmatism.LookTowards(spazPos, 1f);
            Retinazer.LookTowards(retPos, 1f);
            
            Teleport(Spazmatism, spazPos, 0);
            Teleport(Retinazer, retPos, 0);
            
            Spazmatism.LookTowards(target.Center, 1f);
            Retinazer.LookTowards(target.Center, 1f);
            
            Spazmatism.Npc.velocity = Vector2.Zero;
            Retinazer.Npc.velocity = Vector2.Zero;

            NewDashLine(Spazmatism, spazPos, MathHelper.PiOver2, dashAtTime);
            NewDashLine(Retinazer, retPos, MathHelper.PiOver2, dashAtTime);
        }
        
        // Dashes are perfectly synced together because there's no repositioning
        var dashState = DashHelper.Dash(Spazmatism.Npc, attackManager, target.Center, dashOptions);
        DashHelper.Dash(Retinazer.Npc, attackManager, target.Center, dashOptions);

        if (dashState == DashState.StartingDash)
        {
            Spazmatism.UseAfterimages = true;
            Retinazer.UseAfterimages = true;
            SoundEngine.PlaySound(SoundID.Item89, Spazmatism.Npc.Center);
            SoundEngine.PlaySound(SoundID.Item89, Retinazer.Npc.Center);
        }
        
        if (dashState == DashState.Done)
        {
            Spazmatism.UseAfterimages = false;
            Retinazer.UseAfterimages = false;
        }

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
            var spazPos = target.Center + new Vector2(500, -500);
            if (Spazmatism.Npc.Center.Y < target.Center.Y) spazPos = target.Center - new Vector2(500, -500);
            var retPos = target.Center + new Vector2(-500, -500);
            if (Retinazer.Npc.Center.Y < target.Center.Y) retPos = target.Center - new Vector2(-500, -500);
            
            Spazmatism.LookTowards(spazPos, 1f);
            Retinazer.LookTowards(retPos, 1f);
            
            Teleport(Spazmatism, spazPos, 0);
            Teleport(Retinazer, retPos, 0);
            
            Spazmatism.LookTowards(target.Center, 1f);
            Retinazer.LookTowards(target.Center, 1f);
            
            Spazmatism.Npc.velocity = Vector2.Zero;
            Retinazer.Npc.velocity = Vector2.Zero;

            NewDashLine(Spazmatism, spazPos, MathHelper.PiOver2, dashAtTime);
            NewDashLine(Retinazer, retPos, MathHelper.PiOver2, dashAtTime);
        }
        
        // Dashes are perfectly synced together because there's no repositioning
        var dashState = DashHelper.Dash(Spazmatism.Npc, attackManager, target.Center, dashOptions);
        DashHelper.Dash(Retinazer.Npc, attackManager, target.Center, dashOptions);
        
        if (dashState == DashState.StartingDash)
        {
            Spazmatism.UseAfterimages = true;
            Retinazer.UseAfterimages = true;
            SoundEngine.PlaySound(SoundID.Item89, Spazmatism.Npc.Center);
            SoundEngine.PlaySound(SoundID.Item89, Retinazer.Npc.Center);
        }

        if (dashState == DashState.Done)
        {
            Spazmatism.UseAfterimages = false;
            Retinazer.UseAfterimages = false;
        }
        
        return dashState == DashState.Done;
    }

    private bool Attack_AlternatingDashes(int length, int dashLength, float speed, int trackTime, int dashAtTime)
    {
        attackManager.CountUp = true;
        
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
            MinimumDistance = 250,
            TrackTime = trackTime
        };

        var dashState = DashHelper.Dash(dashingTwin.Npc, dashingTwin.AttackManager, target.Center, dashOptions);

        if (dashingTwin.AttackManager.AiTimer == 0)
        {
            NewDashLine(dashingTwin, dashingTwin.Npc.Center, MathHelper.PiOver2, dashAtTime);
        }
        
        if (dashState == DashState.StartingDash)
        {
            dashingTwin.UseAfterimages = true;
            SoundEngine.PlaySound(SoundID.Item89, dashingTwin.Npc.Center);
        }

        if (dashState == DashState.Done)
        {
            turn = ((int) turn + 1) % 2;
            dashingTwin.AttackManager.Reset();
            dashingTwin.UseAfterimages = false;

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

        var dashState = DashHelper.Dash(dashingTwin.Npc, dashingTwin.AttackManager, target.Center, dashOptions);

        if (dashingTwin.AttackManager.AiTimer == 0)
        {
            var dest = new Vector2();
            if (dashingTwin is Spazmatism) dest = target.Center + new Vector2(0, 500);
            else dest = target.Center + new Vector2(0, -500);
            
            dashingTwin.LookTowards(dest, 1f);
            Teleport(dashingTwin, dest, 0);
            dashingTwin.LookTowards(target.Center, 1f);

            dashingTwin.Npc.velocity = Vector2.Zero;

            NewDashLine(dashingTwin, dashingTwin.Npc.Center, MathHelper.PiOver2, dashAtTime);
        }
        
        if (dashState == DashState.StartingDash)
        {
            dashingTwin.UseAfterimages = true;
            SoundEngine.PlaySound(SoundID.Item89, dashingTwin.Npc.Center);
        }

        if (dashState == DashState.Done)
        {
            turn = ((int) turn + 1) % 2;
            dashingTwin.AttackManager.Reset();
            dashingTwin.UseAfterimages = false;

            if (attackManager.AiTimer >= length)
            {
                attackManager.CountUp = false;
                return true;
            }
        }

        return false;
    }

    private void Teleport(Twin twin, Vector2 position, float recoil)
    {
        var npc = twin.Npc;
        var awayDir = npc.DirectionTo(position);
        var startPos = npc.Center;
        
        
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            NewAfterimage(twin, startPos, position, 60);
        }
        
        npc.Center = position;
        npc.velocity = awayDir * recoil;
        
        SoundEngine.PlaySound(SoundID.Item82, npc.Center);
    }
    
    #endregion
    
    #region Projectiles
    
    private Projectile NewDashLine(Twin twin, Vector2 position, float offset, int lifetime, bool anchorToBoss = true)
    {
        var ai1 = anchorToBoss ? twin.Npc.whoAmI : -1;
        return BaseLineProjectile.Create<TwinsDashLine>(NPC.GetSource_FromAI(), position, offset, lifetime, ai1);
    }
    
    private Projectile NewAfterimage(Twin twin, Vector2 startPos, Vector2 endPos, int lifetime)
    {
        return NpcAfterimageTrail.Create(NPC.GetSource_FromAI(), startPos, endPos, twin.Npc.whoAmI, lifetime);
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