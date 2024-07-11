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
        }
    }

    public override void AcidAI()
    {
        spazAttackManager.PreAttackAi();
        retAttackManager.PreAttackAi();

        NPC.TargetClosest();
        var target = Main.player[NPC.target];
        var offset = new Vector2(200, 0);
        var height = new Vector2(0, -250);

        // FlyTo(Retinazer.Npc, target.Center + offset + height, 20, 0.1f);
        // FlyTo(Spazmatism.Npc, target.Center - offset + height, 20, 0.1f);
        // Retinazer.LookTowards(target.Center, 0.05f);
        // Spazmatism.LookTowards(target.Center, 0.05f);

        

        spazAttackManager.PostAttackAi();
        spazAttackManager.PostAttackAi();
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
            connectionLength, connectionSimSettings, Spazmatism.Npc.Center);
        
        // Don't draw the connection if they are too far away from each other
        if (Retinazer.Npc.Center.Distance(Spazmatism.Npc.Center) >= connectionLength * 1.5f) return false;
        
        
        var renderSettings = new PrimitiveSettings(
            _ => connectionTex.Width, 
            _ => Color.White
        );
        renderSettings.Shader.SetTexture(connectionTexAsset, 1, SamplerState.PointClamp);
    
        PrimitiveRenderer.RenderTrail(connectionSegments.Select(s => s.Position), renderSettings, connectionSegments.Count * 2);
        // for (var i = 0; i < connectionSegments.Count; i++)
        // {
        //     var segment = connectionSegments[i];
        //
        //     var nextPos = i != connectionSegments.Count - 1
        
        //         ? connectionSegments[i + 1].Position
        //         : Spazmatism.Npc.Center;
        //     
        //     
        //
        //     var rotation = segment.Position.DirectionTo(nextPos).ToRotation() + MathHelper.PiOver2;
        //     var scale = segment.Position.Distance(nextPos);
        //     scale /= connectionTex.Height;
        //
        //     var pos = segment.Position - screenPos;
        //     var frame = connectionTex.Frame();
        //     var origin = new Vector2(frame.Width / 2f, 0f);
        //
        //     Main.EntitySpriteDraw(connectionTex, pos, frame, Color.White, rotation, origin, new Vector2(1f, scale), SpriteEffects.None);
        // }

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