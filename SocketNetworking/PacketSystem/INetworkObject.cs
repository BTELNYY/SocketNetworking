using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem
{
    public interface INetworkObject
    {
        /// <summary>
        /// The network ID of the object. This should be the same on the client and server.
        /// </summary>
        int NetworkID { get; }

        /// <summary>
        /// If this is false, the object is never updated.
        /// </summary>
        bool IsActive { get; }

        void OnAdded(NetworkClient client);

        void OnReady(NetworkClient client);

        void OnDisconnected(NetworkClient client);
    }
}
