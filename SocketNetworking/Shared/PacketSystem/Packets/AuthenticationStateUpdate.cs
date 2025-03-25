using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem.Packets
{
    public sealed class AuthenticationStateUpdate : Packet
    {
        public override PacketType Type => PacketType.AuthenticationStateUpdate;

        public bool State { get; set; } = false;

        public string Message { get; set; } = "";

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteBool(State);
            writer.WriteString(Message);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            State = reader.ReadBool();
            Message = reader.ReadString();
            return reader;
        }
    }
}
