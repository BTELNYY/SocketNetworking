using SocketNetworking.PacketSystem.Packets;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.PacketSystem;
using SocketNetworking.Shared;

namespace SocketNetworking.Attributes
{
    /// <summary>
    /// This attribute should be applied to all methods on <see cref="INetworkObject"/> objects which should listen for Packets. Note that the method should take specific arguments, the library will print a warning if it ignores the method becuase it has inproper arguments. The <see cref="PacketDirection"/> represents from where the packet must originate to be accepted, for example, if your method is using <see cref="PacketDirection.Server"/>, it will only accept packets sent by the server.
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

        private readonly PacketDirection _direction;

        /// <summary>
        /// From where are you accepting packets from. Any = Anywhere, Client = Server-Bound packets only, Server = Client-Bound packets only
        /// </summary>
        public PacketDirection DefinedDirection
        {
            get
            {
                return _direction;
            }
        }

        /// <summary>
        /// Defines a packet listener. this method attribute should be used in conjunction with <see cref="SocketNetworking.PacketSystem.INetworkObject"/> to create network objects, methods with this attribute should always have two parameters: The <see cref="CustomPacket"/> type which you are listening for and the <see cref="NetworkClient"/>. Note that custom network clients are accepted. 
        /// </summary>
        /// <param name="type">
        /// The type of the packet you are waiting for. Note that it must be registered and inherit from <see cref="CustomPacket"/> as well as use the <see cref="PacketDefinition"/> attribute.
        /// </param>
        /// <param name="directionFromWhichPacketCanBeRecieved">
        /// From where are you accepting packets from. Any = Anywhere, Client = Server-Bound packets only, Server = Client-Bound packets only
        /// </param>
        /// <exception cref="InvalidOperationException"></exception>
        public PacketListener(Type type, PacketDirection directionFromWhichPacketCanBeRecieved = PacketDirection.Any) 
        {
            if (!type.IsSubclassOf(typeof(CustomPacket)) || type.GetCustomAttribute(typeof(PacketDefinition)) == null)
            {
                if(type != null)
                {
                    throw new InvalidOperationException("The type provided isn't a valid Custom Packet.");
                }
            }
            _type = type;
            _direction = directionFromWhichPacketCanBeRecieved;
        }
    }
}