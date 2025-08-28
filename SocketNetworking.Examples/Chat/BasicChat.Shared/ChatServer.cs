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
                client.NetworkInvokeOnClient("ClientGetMessage", message);
            }
        }
    }
}