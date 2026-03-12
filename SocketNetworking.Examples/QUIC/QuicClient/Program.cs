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
        client.QuicTransport.RemoteCertValidationCallback += QuicTransport_RemoteCertValidationCallback;
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

    //bypass SSL
    //We do this because MsQuic does not like self signed certs, and because we know we are only ever connecting to localhost, we do not care. In real applications, this will not be a problem assuming your cert is signed by a trusted CA.
    private static bool QuicTransport_RemoteCertValidationCallback(bool arg1, object arg2, System.Security.Cryptography.X509Certificates.X509Certificate arg3, System.Security.Cryptography.X509Certificates.X509Chain arg4, System.Net.Security.SslPolicyErrors arg5)
    {
        if (arg5 == System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors)
        {
            return true;
        }
        return false;
    }
}