using SocketNetworking.Shared;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem.Packets
{
    public sealed class ConnectionUpdatePacket : Packet
    {
        public sealed override PacketType Type => PacketType.ConnectionStateUpdate;

        public ConnectionState State  = ConnectionState.Disconnected;

        public string Reason  = "";

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Reason = reader.ReadString();
            State = (ConnectionState)reader.ReadInt();
            return reader;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteString(Reason);
            writer.WriteInt((int)State);
            return writer;
        }
    }
}
