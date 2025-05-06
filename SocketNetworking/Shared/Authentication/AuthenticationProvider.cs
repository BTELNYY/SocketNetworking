using SocketNetworking.Client;
using SocketNetworking.Server;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.Authentication
{
    /// <summary>
    /// The <see cref="AuthenticationProvider"/> is an abstract class to allow for authentication of <see cref="NetworkClient"/>s. See <see cref="NetworkClient.Authenticated"/> and <see cref="NetworkClient.AuthenticationProvider"/> as well as <see cref="NetworkServer.Authenticator"/>.
    /// </summary>
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

        /// <summary>
        /// Called on the receiver of the <see cref="BeginAuthentication"/> request. Must return an <see cref="AuthenticationResult"/> as well as extra data, if needed. This method can also be used to authenticate a client with a server side request.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public abstract (AuthenticationResult, byte[]) Authenticate(NetworkHandle handle, AuthenticationPacket packet);

        /// <summary>
        /// Called to handle the <see cref="AuthenticationResult"/> of the <see cref="Authenticate(NetworkHandle, AuthenticationPacket)"/> method. Also contains extra data, if needed.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="packet"></param>
        public abstract void HandleAuthenticationResult(NetworkHandle handle, AuthenticationPacket packet);

        /// <summary>
        /// Called when the <see cref="NetworkClient"/> on the remote or local begins the authentication sequence.
        /// </summary>
        /// <returns></returns>
        public abstract AuthenticationPacket BeginAuthentication();
    }

    public class AuthenticationResult : IPacketSerializable
    {
        public bool Approved
        {
            get
            {
                return State == AuthenticationResultState.Approved;
            }
            set
            {
                if (value)
                {
                    State = AuthenticationResultState.Approved;
                }
            }
        }

        public AuthenticationResultState State = AuthenticationResultState.NoResult;

        public string Message = "";

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            State = (AuthenticationResultState)reader.ReadByte();
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
            writer.WriteByte((byte)State);
            writer.WriteString(Message);
            return writer;
        }
    }

    public enum AuthenticationResultState : byte
    {
        NoResult,
        Approved,
        Denied,
    }
}
