using SocketNetworking.Shared.PacketSystem.Packets;
using System;
using System.Reflection;

namespace SocketNetworking.Shared.Attributes
{
    /// <summary>
    /// This attribute should be applied to all methods on <see cref="INetworkObject"/> objects which should listen for Packets. Note that the method should take specific arguments, the library will print a warning if it ignores the method because it has inproper arguments. The <see cref="NetworkDirection"/> represents from where the packet must originate to be accepted, for example, if your method is using <see cref="NetworkDirection.Server"/>, it will only accept packets sent by the server.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PacketListener : Attribute
    {
        private readonly Type _type;

        /// <summary>
        /// The type of the packet you are waiting for. Note that it must be registered and valid.
        /// </summary>
        public Type DefinedType
        {
            get
            {
                return _type;
            }
        }

        private readonly NetworkDirection _direction;

        /// <summary>
        /// From where are you accepting packets from. Any = Anywhere, Client = Server-Bound packets only, Server = Client-Bound packets only
        /// </summary>
        public NetworkDirection DefinedDirection
        {
            get
            {
                return _direction;
            }
        }

        /// <summary>
        /// Defines a packet listener. this method attribute should be used in conjunction with <see cref="SocketNetworking.Shared.PacketSystem.INetworkObject"/> to create network objects, methods with this attribute should always have two parameters: The <see cref="CustomPacket"/> type which you are listening for and the <see cref="NetworkClient"/>. Note that custom network clients are accepted. 
        /// </summary>
        /// <param name="type">
        /// The type of the packet you are waiting for. Note that it must be registered and inherit from <see cref="CustomPacket"/> as well as use the <see cref="PacketDefinition"/> attribute.
        /// </param>
        /// <param name="directionFromWhichPacketCanBeReceived">
        /// From where are you accepting packets from. Any = Anywhere, Client = Server-Bound packets only, Server = Client-Bound packets only
        /// </param>
        /// <exception cref="InvalidOperationException"></exception>
        public PacketListener(Type type, NetworkDirection directionFromWhichPacketCanBeReceived = NetworkDirection.Any)
        {
            if (!type.IsSubclassOf(typeof(CustomPacket)) || type.GetCustomAttribute(typeof(PacketDefinition)) == null)
            {
                if (type != null)
                {
                    throw new InvalidOperationException("The type provided isn't a valid Custom Packet.");
                }
            }
            _type = type;
            _direction = directionFromWhichPacketCanBeReceived;
        }
    }
}