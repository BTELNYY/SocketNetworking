using System;
using System.Threading.Tasks;
using SocketNetworking;
using SocketNetworking.Server;
using Werewolf.Shared;

namespace Werewolf.Server
{
    public class Program
    {
        static void Main(string[] args)
        {
            Log.Levels = Log.FULL_LOG;
            Log.OnLog += ExampleLogger.HandleNetworkLog;
            Utility.ImportHelper();
            TcpNetworkServer server = new TcpNetworkServer();
            NetworkServer.ClientType = typeof(WerewolfClient);
            NetworkServer.ClientAvatar = typeof(PlayerAvatar);
            NetworkServer.Config.HandshakeTime = 5f;
            NetworkServer.Config.EncryptionMode = ServerEncryptionMode.Required;
            server.StartServer();
            while (true)
            {

            }
        }
    }
}
