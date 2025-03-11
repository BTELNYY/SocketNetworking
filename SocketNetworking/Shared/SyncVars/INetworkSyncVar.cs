using SocketNetworking.Client;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.PacketSystem.Packets;

namespace SocketNetworking.Shared.SyncVars
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
        INetworkObject OwnerObject { get; set; }

        /// <summary>
        /// The direction in which the <see cref="INetworkSyncVar"/> accepts changes to its state.
        /// </summary>
        OwnershipMode SyncOwner { get; set; }

        /// <summary>
        /// The raw value of the object. This should update the value of the object on the Network. See <see cref="RawSet(object, NetworkClient)"/>, which will update the value locally.
        /// </summary>
        object ValueRaw { get; set; }

        /// <summary>
        /// This value should not update the state of the object on the network, instead it should accept the value change in state.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="who"></param>
        void RawSet(object value, NetworkClient who);

        /// <summary>
        /// This value should not update the state of the object on the network, instead it should accept the mode change.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="who"></param>
        void RawSet(OwnershipMode mode, NetworkClient who);

        /// <summary>
        /// Called when the object has been spawned by the <see cref="NetworkClient"/> in order to sync the current value to them.
        /// </summary>
        /// <param name="who"></param>
        void SyncTo(NetworkClient who);

        /// <summary>
        /// Forces the <see cref="INetworkSyncVar"/> to be synced to everyone (as applicable by <see cref="SyncOwner"/> and the <see cref="OwnerObject"/>s <see cref="ObjectVisibilityMode"/>)
        /// </summary>
        void Sync();

        /// <summary>
        /// Get the <see cref="SyncVarData"/> to send.
        /// </summary>
        /// <returns></returns>
        SyncVarData GetData();
    }
}