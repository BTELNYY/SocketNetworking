using SocketNetworking.Client;
using SocketNetworking.Server;

namespace BasicChat.Shared
{
    public class ChatServer : TcpNetworkServer
    {
        public static void SendMessage(Message message)
        {
            foreach (NetworkClient client in Clients)
            {
#pragma warning disable CS0612 // Type or member is obsolete
                client.NetworkInvokeOnClient("ClientGetMessage", message);
#pragma warning restore CS0612 // Type or member is obsolete
            }
        }
    }
}