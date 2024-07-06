using System;
using System.IO;
using AcidicBosses.Common;
using AcidicBosses.Common.Effects;
using AcidicBosses.Common.RenderManagers;
using AcidicBosses.Core.StateManagement;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Content.Bosses.EoW;

public class EoWHead : AcidicNPCOverride
{
    public enum BodyInstructions
    {
        Nothing,
        SpitSlow,
        SpitFast
    }
    
    // Set this to the boss to override
    protected override int OverriddenNpc => NPCID.EaterofWorldsHead;
    
    private EoWBossBar BossBar => (EoWBossBar) Npc.BossBar;

    public override void SetDefaults(NPC entity)
    {
        entity.lifeMax = 10000;
        entity.life = 10000;

        entity.BossBar = ModContent.GetInstance<EoWBossBar>();
        entity.boss = true;
    }
    
    public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
    {
        scale = 1.5f;
        return null;
    }

    #region AI

    public BodyInstructions BodyInstruction
    {
        get => (BodyInstructions) Npc.ai[2];
        set => Npc.ai[2] = (float) value;
    }

    private PhaseTracker phaseTracker;

    private bool isFleeing = false;

    public override void OnFirstFrame(NPC npc)
    {
        phaseTracker = new PhaseTracker([
            PhaseSummon1,
            PhaseChill1,
            PhaseAggressive1,
            PhaseSummon2,
            PhaseChill2,
            PhaseAggressive2,
            PhaseSummon3,
            PhaseChill3,
            PhaseAggressive3
        ]);
        
        WormUtils.HeadSpawnSegments(npc, 50, NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail);
        NetSync(npc);
    }

    public override bool AcidAI(NPC npc)
    {
        CommonEowAI(Npc);
        
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

        if (isFleeing) FleeAI();
        else phaseTracker.RunPhaseAI();

        return false;
    }

    public static void CommonEowAI(NPC npc)
    {
        // Fade in the stupid worm
        npc.Opacity = Math.Max(npc.Opacity + 0.05f, 1f);
    }

    private void FleeAI()
    {
        // Put Flee Behavior here
    }
    
    #endregion

    #region Phase AIs

    private PhaseState PhaseSummon1 => new(Phase_Summon1, AttackManager.Reset);
    
    void Phase_Summon1()
    {
        // Spawn in servants and stop taking damage
        AttackManager.CountUp = true;
        WormUtils.HeadDigAI(Npc, 10, 0.05f, null);

        if (AttackManager.AiTimer == 0)
        {
            Npc.dontTakeDamage = true;
            BossBar.MaxWorms = 5;
        }

        // Spawn 5 servants
        if (AttackManager.AiTimer % 30 == 0)
        {
            NewServant(5);
        }
        
        if (AttackManager.AiTimer >= 30 * 4)
        {
            AttackManager.Reset();
            phaseTracker.NextPhase();
        }
    }

    private PhaseState PhaseChill1 => new(Phase_Chill1);

    void Phase_Chill1()
    {
        // Very simple AI where it just digs towards the player and waits until all of the servants are dead
        if (BossBar.CurrentWorms <= 0)
        {
            AttackManager.AiTimer = 300;
            phaseTracker.NextPhase();
            Npc.dontTakeDamage = false;
            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.ForceRoar, Npc.Center);
            }
        }
        
        WormUtils.HeadDigAI(Npc, 15, 0.075f, null);
    }

    private PhaseState PhaseAggressive1 => new(Phase_Aggressive1);

    void Phase_Aggressive1()
    {
        if (Npc.GetLifePercent() <= 0.75)
        {
            phaseTracker.NextPhase();
            AttackManager.Reset();
        }
        
        WormUtils.HeadDigAI(Npc, 20, 0.1f, null);
        
        if (AttackManager.AiTimer > 0) return;

        if (Attack_Spit()) AttackManager.AiTimer = 300;
    }

    private PhaseState PhaseSummon2 => new(Phase_Summon2, AttackManager.Reset);
    
    void Phase_Summon2()
    {
        // Spawn in servants and stop taking damage
        AttackManager.CountUp = true;
        WormUtils.HeadDigAI(Npc, 10, 0.05f, null);

        if (AttackManager.AiTimer == 0)
        {
            Npc.dontTakeDamage = true;
            BossBar.MaxWorms = 7;
        }

        // Spawn 7 servants
        if (AttackManager.AiTimer % 30 == 0)
        {
            NewServant(5);
        }
        
        if (AttackManager.AiTimer >= 30 * 6)
        {
            AttackManager.Reset();
            phaseTracker.NextPhase();
        }
    }

    private PhaseState PhaseChill2 => new(Phase_Chill2);
    
    void Phase_Chill2()
    {
        // Very simple AI where it just digs towards the player and waits until all of the servants are dead
        if (BossBar.CurrentWorms <= 0)
        {
            AttackManager.AiTimer = 120;
            phaseTracker.NextPhase();
            Npc.dontTakeDamage = false;
            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.ForceRoar, Npc.Center);
            }
        }
        
        WormUtils.HeadDigAI(Npc, 17, 0.075f, null);
    }

    private PhaseState PhaseAggressive2 => new(Phase_Aggressive2);
    
    void Phase_Aggressive2()
    {
        BodyInstruction = BodyInstructions.SpitSlow;
        if (Npc.GetLifePercent() <= 0.4)
        {
            phaseTracker.NextPhase();
            AttackManager.Reset();
        }
        
        WormUtils.HeadDigAI(Npc, 20, 0.1f, null);
        
        if (AttackManager.AiTimer > 0) return;

        if (Attack_Spit()) AttackManager.AiTimer = 120;
    }
    
    private PhaseState PhaseSummon3 => new(Phase_Summon3, AttackManager.Reset);
    
    void Phase_Summon3()
    {
        BodyInstruction = BodyInstructions.Nothing;
        
        // Spawn in servants and stop taking damage
        AttackManager.CountUp = true;
        WormUtils.HeadDigAI(Npc, 10, 0.05f, null);

        if (AttackManager.AiTimer == 0)
        {
            Npc.dontTakeDamage = true;
            BossBar.MaxWorms = 15;
        }

        // Spawn 15 servants
        if (AttackManager.AiTimer % 30 == 0)
        {
            NewServant(3);
            NewServant(4);
            NewServant(5);
        }
        
        if (AttackManager.AiTimer >= 30 * 4)
        {
            AttackManager.Reset();
            phaseTracker.NextPhase();
        }
    }
    
    private PhaseState PhaseChill3 => new(Phase_Chill3);
    
    void Phase_Chill3()
    {
        BodyInstruction = BodyInstructions.SpitSlow;
        
        // Very simple AI where it just digs towards the player and waits until all of the servants are dead
        if (BossBar.CurrentWorms <= 0)
        {
            AttackManager.AiTimer = 60;
            phaseTracker.NextPhase();
            Npc.dontTakeDamage = false;
            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.ForceRoar, Npc.Center);
            }
        }
        
        WormUtils.HeadDigAI(Npc, 20, 0.1f, null);
    }
    
    private PhaseState PhaseAggressive3 => new(Phase_Aggressive3, EnterPhaseAggressive3);

    void EnterPhaseAggressive3()
    {
        AttackManager.SetAttackPattern([
            new AttackState(Attack_Spit, 60),
            new AttackState(Attack_Spit, 60),
            new AttackState(Attack_Spit, 60),
            new AttackState(Attack_Summon, 120)
        ]);
    }
    
    void Phase_Aggressive3()
    {
        BossBar.MaxWorms = 0;
        BodyInstruction = BodyInstructions.SpitFast;
        WormUtils.HeadDigAI(Npc, 30, 0.15f, null);
        
        if (AttackManager.AiTimer > 0) return;

        AttackManager.RunAttackPattern();
    }

    #endregion

    #region Attack Behaviors

    private bool Attack_Spit()
    {
        // Keep trying until above ground
        if (WormUtils.CheckCollision(Npc, false))
        {
            return false;
        }
        
        NewSpit(Npc.Center);
        return true;
    }
    
    private bool Attack_Summon()
    {
        NewServant(3);
        NewServant(3);
        NewServant(3);
        return true;
    }
    
    private NPC NewSpit(Vector2 position)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return null;
        return NPC.NewNPCDirect(Npc.GetSource_FromAI(), position, NPCID.VileSpitEaterOfWorlds, Npc.whoAmI);
    }

    private NPC NewServant(int length)
    {
        SoundEngine.PlaySound(SoundID.NPCDeath13, Npc.Center);

        if (Main.netMode == NetmodeID.MultiplayerClient) return null;
        var startVel = Main.rand.NextVector2Unit();
        const float speed = 5f;
        
        var npc = NPC.NewNPCDirect(Npc.GetSource_FromAI(), Npc.Center, ModContent.NPCType<EoWServant>(), Npc.whoAmI, ai2: length);
        npc.velocity = startVel * speed;

        return npc;
    }

    #endregion

    #region Drawing

    public override bool AcidicDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        CommonPreDraw(npc, spriteBatch, screenPos, lightColor);

        return false;
    }

    /// <summary>
    /// Draw code shared between all EoW segments
    /// </summary>
    public static void CommonPreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        var drawPos = npc.Center - Main.screenPosition;
        var texAsset = TextureAssets.Npc[npc.type];
        var texture = texAsset.Value;
        var origin = npc.frame.Size() * 0.5f;
        lightColor *= npc.Opacity;
        
        // Batch shade because there are a lot of these
        BatchShadingManager.DrawNpc(EffectsRegistry.Shield, sb =>
        {
            if (npc.dontTakeDamage) EffectsManager.ShieldApply(texAsset, lightColor, npc.alpha);
        
            sb.Draw(
                texture, drawPos,
                npc.frame, lightColor,
                npc.rotation, origin, npc.scale,
                SpriteEffects.None, 0f);
        });
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

    public override void OnKill(NPC npc)
    {
        
    }

    public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
    {
        // Drop Tissue Samples directly if the player isn't getting a treasure bag
        var notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());
        notExpertRule.OnSuccess(ItemDropRule.Common(ItemID.ShadowScale, 1, 75, 125));

        npcLoot.Add(notExpertRule);
    }
}