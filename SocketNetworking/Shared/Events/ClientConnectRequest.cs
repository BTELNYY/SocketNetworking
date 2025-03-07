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
