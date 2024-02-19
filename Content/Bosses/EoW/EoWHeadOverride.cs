using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Content.Bosses.EoW;

public class EoWHeadOverride : AcidicNPCOverride
{
    // Set this to the boss to override
    protected override int OverriddenNpc => NPCID.EaterofWorldsHead;

    public override void SetDefaults(NPC entity)
    {
        entity.lifeMax = 10000;
        entity.life = 10000;
    }

    #region Phases

    private enum PhaseState
    {
        Test
    }

    private PhaseState CurrentPhase
    {
        get => (PhaseState) Npc.ai[1];
        set => Npc.ai[1] = (float) value;
    }

    private Action CurrentAi => CurrentPhase switch
    {
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
        CurrentPhase = PhaseState.Test;
        AiTimer = 0;
        
        WormUtils.HeadSpawnSegments(npc, 64, NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail);
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

    void Phase_Test()
    {
        WormUtils.HeadDigAI(Npc, 10, 0.05f, null);
    }

    #endregion

    #region Attack Behaviors

    // Put attack methods here
    
    

    #endregion

    #endregion

    #region Drawing

    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        CommonEowPreDraw(npc, spriteBatch, screenPos, lightColor);

        return false;
    }

    /// <summary>
    /// Draw code shared between all EoW segments
    /// </summary>
    public static void CommonEowPreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        var drawPos = npc.Center - Main.screenPosition;
        var texture = TextureAssets.Npc[npc.type].Value;
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