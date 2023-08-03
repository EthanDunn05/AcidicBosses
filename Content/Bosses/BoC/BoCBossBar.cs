using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.BoC;

public class BoCBossBar : ModBossBar
{
    public float MaxCreepers { get; set; } = 0;
    
    private int bossHeadIndex = -1;

    public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame)
    {
        if (bossHeadIndex != -1)
        {
            return TextureAssets.NpcHeadBoss[bossHeadIndex];
        }

        return null;
    }

    public override bool? ModifyInfo(ref BigProgressBarInfo info, ref float life, ref float lifeMax, ref float shield, ref float shieldMax)
    {
        var npc = Main.npc[info.npcIndexToAimAt];
        if(!npc.active) return false;
        
        bossHeadIndex = npc.GetBossHeadTextureIndex();
        
        life = npc.life;
        lifeMax = npc.lifeMax;
        
        // Show creepers as the shield
        shieldMax = MaxCreepers;
        if(shieldMax != 0)
            shield = Main.npc.Count(n => n.active && n.type == NPCID.Creeper);

        return true;
    }
}