using SocketNetworking.Client;
using SocketNetworking.Shared.NetworkObjects;
using System;

namespace SocketNetworking.Shared.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class NetworkInvokable : Attribute
    {
        /// <summary>
        /// From where are you accepting packets from. Any = Anywhere, Client = Server-Bound packets only, Server = Client-Bound packets only
        /// </summary>
        public NetworkDirection Direction { get; set; } = NetworkDirection.Any;

        /// <summary>
        /// Attempts to make sure network invoked calls only originate from the proper client. because it is Server Authoritive, this property has no effect if the Network call is coming from the server. Effectively, this will check <see cref="INetworkObject.OwnerClientID"/> as well as the <see cref="INetworkObject.OwnershipMode"/> against the calling clients <see cref="NetworkClient.ClientID"/>.
        /// </summary>
        public bool SecureMode { get; set; } = true;

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkInvokable"/> attribute. Note that <see cref="SecureMode"/> is set to true using this constroctur, meaning that security is done. The method that is attached to this attribute must have the object implement <see cref="INetworkOwned"/>, or be a <see cref="NetworkClient"/>. OR, the method may take a <see cref="NetworkClient"/> as its first argument, this does not garrauntee safety, but does allow you to check manually. Not doing any of these will generate a warning at runtime. Note that if called from the server, security checks aren't applied.
        /// </summary>
        public NetworkInvokable() { }

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkInvokable"/> attribute. If <see cref="SecureMode"/> is <see cref="true"/>, the method that is attached to this attribute must have the object must be a <see cref="NetworkClient"/>. OR, the method may take a <see cref="NetworkHandle"/> as its first argument, this does not garrauntee safety, but does allow you to check manually. Not doing any of these will generate a warning at runtime. Note that if called from the server, security checks aren't applied.
        /// </summary>
        /// <param name="secureMode"></param>
        public NetworkInvokable(bool secureMode)
        {
            SecureMode = secureMode;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkInvokable"/> attribute and assigns a <see cref="NetworkDirection"/> value to <see cref="Direction"/>. The default value is <see cref="NetworkDirection.Any"/>
        /// </summary>
        /// <param name="direction"></param>
        public NetworkInvokable(NetworkDirection direction)
        {
            Direction = direction;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkInvokable"/> attribute and assigns a <see cref="NetworkDirection"/> value to <see cref="Direction"/>. The default value is <see cref="NetworkDirection.Any"/>. If <see cref="SecureMode"/> is <see cref="true"/>, the method that is attached to this attribute must have the object implement <see cref="INetworkOwned"/>, or be a <see cref="NetworkClient"/>. OR, the method may take a <see cref="NetworkClient"/> as its first argument, this does not garrauntee safety, but does allow you to check manually. Not doing any of these will generate a warning at runtime. Note that if called from the server, security checks aren't applied.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="secureMode"></param>
        public NetworkInvokable(NetworkDirection direction, bool secureMode) : this(direction)
        {
            SecureMode = secureMode;
        }
    }
}
