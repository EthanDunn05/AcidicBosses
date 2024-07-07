using AcidicBosses.Common;
using AcidicBosses.Common.Effects;
using AcidicBosses.Common.RenderManagers;
using AcidicBosses.Common.Textures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.EoW;

public class EoWServant : AcidicNPC
{
    public override string Texture => TextureRegistry.TerrariaNPC(NPCID.EaterofWorldsHead);

    private int Segments
    {
        get => (int) NPC.ai[2];
        set => NPC.ai[2] = value;
    }

    private int BossId
    {
        get => (int) NPC.ai[3];
        set => NPC.ai[3] = value;
    }

    private NPC Boss => Main.npc[BossId];

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.EaterofWorldsHead);

        NPC.aiStyle = -1;
        NPC.lifeMax = 128;

        NPC.BossBar = ModContent.GetInstance<NoBossBar>();
        NPC.boss = false;
    }
    
    public override void SetStaticDefaults()
    {
        
    }

    public override void OnFirstFrame()
    {
        NPC.behindTiles = false;
        WormUtils.HeadSpawnSegments(NPC, Segments, Type, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail);
        NetSync();
    }

    public override void AcidAI()
    {
        // Sync damage to the boss because it gets scaled twice somehow
        NPC.damage = Boss.damage;
        
        if (!Boss.active)
        {
            NPC.active = false;
        }
        
        EoWHead.CommonEowAI(NPC);
        WormUtils.HeadDigAI(NPC, 15, 0.15f, null);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        CommonPreDraw(NPC, spriteBatch, screenPos, lightColor);

        return false;
    }

    public static void CommonPreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        var drawPos = npc.Center - Main.screenPosition;
        var texAsset = TextureAssets.Npc[npc.type];
        var texture = texAsset.Value;
        var origin = npc.frame.Size() * 0.5f;
        lightColor *= npc.Opacity;

        // Outline when underground
        BatchShadingManager.DrawNpc(npc, EffectsRegistry.UndergroundOutline, sb =>
        {
            EffectsManager.UndergroundOutlineApply(texAsset, Color.Violet, lightColor);

            sb.Draw(
                texture, drawPos,
                npc.frame, lightColor,
                npc.rotation, origin, npc.scale,
                SpriteEffects.None, 0f);
        });
    }
}