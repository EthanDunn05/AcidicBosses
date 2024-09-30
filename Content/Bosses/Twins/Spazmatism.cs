using System;
using System.IO;
using AcidicBosses.Common.Configs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Content.Bosses.Twins;

public class Spazmatism : Twin
{
    protected override int OverriddenNpc => NPCID.Spazmatism;

    #region AI
    
    private int Controller
    {
        get => (int) Npc.ai[0];
        set => Npc.ai[0] = value;
    }
    
    public override void OnFirstFrame(NPC npc)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            Controller = TwinsController.Link(npc);
            Main.npc[Controller].ai[0] = Npc.whoAmI;
        }
    }

    public override bool AcidAI(NPC npc)
    {
        return false;
    }
    
    #endregion
    
    
}