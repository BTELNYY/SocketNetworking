using SocketNetworking;
using SocketNetworking.Server;
using SocketNetworking.Shared;
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
            NetworkServer.ProtocolConfiguration = new SocketNetworking.Shared.Messages.ProtocolConfiguration("Werewolf", "1.0.0");
            server.StartServer();
            NetworkServer.ServerReady += () =>
            {
                GameManager manager = new GameManager();
                manager.ObjectVisibilityMode = ObjectVisibilityMode.Everyone;
                manager.OwnershipMode = OwnershipMode.Server;
                manager.NetworkSpawn();
            };
            while (true)
            {

            }
        }
    }
}
