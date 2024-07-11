using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AcidicBosses.Common.Effects;
using AcidicBosses.Common.Textures;
using AcidicBosses.Core.StateManagement;
using Luminance.Common.VerletIntergration;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.ModLoader;

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
    
    private Texture2D connectionTex = TextureAssets.Chain12.Value; // Note the sprite is oriented vertically
    private Asset<Texture2D> connectionTexAsset = TextureAssets.Chain12; // Note the sprite is oriented vertically
    private int connectionLength = 300;

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

    private AttackManager spazAttackManager = new();
    private AttackManager retAttackManager = new();

    private PhaseTracker spazPhaseTracker;
    private PhaseTracker retPhaseTracker;

    public override void OnFirstFrame()
    {
        NPC.TargetClosest();

        NPC.position = Main.player[NPC.target].position;

        var target = Main.player[NPC.target];
        var offset = new Vector2(200, 0);
        var height = new Vector2(0, -250);

        // Retinazer.Npc.Center = target.Center + offset + height;
        // Spazmatism.Npc.Center = target.Center - offset + height;

        if (Main.netMode != NetmodeID.Server)
        {
            for (var i = 0; i < connectionLength; i += connectionTex.Height)
            {
                connectionSegments.Add(
                    new VerletSegment(new Vector2(Spazmatism.Npc.Center.X + i, Spazmatism.Npc.Center.Y), Vector2.Zero));
            }
            
            // Remove one segment
            connectionSegments.RemoveAt(0);
        }
    }

    public override void AcidAI()
    {
        spazAttackManager.PreAttackAi();
        retAttackManager.PreAttackAi();

        NPC.TargetClosest();
        var target = Main.player[NPC.target];
        NPC.Center = target.Center;
        var offset = new Vector2(200, 0);
        var height = new Vector2(0, -250);

        Spazmatism.Mass = 10f;
        Retinazer.Mass = 1f;
    
        SimulateTether();    
        
        spazAttackManager.PostAttackAi();
        spazAttackManager.PostAttackAi();
    }

    /// <summary>
    /// Simulates the two eyes being connected together by a rope
    /// </summary>
    private void SimulateTether()
    {
        var distance = Retinazer.Npc.Center.Distance(Spazmatism.Npc.Center);
        if (distance >= connectionLength)
        {
            // Each eye contributes half of their velocity to the other because momentum
            var totalMass = Retinazer.Mass + Spazmatism.Mass;
            var origRetVel = Retinazer.Npc.velocity;
            var origSpazVel = Spazmatism.Npc.velocity;
            Retinazer.Npc.velocity = 
                ((origRetVel * Retinazer.Mass) +
                 Retinazer.Npc.Center.DirectionTo(Spazmatism.Npc.Center) * (origSpazVel.Length() * Spazmatism.Mass)) / totalMass;
            Spazmatism.Npc.velocity =
                (origSpazVel * Spazmatism.Mass +
                 Spazmatism.Npc.Center.DirectionTo(Retinazer.Npc.Center) * (origRetVel.Length() * Retinazer.Mass)) / totalMass;
        }
    }

    #endregion

    private static void FlyTo(NPC npc, Vector2 target, float speed, float acceleration)
    {
        npc.SimpleFlyMovement(npc.DirectionTo(target) * speed, acceleration);
    }

    public override void SendAcidAI(BinaryWriter binaryWriter)
    {
        spazAttackManager.Serialize(binaryWriter);
        retAttackManager.Serialize(binaryWriter);
    }

    public override void ReceiveAcidAI(BinaryReader binaryReader)
    {
        spazAttackManager.Deserialize(binaryReader);
        retAttackManager.Deserialize(binaryReader);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        // Yes I'm running this every frame. If I don't the rope is jittery
        connectionSegments = VerletSimulations.RopeVerletSimulation(connectionSegments, Retinazer.Npc.Center,
            connectionLength * 0.75f, connectionSimSettings, Spazmatism.Npc.Center);
        
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

        return false;
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