using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;

namespace SocketNetworking.Shared
{
    public class NetworkStreams
    {
        public NetworkClient Client { get; }

        public NetworkStreams(NetworkClient client)
        {
            Client = client;
        }


    }
}
