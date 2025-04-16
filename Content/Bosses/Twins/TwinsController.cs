using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AcidicBosses.Common.Textures;
using AcidicBosses.Core.StateManagement;
using Luminance.Common.StateMachines;
using Luminance.Common.VerletIntergration;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController : AcidicNPC
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

    public override void SetStaticDefaults()
    {
        // Yeet the bestiary entry
        var bestiary = new NPCID.Sets.NPCBestiaryDrawModifiers()
        {
            Hide = true
        };
        
        NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, bestiary);
    }

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

    private bool changedState = false;

    public override void OnFirstFrame()
    {
        NPC.TargetClosest();
        NPC.position = Main.player[NPC.target].position;

        // Only these three are in order. The rest are managed by the phase ai
        phaseTracker = new PhaseTracker([
            PhaseUntransformed,
            PhaseTransformation,
            PhaseTransformed1,
            PhaseTransformed2,
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

        // Despawn when day
        if (Main.IsItDay())
        {
            Flee();
        }
        
        // Despawn when no targets
        if (!target.active || target.dead)
        {
            NPC.TargetClosest();
            target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                Flee();
            }
        }

        if (!Spazmatism.Npc.active && !changedState)
        {
            phaseTracker.ChangeState(PhaseSoloRet);
            changedState = true;
        }
        if (!Retinazer.Npc.active && !changedState)
        {
            phaseTracker.ChangeState(PhaseSoloSpaz);
            changedState = true;
        }
        
        phaseTracker.RunPhaseAI();
        
        attackManager.PostAttackAi();
    }

    private bool CheckTwinsDead()
    {
        return !Spazmatism.Npc.active && !Retinazer.Npc.active;
    }

    private void Flee()
    {
        Spazmatism.Npc.active = false;
        Retinazer.Npc.active = false;
        NPC.active = false;
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
        // Don't draw on the first frame to fix null errors
        if (IsFirstFrame) return false;
        
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
        
        // spriteBatch.DrawString(FontAssets.MouseText.Value, attackManager.AiTimer.ToString(), NPC.position - Main.screenPosition + new Vector2(50, 50), Color.White);

        return false;
    }

    private bool ShouldDrawTether()
    {
        if (!Retinazer.Npc.active || !Spazmatism.Npc.active) return false;

        return true;
    }

    // Returns null when both are alive
    private Twin? AliveTwin()
    {
        if (Spazmatism.Npc.active && Retinazer.Npc.active) return null;
        if (Spazmatism.Npc.active) return Spazmatism;
        return Retinazer;
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