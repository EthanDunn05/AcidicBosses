using System.IO;
using Terraria;

namespace AcidicBosses.Content.Bosses.Twins.Syncing;

public class TwinsSyncManager(int retId, int spazId)
{
    private int retId = retId;
    private int spazId = spazId;

    public NPC Retinazer => Main.npc[retId];
    public NPC Spazmatism => Main.npc[spazId];

    public SyncState RetSyncState => (SyncState) Retinazer.ai[0];
    public SyncState SpazSyncState => (SyncState) Spazmatism.ai[0];

    public void Send(BinaryWriter writer)
    {
        writer.Write7BitEncodedInt(retId);
        writer.Write7BitEncodedInt(spazId);
    }

    public void Recieve(BinaryReader reader)
    {
        retId = reader.Read7BitEncodedInt();
        spazId = reader.Read7BitEncodedInt();
    }
}