using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem.Packets
{
    public sealed class SSLUpgradePacket : Packet
    {
        public override PacketType Type => PacketType.SSLUpgrade;

        public bool Result { get; set; } = false;

        public bool Continue { get; set; } = false;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteBool(Result);
            writer.WriteBool(Continue);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Result = reader.ReadBool();
            Continue = reader.ReadBool();
            return reader;
        }
    }
}
