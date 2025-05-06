using SocketNetworking.Client;
using SocketNetworking.Shared.PacketSystem.Packets;

namespace SocketNetworking.Shared.NetworkObjects
{
    /// <summary>
    /// The <see cref="INetworkAvatar"/> interface represents the requirements for the <see cref="NetworkAvatarBase"/>.
    /// </summary>
    public interface INetworkAvatar : INetworkObject
    {
        /// <summary>
        /// The public key of the client used to sent private data. Since the server shares this key, it is not truly End-To-End encryption. Assuming the server is trustworthy, this is secure. The public key is shared with the server by default when using encryption.
        /// </summary>
        string PublicKey { get; }

        /// <summary>
        /// Sends private data to the <see cref="NetworkClient"/> which owns this object using the RSA <see cref="PublicKey"/>.
        /// </summary>
        /// <param name="data"></param>
        void SendPrivate(byte[] data);

        /// <summary>
        /// Called locally when the <see cref="NetworkClient"/> receives information which is secured with its <see cref="PublicKey"/>.
        /// </summary>
        /// <param name="data"></param>
        void ReceivePrivate(ClientToClientPacket data);
    }
}
