using Terraria;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public partial class QueenSlime : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.QueenSlimeBoss;
    protected override bool BossEnabled => true;

    public override bool AcidAI(NPC npc)
    {
        return false;
    }
    
    // Stolen from Infernum
    // https://github.com/InfernumTeam/InfernumMode/blob/master/Content/BehaviorOverrides/BossAIs/QueenSlime/QueenSlimeBehaviorOverride.cs#L1223
    public static bool OnSolidGround(NPC npc)
    {
        var solidGround = false;
        for (var i = -8; i < 8; i++)
        {
            var ground = Framing.GetTileSafely((int)(npc.Bottom.X / 16f) + i, (int)(npc.Bottom.Y / 16f) + 1);
            var notAFuckingTree = ground.TileType is not TileID.Trees and not TileID.PineTree and not TileID.PalmTree;
            if (ground.HasUnactuatedTile && notAFuckingTree && (Main.tileSolid[ground.TileType] || Main.tileSolidTop[ground.TileType]))
            {
                solidGround = true;
                break;
            }
        }
        return solidGround;
    }
}