using Terraria;
using Terraria.ID;

namespace AcidicBosses.Helpers;

public static class AcidUtils
{
    public static bool IsServer()
    {
        return Main.netMode != NetmodeID.MultiplayerClient;
    }

    public static bool IsClient()
    {
        return Main.netMode != NetmodeID.Server;
    }
}