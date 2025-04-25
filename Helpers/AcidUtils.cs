using Microsoft.Xna.Framework;
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
    
    // Similar to Luminance's, but includes all walkable tiles 
    public static Point FindGroundVertical(Point p)
    {
        // The tile is solid. Check up to verify that this tile is not inside of solid ground.
        if (WorldGen.ActiveAndWalkableTile(p.X, p.Y))
        {
            while (WorldGen.ActiveAndWalkableTile(p.X, p.Y - 1) && p.Y >= 1)
                p.Y--;
        }

        // The tile is not solid. Check down to verify that this tile is not above ground in the middle of the air.
        else
        {
            while (!WorldGen.ActiveAndWalkableTile(p.X, p.Y + 1) && p.Y < Main.maxTilesY)
                p.Y++;
        }

        return p;
    }
}