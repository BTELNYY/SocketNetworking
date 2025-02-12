using SocketNetworking.Client;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Shared.NetworkObjects
{
    public interface INetworkSpawnable
    {
        /// <summary>
        /// Called locally when the packet to spawn the object was recieved. Also called when the peer of the connection recieves the packet and spawns the object. However, spawner (in case this method is overriden in a <see cref="INetworkObject"/>) does not know the object was spawned succesfully yet, use <see cref="INetworkObject.OnNetworkSpawned(NetworkClient)"/> for this function.
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
