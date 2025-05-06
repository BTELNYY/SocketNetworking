using SocketNetworking.Client;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.NetworkObjects
{
    /// <summary>
    /// The <see cref="INetworkSpawnable"/> interface represents any object which can be spawned by the server.
    /// </summary>
    public interface INetworkSpawnable
    {
        /// <summary>
        /// Called locally when the packet to spawn the object was Received. Also called when the peer of the connection Receives the packet and spawns the object. However, spawner (in case this method is overridden in a <see cref="INetworkObject"/>) does not know the object was spawned successfully yet, use <see cref="INetworkObject.OnNetworkSpawned(NetworkClient)"/> for this function.
        /// </summary>
        /// <param name="spawner"></param>
        void OnLocalSpawned(ObjectManagePacket packet);

        /// <summary>
        /// Determines if the object can be spawned.
        /// </summary>
        bool Spawnable { get; }

        /// <summary>
        /// Called when the object is being spawned to send extra data to it.
        /// </summary>
        /// <param name="extraData"></param>
        /// <returns></returns>
        ByteReader ReceiveExtraData(byte[] extraData);

        /// <summary>
        /// Called before the object is being spawned in order to get any extra information that should be transmitted.
        /// </summary>
        /// <returns></returns>
        ByteWriter SendExtraData();
    }
}
