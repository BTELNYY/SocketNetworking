using SocketNetworking.Client;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.NetworkObjects
{
    public interface INetworkSpawnable
    {
        /// <summary>
        /// Called locally when the packet to spawn the object was Received. Also called when the peer of the connection Receives the packet and spawns the object. However, spawner (in case this method is overriden in a <see cref="INetworkObject"/>) does not know the object was spawned succesfully yet, use <see cref="INetworkObject.OnNetworkSpawned(NetworkClient)"/> for this function.
        /// </summary>
        /// <param name="spawner"></param>
        void OnLocalSpawned(ObjectManagePacket packet);

        /// <summary>
        /// Determines if the object can be spawned.
        /// </summary>
        bool Spawnable { get; }

        ByteReader ReceiveExtraData(byte[] extraData);

        ByteWriter SendExtraData();
    }
}
