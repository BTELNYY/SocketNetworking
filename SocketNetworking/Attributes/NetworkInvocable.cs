using System;
using System.Collections.Generic;
using SocketNetworking.PacketSystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class NetworkInvocable : Attribute
    {
        /// <summary>
        /// From where are you accepting packets from. Any = Anywhere, Client = Server-Bound packets only, Server = Client-Bound packets only
        /// </summary>
        public PacketDirection Direction { get; set; } = PacketDirection.Any;

        /// <summary>
        /// Attempts to make sure network invoked calls only originate from the proper client. Becuase it is Server Authoritive, this property has no effect if the Network call is coming from the server.
        /// </summary>
        public bool SecureMode { get; set; } = true;

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkInvocable"/> attribute. Note that <see cref="SecureMode"/> is set to true using this constroctur, meaning that security is done. The method that is attached to this attribute must have the object implement <see cref="INetworkOwned"/>, or be a <see cref="NetworkClient"/>. OR, the method may take a <see cref="NetworkClient"/> as its first argument, this does not garrauntee safety, but does allow you to check manually. Not doing any of these will generate a warning at runtime. Note that if called from the server, security checks aren't applied.
        /// </summary>
        public NetworkInvocable() { }

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkInvocable"/> attribute. If <see cref="SecureMode"/> is <see cref="true"/>, the method that is attached to this attribute must have the object implement <see cref="INetworkOwned"/>, or be a <see cref="NetworkClient"/>. OR, the method may take a <see cref="NetworkClient"/> as its first argument, this does not garrauntee safety, but does allow you to check manually. Not doing any of these will generate a warning at runtime. Note that if called from the server, security checks aren't applied.
        /// </summary>
        /// <param name="secureMode"></param>
        public NetworkInvocable(bool secureMode) 
        {
            SecureMode = secureMode;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkInvocable"/> attribute and assigns a <see cref="PacketDirection"/> value to <see cref="Direction"/>. The default value is <see cref="PacketDirection.Any"/>
        /// </summary>
        /// <param name="direction"></param>
        public NetworkInvocable(PacketDirection direction)
        {
            Direction = direction;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkInvocable"/> attribute and assigns a <see cref="PacketDirection"/> value to <see cref="Direction"/>. The default value is <see cref="PacketDirection.Any"/>. If <see cref="SecureMode"/> is <see cref="true"/>, the method that is attached to this attribute must have the object implement <see cref="INetworkOwned"/>, or be a <see cref="NetworkClient"/>. OR, the method may take a <see cref="NetworkClient"/> as its first argument, this does not garrauntee safety, but does allow you to check manually. Not doing any of these will generate a warning at runtime. Note that if called from the server, security checks aren't applied.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="secureMode"></param>
        public NetworkInvocable(PacketDirection direction, bool secureMode) : this(direction)
        {
            SecureMode = secureMode;
        }
    }
}
