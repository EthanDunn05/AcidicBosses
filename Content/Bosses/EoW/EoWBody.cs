﻿using System;
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

    public NPC HeadNPC => Main.npc[Npc.realLife];
    public NPC FollowingNPC => Main.npc[(int) Npc.ai[1]];
    public NPC FollowerNPC => Main.npc[(int) Npc.ai[0]];
    public bool FollowingBoss => HeadNPC.type == NPCID.EaterofWorldsHead;
    
    public override void SetDefaults(NPC entity)
    {
        entity.BossBar = ModContent.GetInstance<NoBossBar>();
    }
    
    public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
    {
        return false;
    }

    public override bool AcidAI(NPC npc)
    {
        EoWHead.CommonEowAI(npc);

        WormUtils.BodyTailFollow(npc, FollowingNPC);
        if (!FollowingBoss) Npc.behindTiles = false;

        return false;
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