using System.Runtime.Versioning;
using QuicShared;
using SocketNetworking;
using SocketNetworking.Client;
using SocketNetworking.Shared;

namespace QuicClient;

[RequiresPreviewFeatures]
public class Program
{
    public static void Main(string[] args)
    {
        Log.Levels = Log.FULL_LOG;
        Log.OnLog += ExampleLogger.HandleNetworkLog;
        NetworkManager.ImportAssembly(Utility.GetAssembly());
        QuicNetworkClient client = new QuicNetworkClient();
        client.Connect("127.0.0.1", 7777);
    }
}