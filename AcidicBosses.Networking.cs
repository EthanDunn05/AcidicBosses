using System.IO;
using AcidicBosses.Core.Systems;

namespace AcidicBosses;

partial class AcidicBosses
{
    internal enum MessageType : byte
    {
        SyncDifficulty
    }

    //TODO: This is some nasty hardcoding, but I don't need more than it for right now.
    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        MessageType type = (MessageType) reader.ReadByte();

        switch (type)
        {
            case MessageType.SyncDifficulty:
                DifficultySystem.AcidicActive = reader.ReadBoolean();
                break;
        }
    }
}