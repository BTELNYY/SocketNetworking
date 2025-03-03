using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.Misc;

namespace SocketNetworking.Shared.Events
{
    public class ClientConnectRequest : ChoiceEvent
    {
        public ClientConnectRequest(NetworkClient client, bool defaultState) : base(defaultState)
        {
            Client = client;
        }

        public NetworkClient Client { get; }
    }
}
