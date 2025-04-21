using AcidicBosses.Helpers;
using Terraria;
using Terraria.Audio;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public partial class QueenSlime
{
    private bool Attack_SummonSlimes(int slimes)
    {
        AttackManager.CountUp = true;
        const int interval = 10;

        if (AttackManager.AiTimer % interval == 0)
        {
            SoundEngine.PlaySound(ShortBubbleSound, Npc.Center);
            NewHeavenlySlime(Npc.Center);
        }

        if (AttackManager.AiTimer >= interval * (slimes - 1))
        {
            AttackManager.CountUp = false;
            return true;
        }


        return false;
    }
}