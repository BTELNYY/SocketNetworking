using SocketNetworking.Client;
using SocketNetworking.PacketSystem;
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
        /// Called on the server when a <see cref="NetworkClient"/> spawns the object on the local machine.
        /// </summary>
        /// <param name="spawner"></param>
        void OnLocalSpawned(ObjectManagePacket packet);

        /// <summary>
        /// Determines if the object can be spawned.
        /// </summary>
        bool Spawnable { get; }

        ByteReader RecieveExtraData(byte[] extraData);

        ByteWriter SendExtraData();
    }
}
