using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

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

    #region Phases

    private enum PhaseState
    {
        Intro,
        Chill,
        Aggressive,
        Test
    }

    private PhaseState CurrentPhase
    {
        get => (PhaseState) Npc.ai[1];
        set => Npc.ai[1] = (float) value;
    }

    private Action CurrentAi => CurrentPhase switch
    {
        PhaseState.Intro => Phase_Intro,
        PhaseState.Chill => Phase_Chill,
        PhaseState.Test => Phase_Test,
        _ => throw new UsageException(
            $"The PhaseState {CurrentPhase} and does not have an ai")
    };

    #endregion

    #region Attacks

    private enum Attack
    {
    }

    private Attack[] CurrentAttackPattern => CurrentPhase switch
    {
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
        get => (int) Npc.ai[3];
        set => Npc.ai[3] = value;
    }

    public override void OnFirstFrame(NPC npc)
    {
        CurrentPhase = PhaseState.Intro;
        AiTimer = 0;
        
        WormUtils.HeadSpawnSegments(npc, 72, NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail);
    }

    public override bool AcidAI(NPC npc)
    {
        if (AiTimer > 0 && !countUpTimer)
            AiTimer--;
        
        CommonEowAI(Npc);

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

        if (isFleeing) FleeAI();
        else CurrentAi.Invoke();

        if (countUpTimer)
            AiTimer++;

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

    #region Phase AIs

    void Phase_Intro()
    {
        // Spawn in servants and stop taking damage
        countUpTimer = true;
        WormUtils.HeadDigAI(Npc, 10, 0.05f, null);

        if (AiTimer == 0)
        {
            Npc.dontTakeDamage = true;
            BossBar.MaxWorms = 5;
        }

        // Spawn 5 servants
        if (AiTimer % 30 == 0)
        {
            NewServant();
        }
        
        if (AiTimer >= 120)
        {
            AiTimer = 0;
            countUpTimer = false;
            CurrentPhase = PhaseState.Chill;
        }
    }

    void Phase_Chill()
    {
        // Very simple AI where it just digs towards the player and waits until all of the servants are dead
        if (BossBar.CurrentWorms <= 0)
        {
            AiTimer = 0;
            CurrentPhase = PhaseState.Aggressive;
            Npc.dontTakeDamage = false;
        }
        
        WormUtils.HeadDigAI(Npc, 10, 0.05f, null);
    }

    void Phase_Aggressive()
    {
        WormUtils.HeadDigAI(Npc, 10, 0.05f, null);
    }
    
    void Phase_Test()
    {
        WormUtils.HeadDigAI(Npc, 10, 0.05f, null);
    }

    #endregion

    #region Attack Behaviors

    

    private NPC NewServant()
    {
        return NPC.NewNPCDirect(Npc.GetSource_FromAI(), Npc.Center, ModContent.NPCType<EoWServant>(), Npc.whoAmI);
    }

    #endregion

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
}