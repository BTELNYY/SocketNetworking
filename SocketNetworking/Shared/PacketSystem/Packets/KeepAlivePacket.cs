using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem.Packets
{
    public sealed class KeepAlivePacket : Packet
    {
        public override PacketType Type => PacketType.KeepAlive;

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            return reader;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            return writer;
        }
    }
}
