using AcidicBosses.Common;
using AcidicBosses.Common.Effects;
using AcidicBosses.Common.Textures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.EoW;

public class ServantWorm : AcidicNPC
{
    public override string Texture => TextureRegistry.TerrariaNPC(NPCID.EaterofWorldsHead);

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.EaterofWorldsHead);

        NPC.aiStyle = -1;
        NPC.lifeMax = 128;
    }

    public override void SetStaticDefaults()
    {
        
    }

    public override void OnFirstFrame()
    {
        WormUtils.HeadSpawnSegments(NPC, 3, Type, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail);
    }

    public override void AcidAI()
    {
        EoWHeadOverride.CommonEowAI(NPC);
        WormUtils.HeadDigAI(NPC, 10, 0.1f, null);
    }
    
    

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        var drawPos = NPC.Center - Main.screenPosition;
        var texAsset = TextureAssets.Npc[NPCID.EaterofWorldsHead];
        var texture = texAsset.Value;
        var origin = NPC.frame.Size() * 0.5f;
        lightColor *= NPC.Opacity;

        // Outline when underground
        spriteBatch.StartShader();
        EffectsManager.UndergroundOutline(texAsset, Color.Violet, lightColor);

        spriteBatch.Draw(
            texture, drawPos,
            NPC.frame, lightColor,
            NPC.rotation, origin, NPC.scale,
            SpriteEffects.None, 0f);

        spriteBatch.EndShader();

        return false;
    }
}