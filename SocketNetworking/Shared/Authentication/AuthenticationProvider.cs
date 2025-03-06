using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;

namespace SocketNetworking.Shared.Authentication
{
    public abstract class AuthenticationProvider
    {
        public AuthenticationProvider(NetworkClient client)
        {
            Client = client;
        }

        /// <summary>
        /// By default, <see cref="AuthenticationProvider"/>s have the <see cref="NetworkClient"/> begin the auth process.
        /// </summary>
        public virtual bool ClientInitiate => true;

        public NetworkClient Client { get; }
    }
}
