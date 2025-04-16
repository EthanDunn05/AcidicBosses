using AcidicBosses.Common.Textures;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.WoF;

public class EasyPixieNPC : AcidicNPC
{
    public override string Texture => TextureRegistry.TerrariaNPC(NPCID.Pixie);

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = 4;
        
        // Nobody can know this isn't a real pixie
        var bestiary = new NPCID.Sets.NPCBestiaryDrawModifiers()
        {
            Hide = true
        };
        
        NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, bestiary);
    }

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.Pixie);

        // Set stuff to match pre-hardmode world evil mobs flyers
        NPC.damage = 22;
        NPC.defense = 8;
        NPC.lifeMax = 40;
        NPC.life = 40;
        NPC.value = 90;
    }

    public override void FindFrame(int frameHeight)
    {
        if (NPC.velocity.X > 0f)
        {
            NPC.spriteDirection = 1;
        }
        else
        {
            NPC.spriteDirection = -1;
        }
        NPC.rotation = NPC.velocity.X * 0.1f;
        NPC.frameCounter += 1.0;
        if (NPC.frameCounter >= 4.0)
        {
            NPC.frame.Y += frameHeight;
            NPC.frameCounter = 0.0;
        }
        if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[Type])
        {
            NPC.frame.Y = 0;
        }
    }
}