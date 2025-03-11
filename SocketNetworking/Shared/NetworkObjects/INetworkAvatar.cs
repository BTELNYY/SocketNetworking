using SocketNetworking.Shared.PacketSystem.Packets;

namespace SocketNetworking.Shared.NetworkObjects
{
    public interface INetworkAvatar : INetworkObject
    {
        string PublicKey { get; }

        void SendPrivate(byte[] data);

        void ReceivePrivate(ClientToClientPacket data);
    }
}
