using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.EoW;

public class EoWBodyOverride : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.EaterofWorldsBody;

    public NPC FollowingNPC => Main.npc[(int) Npc.ai[1]];
    public NPC FollowerNPC => Main.npc[(int) Npc.ai[0]];
    
    public override void SetDefaults(NPC entity)
    {
        entity.BossBar = ModContent.GetInstance<EoWBossBar>();
    }
    
    public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
    {
        return false;
    }

    public override bool AcidAI(NPC npc)
    {
        EoWHeadOverride.CommonEowAI(npc);

        WormUtils.BodyTailFollow(npc, FollowingNPC);

        return false;
    }

    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        EoWHeadOverride.CommonEowPreDraw(npc, spriteBatch, screenPos, lightColor);

        return false;
    }
}