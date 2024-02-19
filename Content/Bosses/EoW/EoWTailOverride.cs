using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.EoW;

public class EoWTailOverride : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.EaterofWorldsTail;

    public NPC FollowingNPC => Main.npc[(int) Npc.ai[1]];

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