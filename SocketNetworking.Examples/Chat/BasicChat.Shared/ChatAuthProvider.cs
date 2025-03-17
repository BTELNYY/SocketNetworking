using SocketNetworking.Shared;
using SocketNetworking.Shared.Authentication;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;

namespace BasicChat.Shared
{
    public class ChatAuthProvider : AuthenticationProvider
    {
        public ChatAuthProvider(ChatClient client) : base(client)
        {
        }

        public override (AuthenticationResult, byte[]) Authenticate(NetworkHandle handle, AuthenticationPacket packet)
        {
            ChatClient client = (ChatClient)Client;
            ChatAuthData data;
            ByteReader reader = new ByteReader(packet.ExtraAuthenticationData);
            data = reader.ReadPacketSerialized<ChatAuthData>();
            if (string.IsNullOrWhiteSpace(data.Name))
            {
                data.Name = handle.Client.ConnectedHostname;
            }
            client.RequestedName = data.Name;
            return (new AuthenticationResult()
            {
                Approved = true,
                Message = "",
            }, new byte[] { });
        }

        public override AuthenticationPacket BeginAuthentication()
        {
            AuthenticationPacket packet = new AuthenticationPacket();
            ChatAuthData data = new ChatAuthData();
            data.Name = ((ChatClient)Client).RequestedName;
            ByteWriter writer = new ByteWriter();
            writer.WritePacketSerialized<ChatAuthData>(data);
            packet.ExtraAuthenticationData = writer.Data;
            return packet;
        }

        public override void HandleAuthenticationResult(NetworkHandle handle, AuthenticationPacket packet)
        {

        }
    }

    public struct ChatAuthData : IPacketSerializable
    {
        public string Name;

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Name = reader.ReadString();
            return reader;
        }

        public int GetLength()
        {
            return Serialize().Length;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteString(Name);
            return writer;
        }
    }
}
