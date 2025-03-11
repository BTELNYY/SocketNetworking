using SocketNetworking.Shared.Messages;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem.Packets
{
    public sealed class ClientDataPacket : Packet
    {
        public sealed override PacketType Type => PacketType.ClientData;

        public ProtocolConfiguration Configuration { get; set; } = new ProtocolConfiguration();

        public ClientDataPacket()
        {

        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Configuration = reader.ReadPacketSerialized<ProtocolConfiguration>();
            return reader;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WritePacketSerialized<ProtocolConfiguration>(Configuration);
            return writer;
        }
    }
}
