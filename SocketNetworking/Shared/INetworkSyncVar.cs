using SocketNetworking.Client;
using SocketNetworking.PacketSystem;

namespace SocketNetworking.Shared
{
    public interface INetworkSyncVar
    {
        /// <summary>
        /// The name of the SyncVar. If left blank, this will be set to the name of the field it is in by the library when the object is registered.
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// The <see cref="INetworkObject"/> where this is registered.
        /// </summary>
        INetworkObject OwnerObject { get; }
        /// <summary>
        /// The direction in which the <see cref="INetworkSyncVar"/> accepts changes to its state.
        /// </summary>
        OwnershipMode SyncOwner { get; }
        /// <summary>
        /// The raw value of the object.
        /// </summary>
        object ValueRaw { get; }
        /// <summary>
        /// This value should not update the state of the object on the network, instead it should accept the name change in state.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="who"></param>
        void RawSet(object value, NetworkClient who);
        /// <summary>
        /// This method should be called to update the Network value of the SyncVar
        /// </summary>
        /// <param name="value"></param>
        /// <param name="who"></param>
        void NetworkSet(object value, NetworkClient who);
        object Clone();
        bool Equals(object other);
    }
}