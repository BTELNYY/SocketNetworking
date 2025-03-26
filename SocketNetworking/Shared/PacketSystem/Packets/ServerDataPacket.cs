using System.Collections.Generic;
using SocketNetworking.Shared.Messages;
using SocketNetworking.Shared.PacketSystem.TypeWrappers;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem.Packets
{
    public sealed class ServerDataPacket : Packet
    {
        public sealed override PacketType Type => PacketType.ServerData;

        public int YourClientID { get; set; } = 0;

        public ProtocolConfiguration Configuration { get; set; } = new ProtocolConfiguration();

        public bool UpgradeToSSL { get; set; } = false;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(YourClientID);
            writer.WritePacketSerialized<ProtocolConfiguration>(Configuration);
            writer.WriteBool(UpgradeToSSL);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            YourClientID = reader.ReadInt();
            Configuration = reader.ReadPacketSerialized<ProtocolConfiguration>();
            UpgradeToSSL = reader.ReadBool();
            return reader;
        }
    }
}
