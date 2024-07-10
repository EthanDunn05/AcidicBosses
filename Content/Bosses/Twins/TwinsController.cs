using System.IO;
using AcidicBosses.Common.Textures;
using AcidicBosses.Core.StateManagement;
using Microsoft.Xna.Framework;
using Terraria;
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
        
    }

    public override void AcidAI()
    {
        spazAttackManager.PreAttackAi();
        retAttackManager.PreAttackAi();
        
        NPC.TargetClosest();
        NPC.position = Main.player[NPC.target].position;

        var target = Main.player[NPC.target];
        var offset = new Vector2(200, 0);
        var height = new Vector2(0, -250);

        FlyTo(Retinazer.Npc, target.Center + offset + height, 30, 0.25f);
        Retinazer.LookTowards(target.Center, 0.05f);
        
        FlyTo(Spazmatism.Npc, target.Center - offset + height, 30, 0.25f);
        Spazmatism.LookTowards(target.Center, 0.05f);
        
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

    public static int Link(NPC npc)
    {
        var controller = NPC.FindFirstNPC(ModContent.NPCType<TwinsController>());
        if (controller != -1 && Main.npc[controller].active)
        {
            return controller;
        }
        
        return NPC.NewNPC(npc.GetSource_FromAI(), (int) npc.position.X, (int) npc.position.Y, ModContent.NPCType<TwinsController>(), npc.whoAmI);
    }
}