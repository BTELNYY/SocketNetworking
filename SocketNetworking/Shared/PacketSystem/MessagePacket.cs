using SocketNetworking.Shared.Messages;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem
{
    public sealed class MessagePacket : Packet
    {
        public override PacketType Type => PacketType.Message;

        public INetworkMessage Message { get; set; }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteObject(Message);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Message = reader.ReadObject<INetworkMessage>();
            return reader;
        }
    }
}
