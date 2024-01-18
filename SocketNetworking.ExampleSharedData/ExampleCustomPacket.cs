using SocketNetworking.Attributes;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;

namespace SocketNetworking.ExampleSharedData
{
    [PacketDefinition]
    public class ExampleCustomPacket : CustomPacket
    {
        public override int CustomPacketID => 0;

        public string Data { get; set; } = "DataTest!";

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Data = reader.ReadString();
            return reader;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteString(Data);
            return writer;
        }
    }
}
