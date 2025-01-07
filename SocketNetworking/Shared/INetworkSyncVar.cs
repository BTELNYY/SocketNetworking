using SocketNetworking.PacketSystem;

namespace SocketNetworking.Shared
{
    public interface INetworkSyncVar
    {
        string Name { get; set; }
        INetworkObject OwnerObject { get; }
        NetworkDirection SyncOwner { get; }
        object ValueRaw { get; set; }

        object Clone();
        bool Equals(object other);
    }
}