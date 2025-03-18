using SocketNetworking.Shared.Authentication;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem.Packets
{
    public sealed class AuthenticationPacket : Packet
    {
        public override PacketType Type => PacketType.Authentication;

        public byte[] ExtraAuthenticationData { get; set; }

        public bool IsResult { get; set; }

        public AuthenticationResult Result { get; set; } = new AuthenticationResult();

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            IsResult = reader.ReadBool();
            Result = reader.ReadPacketSerialized<AuthenticationResult>();
            ExtraAuthenticationData = reader.ReadByteArray();
            return reader;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteBool(IsResult);
            writer.WritePacketSerialized<AuthenticationResult>(Result);
            writer.WriteByteArray(ExtraAuthenticationData);
            return writer;
        }
    }
}
