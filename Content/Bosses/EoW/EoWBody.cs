﻿using System;
using AcidicBosses.Common.Configs;
using AcidicBosses.Helpers.NpcHelpers;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.EoW;

public class EoWBody : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.EaterofWorldsBody;

    protected override bool BossEnabled => BossToggleConfig.Get().EnableEaterOfWorlds;

    public NPC HeadNPC => Main.npc[Npc.realLife];
    public NPC FollowingNPC => Main.npc[(int) Npc.ai[1]];
    public NPC FollowerNPC => Main.npc[(int) Npc.ai[0]];
    public bool FollowingBoss => HeadNPC.type == NPCID.EaterofWorldsHead;

    public EoWHead.BodyInstructions Instruction => (EoWHead.BodyInstructions) HeadNPC.ai[2];
    
    public override void SetDefaults(NPC entity)
    {
        if (!ShouldOverride()) return;
        entity.BossBar = ModContent.GetInstance<NoBossBar>();
    }
    
    public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
    {
        if (!ShouldOverride()) return null;
        return false;
    }

    public override bool AcidAI(NPC npc)
    {
        if (FollowingNPC.type == NPCID.EaterofWorldsBody)
            Npc.realLife = (int) FollowingNPC.realLife;
        else Npc.realLife = (int) Npc.ai[1];
        
        EoWHead.CommonEowAI(npc);
        
        try
        {
            WormUtils.BodyTailFollow(npc, FollowingNPC);
        }
        catch (Exception e)
        {
            Mod.Logger.ErrorFormat("Can't find following eater segment {0}, {1}, {2}", Npc.ai[0], Npc.ai[1], Main.npc[(int)Npc.ai[1]]);
            NetSync(Npc);
            return false;
        }
        
        if (!FollowingBoss) Npc.behindTiles = false;

        if (FollowingBoss)
        {
            switch (Instruction)
            {
                case EoWHead.BodyInstructions.Nothing:
                    break;
                case EoWHead.BodyInstructions.SpitSlow:
                    SpitAI(120, 30);
                    break;
                case EoWHead.BodyInstructions.SpitFast:
                    SpitAI(45, 30);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Instruction));
            }
        }

        return false;
    }

    public void SpitAI(int spitInterval, int failPenalty)
    {

        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        if (AttackManager.AiTimer <= 0)
        {
            // 1 in 10 chance to spit. This is on the server, so randomness is fine
            if (Main.rand.NextBool(10) && WormUtils.CheckCollision(Npc, false))
            {
                NewSpit(Npc.Center);
                AttackManager.AiTimer = spitInterval;
                return;
            }

            AttackManager.AiTimer = failPenalty;
        }
    }
    
    private NPC NewSpit(Vector2 position)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return null;
        return NPC.NewNPCDirect(Npc.GetSource_FromAI(), position, NPCID.VileSpitEaterOfWorlds, Npc.whoAmI);
    }

    public override bool AcidicDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        // Draw differently depending on if following the main boss or a servant
        if (FollowingBoss)
        {
            EoWHead.CommonPreDraw(npc, spriteBatch, screenPos, lightColor);
        }
        else
        {
            EoWServant.CommonPreDraw(npc, spriteBatch, screenPos, lightColor);
        }

        return false;
    }
}