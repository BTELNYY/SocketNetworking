using SocketNetworking.Shared;
using SocketNetworking.Shared.Authentication;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem.Packets
{
    public sealed class AuthenticationPacket : Packet
    {
        public override PacketType Type => PacketType.Authentication;

        public byte[] AuthData { get; set; }

        public bool IsResult { get; set; }

        public AuthenticationResult Result { get; set; } = new AuthenticationResult();

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            AuthData = reader.ReadByteArray();
            IsResult = reader.ReadBool();
            Result = reader.ReadPacketSerialized<AuthenticationResult>();
            return reader;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteByteArray(AuthData);
            writer.WriteBool(IsResult);
            writer.WritePacketSerialized<AuthenticationResult>(Result);
            return writer;
        }
    }
}
