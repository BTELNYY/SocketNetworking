using SocketNetworking.PacketSystem.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem
{
    /// <summary>
    /// All NetworkObjects should implement this interface. Note that Client/Server authority isn't handled, all packets are sent to this object regardless of source.
    /// </summary>
    public interface INetworkObject
    {
        /// <summary>
        /// The <see cref="NetworkClient.ClientID"/> of the current owner.
        /// </summary>
        int OwnerClientID { get; set; }

        /// <summary>
        /// Determines the objects ownership mode. <see cref="OwnershipMode.Client"/> = the <see cref="OwnerClientID"/> owns the object. <see cref="OwnershipMode.Server"/> = the Server owns the object. <see cref="OwnershipMode.Public"/> = Everybody owns the object.
        /// </summary>
        OwnershipMode OwnershipMode { get; set; }

        /// <summary>
        /// The network ID of the object. This should be the same on the client and server.
        /// </summary>
        int NetworkID { get; }

        /// <summary>
        /// If this is false, the object is never updated.
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
        /// Called on Server and Client when the <see cref="NetworkClient"/> is disconnected.
        /// </summary>
        /// <param name="client"></param>
        void OnDisconnected(NetworkClient client);
    }

    public enum OwnershipMode
    {
        Client,
        Server,
        Public
    }
}
