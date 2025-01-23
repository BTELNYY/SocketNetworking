using SocketNetworking.Client;
using SocketNetworking.PacketSystem.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Shared
{
    public interface INetworkSpawnable
    {
        /// <summary>
        /// Called on the client when the object has finished being spawned and the spawn response has been sent to the server.
        /// </summary>
        void OnLocalSpawned(ObjectManagePacket packet);

        /// <summary>
        /// Determines if the object can be spawned.
        /// </summary>
        bool Spawnable { get; }

        void RecieveExtraData(byte[] extraData);

        byte[] SendExtraData();
    }
}
