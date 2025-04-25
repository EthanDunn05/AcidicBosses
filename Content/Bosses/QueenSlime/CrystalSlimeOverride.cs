using Terraria.ID;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public class CrystalSlimeOverride : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.QueenSlimeMinionBlue;
    protected override bool BossEnabled => true;
}