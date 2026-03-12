using System.Runtime.Versioning;
using QuicShared;
using SocketNetworking;
using SocketNetworking.Client;
using SocketNetworking.Server;
using SocketNetworking.Shared;

namespace QuicServer
{
    [RequiresPreviewFeatures]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macOS")]
    public class Program
    {
        static readonly string Title = "Clients: {count}";

        public static void Main(string[] args)
        {
            Log.Levels = Log.FULL_LOG;
            Log.OnLog += ExampleLogger.HandleNetworkLog;
            AppDomain.CurrentDomain.ProcessExit += (sender, evtArgs) =>
            {
                NetworkServer.ServerInstance.StopServer();
            };
            Console.CancelKeyPress += (sender, e) =>
            {
                NetworkServer.ServerInstance.StopServer();
            };
            NetworkManager.ImportAssembly(Utility.GetAssembly());
            NetworkManager.ImportTypes([typeof(QuicNetworkClient)]);
            QuicNetworkServer server = new QuicNetworkServer();
            NetworkServer.ClientType = typeof(QuicNetworkClient);
            NetworkServer.Config.HandshakeTime = 10f;
            NetworkServer.Config.EncryptionMode = ServerEncryptionMode.Required;
            //speeeling
            NetworkServer.Config.CertificatePath = "./cert.pfx";
            NetworkServer.Config.CertificatePassword = "Pa55w.rd";
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
