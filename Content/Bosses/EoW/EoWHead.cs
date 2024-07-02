using System;
using System.IO;
using AcidicBosses.Core.StateManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Content.Bosses.EoW;

public class EoWHead : AcidicNPCOverride
{
    // Set this to the boss to override
    protected override int OverriddenNpc => NPCID.EaterofWorldsHead;
    
    private EoWBossBar BossBar => (EoWBossBar) Npc.BossBar;

    public override void SetDefaults(NPC entity)
    {
        entity.lifeMax = 10000;
        entity.life = 10000;

        entity.BossBar = ModContent.GetInstance<EoWBossBar>();
    }
    
    public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
    {
        scale = 1.5f;
        return null;
    }

    #region AI

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
        
        WormUtils.HeadSpawnSegments(npc, 72, NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail);
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

    private PhaseState PhaseSummon1 => new(Phase_Summon1);
    
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
            NewServant();
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

    private PhaseState PhaseSummon2 => new(Phase_Summon2);
    
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
            NewServant();
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
        if (Npc.GetLifePercent() <= 0.4)
        {
            phaseTracker.NextPhase();
            AttackManager.Reset();
        }
        
        WormUtils.HeadDigAI(Npc, 20, 0.1f, null);
        
        if (AttackManager.AiTimer > 0) return;

        if (Attack_Spit()) AttackManager.AiTimer = 120;
    }
    
    private PhaseState PhaseSummon3 => new(Phase_Summon3);
    
    void Phase_Summon3()
    {
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
            NewServant();
            NewServant();
            NewServant();
        }
        
        if (AttackManager.AiTimer >= 30 * 4)
        {
            AttackManager.Reset();
            phaseTracker.NextPhase();
        }
    }
    
    private PhaseState PhaseChill3 => new(Phase_Chill2);
    
    void Phase_Chill3()
    {
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
    
    private PhaseState PhaseAggressive3 => new(Phase_Aggressive3);
    
    void Phase_Aggressive3()
    {
        WormUtils.HeadDigAI(Npc, 30, 0.15f, null);
        
        if (AttackManager.AiTimer > 0) return;

        if (Attack_Spit()) AttackManager.AiTimer = 60;
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
    
    private NPC NewSpit(Vector2 position)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return null;
        return NPC.NewNPCDirect(Npc.GetSource_FromAI(), position, NPCID.VileSpitEaterOfWorlds, Npc.whoAmI);
    }

    private NPC NewServant()
    {
        SoundEngine.PlaySound(SoundID.NPCDeath13, Npc.Center);

        if (Main.netMode == NetmodeID.MultiplayerClient) return null;
        return NPC.NewNPCDirect(Npc.GetSource_FromAI(), Npc.Center, ModContent.NPCType<EoWServant>(), Npc.whoAmI);
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

        spriteBatch.Draw(
            texture, drawPos,
            npc.frame, lightColor,
            npc.rotation, origin, npc.scale,
            SpriteEffects.None, 0f);
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
}