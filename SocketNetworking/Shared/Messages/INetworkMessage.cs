using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.Messages
{
    /// <summary>
    /// Specifies the generic delegate which handles <see cref="INetworkMessage{T}"/>s sent and recieved by <see cref="INetworkObject"/>s.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="message"></param>
    public delegate void NetworkMessageHandler<T>(INetworkMessage<T> message);

    /// <summary>
    /// Represents a generic message which can be sent and recieved. Messages are sent between <see cref="INetworkObject"/>s, and are sent remotely. Peers may send messages to any <see cref="INetworkObject"/> they have access to. RPCs, messages do not have an option to return a value. Instead, send a message back.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface INetworkMessage<T> : INetworkMessage
    {
        /// <summary>
        /// The data contained within the <see cref="INetworkMessage{T}"/>
        /// </summary>
        T Data { get; }
    }

    public interface INetworkMessage : IByteSerializable
    {
        /// <summary>
        /// Specifies the source <see cref="INetworkObject"/>
        /// </summary>
        INetworkObject Source { get; }

        /// <summary>
        /// Specifies the destination <see cref="INetworkObject"/>. Note that this can be null if the message was broadcasted.
        /// </summary>
        INetworkObject Destination { get; }

        /// <summary>
        /// Is <see langword="true"/> if the sender of this message was not a peer, and instead was the server.
        /// </summary>
        bool SenderIsAuthority { get; set; }

        /// <summary>
        /// Represents the stored data as an object.
        /// </summary>
        object DataObject { get; }
    }
}
