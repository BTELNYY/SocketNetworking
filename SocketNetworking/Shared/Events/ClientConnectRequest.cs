using SocketNetworking.Client;
using SocketNetworking.Misc;

namespace SocketNetworking.Shared.Events
{
    /// <summary>
    /// The <see cref="ClientConnectRequest"/> <see cref="ChoiceEvent"/> is used to determine if a connection request is authorized to continue.
    /// </summary>
    public class ClientConnectRequest : ChoiceEvent
    {
        public ClientConnectRequest(NetworkClient client, bool defaultState) : base(defaultState)
        {
            Client = client;
        }

        public NetworkClient Client { get; }
    }
}
