using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.Server;

namespace BasicChat.Shared
{
    public class ChatServer : TcpNetworkServer
    {
        public static void SendMessage(Message message)
        {
            foreach(NetworkClient client in Clients)
            {
                client.NetworkInvoke("ClientGetMessage", new object[] { message });
            }
        }
    }
}
