using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.UnityEngine.Packets
{
    public class NetworkTransformBasePacket : CustomPacket
    {
        public string UUID { get; set; }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            UUID = reader.ReadString();
            return reader;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteString(UUID);
            return writer;
        }
    }
}
