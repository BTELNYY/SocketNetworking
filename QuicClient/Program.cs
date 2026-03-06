using System.Runtime.Versioning;
using QuicShared;
using SocketNetworking;
using SocketNetworking.Client;
using SocketNetworking.Shared;

namespace QuicClient;

[RequiresPreviewFeatures]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("windows")]
[SupportedOSPlatform("macOS")]
public class Program
{
    static string Title = "ClientID: {id}, Latency: {ms}, Rx: {rxb}, Tx: {txb}";

    public static async Task Main(string[] args)
    {
        Log.Levels = Log.FULL_LOG;
        Log.OnLog += ExampleLogger.HandleNetworkLog;
        NetworkManager.ImportAssembly(Utility.GetAssembly());
        NetworkManager.ImportTypes([typeof(QuicNetworkClient)]);
        QuicNetworkClient client = new QuicNetworkClient();
        client.InitLocalClient();
        client.ClientIdUpdated += () =>
        {
            Console.Title = Title.Replace("{id}", client.ClientID.ToString()).Replace("{ms}", client.Latency.ToString()).Replace("{rxb}", client.BytesReceived.ToString()).Replace("{txb}", client.BytesSent.ToString());
        };
        client.LatencyChanged += (latency) =>
        {
            Console.Title = Title.Replace("{id}", client.ClientID.ToString()).Replace("{ms}", client.Latency.ToString()).Replace("{rxb}", client.BytesReceived.ToString()).Replace("{txb}", client.BytesSent.ToString());
        };
        client.Connect("127.0.0.1", 7777);
        await Task.Delay(-1);
    }
}