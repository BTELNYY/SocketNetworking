using SocketNetworking.Client;
using SocketNetworking.Shared.PacketSystem.Packets;
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

        /// <summary>
        /// Determines if <see cref="NetworkClient.ServerBeginSync"/> can spawn this object automatically. Note that this differs from <see cref="Spawnable"/> as this only controls automatic spawning, <see cref="Spawnable"/> is still responsible for if the <see cref="INetworkSpawnable"/> can be spawned at all.
        /// </summary>
        bool AutoSpawn { get; }

        ByteReader ReceiveExtraData(byte[] extraData);

        ByteWriter SendExtraData();
    }
}
