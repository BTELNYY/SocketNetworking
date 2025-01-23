using SocketNetworking.PacketSystem.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.Shared;
using SocketNetworking.Attributes;

namespace SocketNetworking.Shared
{
    /// <summary>
    /// All NetworkObjects should implement this interface. Note that Client/Server authority isn't handled, all packets are sent to this object regardless of source.
    /// </summary>
    public interface INetworkObject : INetworkSpawnable
    {
        /// <summary>
        /// The <see cref="NetworkClient.ClientID"/> of the current owner. This must not be a live property, Do NOT use the setter to update it networkside. Use <see cref="NetworkObjectExtensions.NetworkSetOwner(INetworkObject, int)"/> to set this network wide.
        /// </summary>
        int OwnerClientID { get; set; }

        /// <summary>
        /// Determines the objects ownership mode. <see cref="OwnershipMode.Client"/> = the <see cref="OwnerClientID"/> owns the object. <see cref="OwnershipMode.Server"/> = the Server owns the object. <see cref="OwnershipMode.Public"/> = Everybody owns the object. This must not be a live property, Do NOT use the setter to update it networkside. Use <see cref="NetworkObjectExtensions.NetworkSetOwnershipMode(INetworkObject, OwnershipMode)"/> to set this network wide.
        /// </summary>
        OwnershipMode OwnershipMode { get; set; }

        /// <summary>
        /// Determines the object visibility. This must not be a live property, Do NOT use the setter to update it networkside.
        /// </summary>
        ObjectVisibilityMode ObjectVisibilityMode { get; set; }

        /// <summary>
        /// The network ID of the object. This should be the same on the client and server.
        /// </summary>
        int NetworkID { get; }

        /// <summary>
        /// If this is false, the object is never updated. This includes <see cref="INetworkSyncVar"/> fields, <see cref="PacketListener>"/> and <see cref="NetworkInvocable"/> methods.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// This is called on the server and client when the object is added to the list of currently updated network objects.
        /// </summary>
        void OnAdded(INetworkObject addedObject);

        /// <summary>
        /// Called on server and client when an object is removed.
        /// </summary>
        /// <param name="removedObject"></param>
        void OnRemoved(INetworkObject removedObject);

        /// <summary>
        /// Called on the Server and Client when the <see cref="NetworkClient.Ready"/> property is set to true.
        /// </summary>
        /// <param name="client"></param>
        void OnReady(NetworkClient client, bool isReady);

        /// <summary>
        /// Called on the server and client when the <see cref="NetworkClient.CurrentConnectionState"/> is set to <see cref="ConnectionState.Connected"/>
        /// </summary>
        /// <param name="client"></param>
        void OnConnected(NetworkClient client);

        /// <summary>
        /// Called on Server and Client when the <see cref="NetworkClient"/> is disconnected.
        /// </summary>
        /// <param name="client"></param>
        void OnDisconnected(NetworkClient client);
    }

    public enum OwnershipMode : byte
    {
        Client,
        Server,
        Public
    }
}
