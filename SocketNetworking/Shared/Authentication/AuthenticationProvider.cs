using SocketNetworking.Client;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.Authentication
{
    public abstract class AuthenticationProvider
    {
        public AuthenticationProvider(NetworkClient client)
        {
            Client = client;
        }

        /// <summary>
        /// By default, <see cref="AuthenticationProvider"/>s have the <see cref="NetworkClient"/> begin the auth process.
        /// </summary>
        public virtual bool ClientInitiate => true;

        public NetworkClient Client { get; }

        public abstract (AuthenticationResult, byte[]) Authenticate(NetworkHandle handle, AuthenticationPacket packet);

        public abstract void HandleAuthResult(NetworkHandle handle, AuthenticationPacket packet);

        public abstract AuthenticationPacket BeginAuthentication();
    }

    public class AuthenticationResult : IPacketSerializable
    {
        public bool Approved = false;

        public string Message = "";

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Approved = reader.ReadBool();
            Message = reader.ReadString();
            return reader;
        }

        public int GetLength()
        {
            return Serialize().Length;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteBool(Approved);
            writer.WriteString(Message);
            return writer;
        }
    }
}
