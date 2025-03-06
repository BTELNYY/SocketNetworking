using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketNetworking.Server;
using SocketNetworking.Shared;
using SocketNetworking;
using BasicChat.Shared;

namespace BasicChat.Server
{
    public class Program
    {
        static string Title = "Clients: {count}";

        public static void Main(string[] args)
        {
            Log.OnLog += Logger.HandleNetworkLog;
            Log.Levels = Log.FULL_LOG;
            AppDomain.CurrentDomain.ProcessExit += (sender, evtArgs) =>
            {
                NetworkServer.ServerInstance.StopServer();
            };
            Console.CancelKeyPress += (sender, e) =>
            {
                NetworkServer.ServerInstance.StopServer();
            };
            NetworkManager.ImportAssmebly(Utility.GetAssembly());
            TcpNetworkServer server = new TcpNetworkServer();
            NetworkServer.ClientType = typeof(ChatClient);
            NetworkServer.ClientAvatar = typeof(ChatAvatar);
            NetworkServer.Config.HandshakeTime = 5f;
            NetworkServer.Config.EncryptionMode = ServerEncryptionMode.Required;
            //speeeling
            NetworkServer.ClientConnected += (x) =>
            {
                Console.Title = Title.Replace("{count}", NetworkServer.Clients.Count.ToString());
            };
            NetworkServer.ClientDisconnected += (x) =>
            {
                Console.Title = Title.Replace("{count}", NetworkServer.Clients.Count.ToString());
            };
            server.StartServer();
        }
    }
}
